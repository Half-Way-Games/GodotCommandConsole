using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HWG.CommandConsole.Commands;
namespace HWG.CommandConsole.Console;

public static class DevConsole
{
    private static readonly Dictionary<string, CommandInfo> commands = new();

    public static void RegisterAllCommands()
    {
        // Get all loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            // Skip system, test platform, and compiler assemblies to maximize performance and avoid type load errors
           if (ShouldSkipAssembly(assembly))
               continue;
            
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                    RegisterCommandsFromType(type);
            }
            catch (ReflectionTypeLoadException exception)
            {
                foreach (var loadedType in exception.Types.Where(t => t != null))
                    RegisterCommandsFromType(loadedType);
            }
            catch (Exception e)
            {
                // Safely skip any assembly that cannot be inspected
                Log.Error($"Error registering commands for '{assembly.GetName().Name}': {e.Message}");
            }
        }
        
        Log.Info($"Registered {commands.Count} commands");
    }

    public static bool ExecuteCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var parseResult = ConsoleInputParser.Parse(input);
        
        if (parseResult.Tokens.Count == 0)
            return false;
        
        var commandName = parseResult.CommandName.ToLower();
        var args = parseResult.Arguments.ToArray();
        
        if (commands.TryGetValue(commandName, out var command))
            return InvokeCommand(command, args);

        Log.Error($"Unknown Command: {commandName}");
        return false;
    }
    
    public static Dictionary<string, CommandInfo> GetAllCommands()
    {
        return commands;
    }

    public static CommandInfo GetCommand(string name)
    {
        return commands.GetValueOrDefault(name.ToLower());
    }

    private static bool ShouldSkipAssembly(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name;

        if (string.IsNullOrWhiteSpace(assemblyName))
            return true;
        
        if (IsKnownFrameworkOrAssembly(assemblyName))
            return true;
       
        return !CouldContainConsoleCommands(assembly);
    }

    private static bool IsKnownFrameworkOrAssembly(string assemblyName)
    {
        string[] exactNames =
        [
            "mscorlib",
            "netstandard",
            "GodotSharp",
            "GdUnit4",
            "gdUnit4.api",
            "gdUnit4.test.adapter",
            "Microsoft.NET.Test.Sdk"
        ];

        string[] dottedPrefixes =
        [
            "System.",
            "Microsoft.",
            "Godot.",
            "GodotSharp.",
            "GdUnit4.",
            "gdUnit4."
        ];

        return exactNames.Contains(assemblyName, StringComparer.OrdinalIgnoreCase)
            || dottedPrefixes.Any(prefix => assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool CouldContainConsoleCommands(Assembly assembly)
    {
        var commandAssemblyName = typeof(ConsoleCommandAttribute).Assembly.GetName().Name;
        var currentAssemblyName = assembly.GetName().Name;
        
        return string.Equals(currentAssemblyName, commandAssemblyName, StringComparison.OrdinalIgnoreCase) 
            || assembly.GetReferencedAssemblies().Any(reference => string.Equals(reference.Name, commandAssemblyName, StringComparison.OrdinalIgnoreCase));
    }
    
    private static void RegisterCommandsFromType(Type type)
    {
        try
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var method in methods)
            {
                ConsoleCommandAttribute attribute;
                try
                {
                    attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                }
                catch (Exception e)
                {
                    // Skip methods whose attributes or parameter types cannot be loaded
                    Log.Warning($"Failed to get attribute  for '{type.FullName}': {e.Message}");
                    continue;
                }

                if (attribute == null) continue;
            
                var commandInfo = new CommandInfo
                {
                    Name = method.Name,
                    Description = attribute.Description,
                    Prefix = attribute.Prefix,
                    Method = method,
                    Parameters = method.GetParameters(),
                    FullName = string.IsNullOrEmpty(attribute.Prefix) ? method.Name : $"{attribute.Prefix}.{method.Name}"
                };

                var key = commandInfo.FullName.ToLower();
                if (commands.TryAdd(key, commandInfo))
                    continue;

                Log.Warning($"Duplicate command: '{commandInfo.FullName}' ignored");
            }
        }
        catch (Exception e)
        {
            // Safely skip types that fail method reflection
            Log.Error($"Error registering commands for '{type.FullName}': {e.Message}");
        }
    }

    private static bool InvokeCommand(CommandInfo command, string[] args)
    {
        try
        {
            var parameters = command.Parameters;
            var convertedArgs = new object[parameters.Length];
            
            // Convert string arguments to appropriate types
            for (var i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length)
                {
                    convertedArgs[i] = ConvertArgument(args[i], parameters[i].ParameterType);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    convertedArgs[i] = parameters[i].DefaultValue;
                }
                else
                {
                    Log.Error($"Missing required parameter: {parameters[i].Name}");
                    PrintCommandUsage(command);
                    return false;
                }
            }
            
            var result = command.Method.Invoke(null, convertedArgs);

            if (command.Method.ReturnType != typeof(void) && result != null)
                Log.Info($"→ {result}");

            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Error executing '{command.FullName}': {e.Message}");
            return false;
        }
    }

    private static object ConvertArgument(string arg, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return arg.Trim('"'); // Remove any quotes if present
            if (targetType == typeof(int))
                return int.Parse(arg);
            if (targetType == typeof(float))
                return float.Parse(arg);
            if (targetType == typeof(double))
                return double.Parse(arg);
            if (targetType == typeof(bool))
                return bool.Parse(arg);
            if (targetType == typeof(Vector2))
            {
                var parts = arg.Split(',');
                return parts.Length != 2 ? throw new ArgumentException("Vector2 format: x,y") : new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }
            if (targetType == typeof(Vector3))
            {
                var parts = arg.Split(',');
                return parts.Length != 3 ? throw new ArgumentException("Vector3 format: x,y,z") : new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }
            return Convert.ChangeType(arg, targetType);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Cannot convert '{arg}' to {targetType.Name}: {e.Message}", e);
        }
    }
    
    private static void PrintCommandUsage(CommandInfo command)
    {
        var usage = command.FullName;
        if (command.Parameters.Length > 0)
        {
            var parameterStrings = command.Parameters.Select(p => p.HasDefaultValue ? $"[{p.Name}]" : $"<{p.Name}>");
            usage += $" {string.Join(" ", parameterStrings)}";
        }

        Log.Info($"Usage: {usage}");
        if (!string.IsNullOrEmpty(command.Description))
            Log.Info($"\t{command.Description}");
    }
}