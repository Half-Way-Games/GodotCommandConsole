using Godot;
namespace HWG.CommandConsole;

public partial class DevConsoleUI : Control
{
    [Export] private LineEdit inputField;
    [Export] private RichTextLabel outputText;
    
    [Export] private ScrollContainer suggestionContainer;
    [Export] private VBoxContainer suggestionList;
    [Export] private PackedScene suggestionItemScene;
    
    [Export] private CheckBox debugToggle;
    [Export] private CheckBox infoToggle;
    [Export] private CheckBox warningToggle;
    [Export] private CheckBox errorToggle;
    [Export] private Button clearButton;
    
    
}