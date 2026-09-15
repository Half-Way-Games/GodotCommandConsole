using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Godot;
using HWG.CommandConsole.Autocomplete;
using HWG.CommandConsole.Console;
namespace HWG.CommandConsole;

public partial class DevConsoleUI : Control
{
    [Export] private LineEdit inputField;
    [Export] private RichTextLabel outputText;
    
    [Export] private ScrollContainer suggestionContainer;
    [Export] private VBoxContainer suggestionVBox;
    [Export] private PackedScene suggestionItemScene;
    
    [Export] private CheckBox debugToggle;
    [Export] private CheckBox infoToggle;
    [Export] private CheckBox warningToggle;
    [Export] private CheckBox errorToggle;
    [Export] private Button clearButton;
    
    private readonly List<AutocompleteSuggestion> currentSuggestions = new(MAX_SUGGESTIONS);
    private SuggestionItemUI[] suggestionItems = new SuggestionItemUI[MAX_SUGGESTIONS];
    private int selectedSuggestionIndex = -1;
    private const int MAX_SUGGESTIONS = 8;
    private const int MAX_VISIBLE_LOG_LINES = 1000;
    private const int MAX_STORED_LOG_ENTRIES = MAX_VISIBLE_LOG_LINES * 2;
    private const int LOG_PRUNE_BUFFER = 200; // Allow going over MaxLogLines by a bit so not constantly pruning

    private readonly List<string> commandHistory = [];
    private int historyIndex = -1;
    private readonly List<Log.LogEntry> allLogEntries = [];
    private readonly StringBuilder logBuilder = new();
    private readonly int[] visibleLogIndices = new int[MAX_VISIBLE_LOG_LINES];

    private static readonly StringName devConsoleInput = "dev_console_toggle";
    private static readonly StringName escapeInput = "ui_cancel";
    
    // Queue to hold logs coming from any thread, processed in _Process
    private readonly ConcurrentQueue<Log.LogEntry> logQueue = new();

    private Input.MouseModeEnum previousMouseMode;


    public override void _Ready()
    {
        inputField.TextChanged += OnInputTextChanged;
        inputField.TextSubmitted += OnInputSubmitted;
        inputField.GuiInput += OnInputGuiInput;
        
        // Subscribe to log messages
        Log.MessageLogged += OnLogMessage;

        SetupFilterToggles();

        clearButton.Pressed += OnClearPressed;
        
        // Hide suggestions initially
        CreateSuggestionItems();
        suggestionContainer.Visible = false;
        
        DevConsole.RegisterAllCommands();
        
        Log.Info("Developer Console initialized. Type 'help' for commands.");
    }

    
    public override void _Process(double delta)
    {
        if (logQueue.IsEmpty) return;

        logBuilder.Clear();
        int newLinesCount = 0;

        while (logQueue.TryDequeue(out var entry))
        {
            allLogEntries.Add(entry);
            
            if (allLogEntries.Count > MAX_STORED_LOG_ENTRIES + LOG_PRUNE_BUFFER)
                allLogEntries.RemoveRange(0, LOG_PRUNE_BUFFER);
            
            if (!ShouldShowEntry(entry))
                continue;

            logBuilder.Append(Log.FormatLogEntry(entry));
            logBuilder.Append('\n');
            newLinesCount++;
        }
        
        if (newLinesCount <= 0)
            return;
        
        // Single UI update for all logs this frame
        outputText.AppendText(logBuilder.ToString());
        
        // Only rebuild if we exceed the limit + buffer
        // This prevents rebuilding every frame when at capacity
        if (outputText.GetLineCount() > MAX_VISIBLE_LOG_LINES + LOG_PRUNE_BUFFER)
            RebuildLogView();
        else
            outputText.ScrollToLine(outputText.GetLineCount() - 1);
    }


