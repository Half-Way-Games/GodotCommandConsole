using System;
using System.Collections.Generic;
using System.Linq;
using HWG.CommandConsole.Commands;
using HWG.CommandConsole.Console;
namespace HWG.CommandConsole.Autocomplete;

public class ConsoleAutocomplete
{
    private const float EXACT_MATCH_BONUS = 100f;
    private const float PREFIX_MATCH_BONUS = 50f;
    private const float CONTAINS_MATCH_BONUS = 25f;
    private const float SEQUENCE_MATCH_BONUS = 10f;

    public List<AutocompleteSuggestion> GetSuggestions(string input, int maxResults = 10)
    {
        var parseResult = ConsoleInputParser.Parse(input);
        
        if (string.IsNullOrWhiteSpace(input))
        {
            return
            [
                .. DevConsole.GetAllCommands()
                    .Values
                    .OrderBy(cmd => cmd.FullName)
                    .Take(maxResults)
                    .Select(cmd => new AutocompleteSuggestion
                {
                    Text = cmd.FullName,
                    DisplayText = cmd.FullName,
                    Description = cmd.Description,
                    Score = 0,
                    CommandInfo = cmd,
                    Type = AutocompleteSuggestionType.Command
                })
            ];
        }
        
        var suggestions = new List<AutocompleteSuggestion>();

        if (parseResult.IsTypingCommandName)
        {
            suggestions.AddRange(GetCommandSuggestions(parseResult.CommandName, maxResults));
        }
        else
        {
            // Typing parameters - show command signature/help row
            var command = DevConsole.GetCommand(parseResult.CommandName);
            if (command != null)
                suggestions.Add(CreateSignatureSuggestions(command, parseResult.CurrentArgumentIndex));
        }
        
        return [
            .. suggestions
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.Text)
                .Take(maxResults)
        ];
    }
    
    public string AutoComplete(string currentInput, List<AutocompleteSuggestion> suggestions)
    {
        if (suggestions.Count == 0) 
            return currentInput;

        var best = suggestions[0];

        return best.Type switch
        {
            AutocompleteSuggestionType.Signature => currentInput,
            AutocompleteSuggestionType.Command => best.Text + " ",
            AutocompleteSuggestionType.ParameterValue => ApplyParameterSuggestion(currentInput, best),
            _ => currentInput
        };
    }
    
    private static string ApplyParameterSuggestion(string currentInput, AutocompleteSuggestion suggestion)
    {
        var parseResult = ConsoleInputParser.Parse(currentInput);
        var tokens = parseResult.Tokens.ToList();

        if (tokens.Count == 0)
            return currentInput;

        int tokenIndex = suggestion.ParameterIndex + 1;
        
        while (tokens.Count <= tokenIndex)
            tokens.Add("");

        tokens[tokenIndex] = suggestion.Text;
        
        return string.Join(" ", tokens.Where(token => !string.IsNullOrEmpty(token))) + " ";
    }

    private static List<AutocompleteSuggestion> GetCommandSuggestions(string input, int maxResults)
    {
        var suggestions = new List<AutocompleteSuggestion>();

        foreach (var command in DevConsole.GetAllCommands().Values)
        {
            float score = CalculateFuzzyScore(input, command.FullName);
            if (score <= 0)
                continue;
            
            suggestions.Add(new AutocompleteSuggestion
            {
                Text = command.FullName,
                DisplayText = HighlightMatches(command.FullName, input),
                Description = command.Description,
                Score = score,
                CommandInfo = command,
                Type = AutocompleteSuggestionType.Command
            });
            
        }
        
        return [
            .. suggestions
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.Text)
                .Take(maxResults)
        ];
    }
    
    private static string HighlightMatches(string target, string input)
    {
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(input))
            return target;
        
        var matchIndices = GetMatchIndices(target, input);
        if (matchIndices.Count == 0)
            return target;

        var highlighted = "";
        bool inHighlight = false;

        for (int i = 0; i < target.Length; i++)
        {
            bool shouldHighlight = matchIndices.Contains(i);

            if (shouldHighlight && !inHighlight)
            {
                highlighted += "[color=yellow]";
                inHighlight = true;
            }
            else if (!shouldHighlight && inHighlight)
            {
                highlighted += "[/color]";
                inHighlight = false;
            }
            
            highlighted += target[i];
        }
        
        if (inHighlight)
            highlighted += "[/color]";
        
        return highlighted;
    }
    private static HashSet<int> GetMatchIndices(string target, string input)
    {
        var indices = new HashSet<int>();
        
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(input))
            return indices;

        if (string.Equals(target, input, StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < target.Length; i++)
                indices.Add(i);

            return indices;
        }

        if (target.StartsWith(input, StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < input.Length; i++)
                indices.Add(i);

            return indices;
        }

        int containsIndex = target.IndexOf(input, StringComparison.OrdinalIgnoreCase);
        if (containsIndex >= 0)
        {
            for (int i = containsIndex; i < containsIndex + input.Length; i++)
                indices.Add(i);

            return indices;
        }

        int inputIndex = 0;
        for (int targetIndex = 0; targetIndex < target.Length && inputIndex < input.Length; targetIndex++)
        {
            if (!CharsEqual(target[targetIndex], input[inputIndex]))
                continue;

            indices.Add(targetIndex);
            inputIndex++;
        }
        
        if (inputIndex == input.Length)
            indices.Clear();
        
        return indices;
    }
    
    private static bool CharsEqual(char left, char right)
    {
        return char.ToLowerInvariant(left) == char.ToLowerInvariant(right);
    }

    private static AutocompleteSuggestion CreateSignatureSuggestions(CommandInfo command, int currentArgIndex)
    {
        string signature = BuildSignature(command, currentArgIndex);
        string currentParamInfo = GetCurrentParameterInfo(command, currentArgIndex);

        return new AutocompleteSuggestion
        {
            Text = command.FullName,
            DisplayText = signature,
            Description = currentParamInfo,
            Score = EXACT_MATCH_BONUS,
            CommandInfo = command,
            Type = AutocompleteSuggestionType.Signature
        };
    }

    private static string BuildSignature(CommandInfo command, int highlightIndex)
    {
        if (command.Parameters.Length == 0)
            return command.FullName;
        
        var paramParts = new List<string>();

        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var param = command.Parameters[i];
            string typeName = param.ParameterType.GetFriendlyName();
            string paramName = param.Name ?? $"arg{i}";
            
            // Highlight the current parameter being typed
            if (i == highlightIndex)
            {
                string open = param.HasDefaultValue ? "[" : "<";
                string close = param.HasDefaultValue ? "]" : ">";
                paramParts.Add($"[color=yellow][b]{open}{typeName} {paramName}{close}[/b][/color]");
            }
            else
            {
                string formatted = param.HasDefaultValue 
                    ? $"[{typeName} {paramName}]" 
                    : $"<{typeName} {paramName}>";
                
                paramParts.Add(formatted);
            }
        }

        return $"{command.FullName} {string.Join(" ", paramParts)}";
    }

    private static string GetCurrentParameterInfo(CommandInfo command, int currentArgIndex)
    {
        if (currentArgIndex >= command.Parameters.Length)
            return "All parameters provided - press Enter to execute";
        
        var param = command.Parameters[currentArgIndex];
        string typeName = param.ParameterType.GetFriendlyName();
        string required = param.HasDefaultValue ? "Optional" : "Required";
        string defaultInfo = param.HasDefaultValue 
            ? $" (default: {CommandInfo.FormatDefaultValue(param.DefaultValue)})" 
            : "";

        return $"→ {param.Name}: {typeName} ({required}){defaultInfo}";
    }
    
    private static float CalculateFuzzyScore(string input, string target)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target))
            return 0f;
        
        if (string.Equals(target, input, StringComparison.OrdinalIgnoreCase))
            return EXACT_MATCH_BONUS;

        if (target.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            return PREFIX_MATCH_BONUS + input.Length;
        
        if (target.Contains(input, StringComparison.OrdinalIgnoreCase))
            return CONTAINS_MATCH_BONUS + input.Length;

        return CalculateSequenceScore(input, target);
    }
    
    private static float CalculateSequenceScore(string input, string target)
    {
        float score = 0f;
        int inputIndex = 0;
        int consecutiveMatches = 0;

        for (int targetIndex = 0; targetIndex < target.Length && inputIndex < input.Length; targetIndex++)
        {
            if (CharsEqual(target[targetIndex], input[inputIndex]))
            {
                consecutiveMatches++;
                score += SEQUENCE_MATCH_BONUS * consecutiveMatches;
                inputIndex++;
            }
            else
            {
                consecutiveMatches = 0;
            }
        }

        return inputIndex != input.Length ? 0f : score;

    }
}