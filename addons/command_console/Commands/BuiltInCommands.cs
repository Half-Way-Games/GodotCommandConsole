using System;
using System.Linq;
using System.Text;
using HWG.CommandConsole.Console;
namespace HWG.CommandConsole.Commands;

public static class BuiltInCommands
{
    private static readonly StringBuilder output = new();
    
    [ConsoleCommand(Description = "Lists all available commands")]
    public static void Help(string filter = "")
    {
        output.Clear();
        var commands = DevConsole.GetAllCommands();
        bool hasFilter = !string.IsNullOrWhiteSpace(filter);

        if (!hasFilter)
        {
            output.AppendLine($"Available commands ({commands.Count})");
            output.AppendLine("Type 'help <search>' for commands matching that search");
            output.AppendLine("─────────────────────────────────────");
        }
        
        var sortedCommands = commands.Values.OrderBy(c => c.FullName);

        int matchCount = 0;
        foreach (var command in sortedCommands)
        {
            if (hasFilter)
            {
                if (!command.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase) && !command.Description.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            string paramInfo = "";
            if (command.Parameters.Length > 0)
            {
                var paramStrings = command.Parameters.Select(p => $"{p.ParameterType.GetFriendlyName()} {p.Name}" + (p.HasDefaultValue ? $" = {CommandInfo.FormatDefaultValue(p.DefaultValue)}" : ""));
                paramInfo = $" ({string.Join(", ", paramStrings)})";
            }
            string description = !string.IsNullOrWhiteSpace(command.Description) 
                ? $" → {command.Description}" 
                : "";
            
            output.AppendLine($"\t{command.FullName}{paramInfo}{description}");
            matchCount++;
        }

        output.AppendLine("─────────────────────────────────────");
        if (hasFilter)
        {
            output.AppendLine($"Found {matchCount} matching command(s) for '{filter}'");
        }
        Log.Info(output.ToString());
    }

    [ConsoleCommand(Description = "Clears the console window")]
    public static void Clear()
    {
        DevConsoleUI.Instance.Clear();
    }
}