    public override void _ExitTree()
    {
        Log.MessageLogged -= OnLogMessage;
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(devConsoleInput))
        {
            if (Visible)
                HideConsole();
            else
                ShowConsole();
        }
        else if (@event.IsActionPressed(escapeInput) && Visible)
            HideConsole();
    }

    public void Clear()
    {
        outputText.Text = string.Empty;
        allLogEntries.Clear();
    }

    private void ShowConsole()
    {
        Visible = true;
        previousMouseMode = Input.MouseMode;
        Input.SetMouseMode(Input.MouseModeEnum.Visible);
        inputField.GrabFocus();
    }

    private void HideConsole()
    {
        Visible = false;
        inputField.ReleaseFocus();
        Input.SetMouseMode(previousMouseMode);
    }
    
    private void SetupFilterToggles()
    {
        debugToggle.Toggled += FilterLogs;
        infoToggle.Toggled += FilterLogs;
        warningToggle.Toggled += FilterLogs;
        errorToggle.Toggled += FilterLogs;
    }

    private void CreateSuggestionItems()
    {
        for (int i = 0; i < MAX_SUGGESTIONS; i++)
        {
            var currentSuggestionItem = suggestionItemScene.Instantiate<SuggestionItemUI>();
            suggestionItems[i] = currentSuggestionItem;
            suggestionVBox.AddChild(currentSuggestionItem);
        }
    }
    
    private bool ShouldShowEntry(Log.LogEntry entry)
    {
        return entry.Level switch
        {
            Log.LogLevel.Debug => debugToggle.ButtonPressed,
            Log.LogLevel.Info => infoToggle.ButtonPressed,
            Log.LogLevel.Warning => warningToggle.ButtonPressed,
            Log.LogLevel.Error => errorToggle.ButtonPressed,
            _ => true
        };
    }

    private void FilterLogs(bool toggled)
    {
        RebuildLogView();
    }

    private void RebuildLogView()
    {
        logBuilder.Clear();

        int visibleCount = 0;

        for (int i = allLogEntries.Count - 1; i >= 0 && visibleCount < MAX_VISIBLE_LOG_LINES; i--)
        {
            if (!ShouldShowEntry(allLogEntries[i]))
                continue;
            
            visibleLogIndices[visibleCount] = i;
            visibleCount++;
        }

        for (int i = visibleCount - 1; i > +0; i--)
        {
            var entry = allLogEntries[visibleLogIndices[i]];
            logBuilder.AppendLine(Log.FormatLogEntry(entry));
        }

        outputText.Text = logBuilder.ToString();
        outputText.ScrollToLine(outputText.GetLineCount() - 1);
    }

    private void UpdateSuggestions(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            HideSuggestions();
            return;
        }

        ConsoleAutocomplete.FillSuggestions(input, currentSuggestions, MAX_SUGGESTIONS);
        selectedSuggestionIndex = -1;

        DisplaySuggestions();
    }

    private void DisplaySuggestions()
    {
        if (currentSuggestions.Count == 0)
        {
            HideSuggestions();
            return;
        }

        suggestionContainer.Visible = true;

        for (var i = 0; i < suggestionItems.Length; i++)
        {
            var suggestionItem = suggestionItems[i];
            if (i >= currentSuggestions.Count)
            {
                suggestionItem.Visible = false;
                continue;
            }
            
            var suggestion = currentSuggestions[i];
            bool isSelected = i == selectedSuggestionIndex;

            suggestionItem.Visible = true;
            suggestionItem.SetLabelText(suggestion.DisplayText);
            suggestionItem.SetDescriptionText(suggestion.Description);
            suggestionItem.SetSelected(isSelected);
        }
    }
    
    private void NavigateSuggestions(int direction)
    {
        if (currentSuggestions.Count == 0)
            return;

        selectedSuggestionIndex += direction;

        if (selectedSuggestionIndex < 0)
            selectedSuggestionIndex = currentSuggestions.Count - 1;
        else if (selectedSuggestionIndex >= currentSuggestions.Count)
            selectedSuggestionIndex = 0;

        DisplaySuggestions();
        ScrollToSelectedSuggestion();
    }
    
    private void ScrollToSelectedSuggestion()
    {
        if (selectedSuggestionIndex < 0 || selectedSuggestionIndex >= suggestionItems.Length)
            return;

        var selectedChild = suggestionItems[selectedSuggestionIndex];
        if (!selectedChild.Visible)
            return;
        
        // Calculate positions
        float itemTop = selectedChild.Position.Y;
        float itemBottom = itemTop + selectedChild.Size.Y;
        float viewportTop = suggestionContainer.ScrollVertical;
        float viewportBottom = viewportTop + suggestionContainer.Size.Y;
        
        // Scroll if item is out of view
        if (itemTop < viewportTop)
        {
            // Item is above visible area - scroll up
            suggestionContainer.ScrollVertical = (int)itemTop;
        } else if (itemBottom > viewportBottom)
        {
            // Item is below visible area - scroll down
            suggestionContainer.ScrollVertical = (int)(itemBottom - suggestionContainer.Size.Y);
        }
    }

    private static bool CanAcceptSuggestion(AutocompleteSuggestion suggestion)
    {
        return suggestion.Type is AutocompleteSuggestionType.Command or AutocompleteSuggestionType.ParameterValue && !string.IsNullOrEmpty(suggestion.Text);
    }
    
    private void AcceptSuggestion(AutocompleteSuggestion suggestion)
    {
        var newText = ConsoleAutocomplete.AutoComplete(inputField.Text, suggestion);
        inputField.Text = newText;
        inputField.CaretColumn = newText.Length;

        // Update suggestions for the new text
        UpdateSuggestions(newText);
    }

    private void HideSuggestions()
    {
        suggestionContainer.Visible = false;
        currentSuggestions.Clear();
        selectedSuggestionIndex = -1;
    }

    private void NavigateHistory(int direction)
    {
        if (commandHistory.Count == 0)
            return;

        historyIndex += direction;

        if (historyIndex < 0)
            historyIndex = 0;
        else if (historyIndex >= commandHistory.Count)
        {
            historyIndex = commandHistory.Count;
            inputField.Text = "";
            return;
        }

        inputField.Text = commandHistory[historyIndex];
        inputField.CaretColumn = inputField.Text.Length;
    }

    private void ExecuteCommand(string command)
    {
        HideSuggestions();

        if (string.IsNullOrWhiteSpace(command))
            return;

        // Add to history
        commandHistory.Add(command);
        historyIndex = commandHistory.Count;

        // Add a command to output
        Log.Command($"> {command}");

        
        DevConsole.ExecuteCommand(command);

        // Clear input
        inputField.Text = "";

        // Scroll to bottom
        outputText.ScrollToLine(outputText.GetLineCount() - 1);
    }

    private void OnInputTextChanged(string input)
    {
        UpdateSuggestions(input);
    }

    private void OnInputSubmitted(string input)
    {
        // Only accept suggestion if it has actual text to insert
        if (selectedSuggestionIndex >= 0 && selectedSuggestionIndex < currentSuggestions.Count && CanAcceptSuggestion(currentSuggestions[selectedSuggestionIndex]))
            AcceptSuggestion(currentSuggestions[selectedSuggestionIndex]);
        else
            ExecuteCommand(input);
    }

    private void OnInputGuiInput(InputEvent @event)
    {
        if (@event is not InputEventKey {Pressed: true} keyEvent)
            return;
        switch (keyEvent.Keycode)
        {
            case Key.Up:
                if (currentSuggestions.Count > 0)
                    NavigateSuggestions(-1);
                else
                    NavigateHistory(-1);
                break;
            case Key.Down:
                if (currentSuggestions.Count > 0)
                    NavigateSuggestions(1);
                else
                    NavigateHistory(1);
                break;
            case Key.Tab:
                if (currentSuggestions.Count > 0)
                {
                    int index = selectedSuggestionIndex >= 0 ? selectedSuggestionIndex : 0;
                    var suggestion = currentSuggestions[index];
                    
                    if (CanAcceptSuggestion(suggestion))
                        AcceptSuggestion(suggestion);
                }
                break;
            case Key.Quoteleft:
            case Key.Escape:
                if (suggestionContainer.Visible)
                    HideSuggestions();
                else
                    HideConsole();
                break;
            default:
                return;
        }
        GetViewport().SetInputAsHandled();
    }

    private void OnLogMessage(Log.LogEntry entry)
    {
        logQueue.Enqueue(entry);
    }

    private void OnClearPressed()
    {
        Clear();
    }
}