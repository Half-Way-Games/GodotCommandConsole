using HWG.CommandConsole.Commands;
namespace HWG.CommandConsole.Autocomplete;

public enum AutocompleteSuggestionType
{
    Command,
    Signature,
    ParameterValue
}

public readonly record struct AutocompleteSuggestion (string Text, string DisplayText, string Description, float Score, CommandInfo CommandInfo, AutocompleteSuggestionType Type, int ParameterIndex);