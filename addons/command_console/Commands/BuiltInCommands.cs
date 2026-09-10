using System;
using System.Linq;
using HWG.CommandConsole.Console;
namespace HWG.CommandConsole.Commands;

public static class BuiltInCommands
{
    [ConsoleCommand(Description = "Lists all available commands")]
    public static void Help(string filter = "")
    {
        var commands = DevConsole.GetAllCommands();
        bool hasFilter = !string.IsNullOrWhiteSpace(filter);

        if (!hasFilter)
        {
            Log.Info($"Available commands ({commands.Count})");
            Log.Info("Type 'help <search>' for commands matching that search");
            Log.Info("─────────────────────────────────────");
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
            
            Log.Info($"\t{command.FullName}{paramInfo}{description}");
            matchCount++;
        }

        Log.Info("─────────────────────────────────────");
        if (hasFilter)
        {
            Log.Info($"Found {matchCount} matching command(s) for '{filter}'");
        }
    }

    [ConsoleCommand(Description = "Clears the console window")]
    public static void Clear()
    {
        // TODO: Use the singleton reference to the console window UI
    }
}