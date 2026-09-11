using Godot;
namespace HWG.CommandConsole.Autocomplete;

public partial class SuggestionItemUI : PanelContainer
{
    [Export] private PanelContainer container;
    [Export] private RichTextLabel mainLabel;
    [Export] private RichTextLabel descriptionLabel;
    private static readonly StyleBoxFlat selectedStyle = new()
        { BgColor = new Color(0.3f, 0.3f, 0.7f, 0.5f) };
    private static readonly StyleBoxFlat normalStyle = new();
    
    private static readonly StringName panel = "panel";

    public void SetSelected(bool isSelected)
    {
        container.AddThemeStyleboxOverride(panel, isSelected ? selectedStyle :  normalStyle);
    }

    public void SetLabelText(string text)
    {
        mainLabel.SetText(text);
    }
    
    public void SetDescriptionText(string text)
    {
        descriptionLabel.SetText(text);
    }
}