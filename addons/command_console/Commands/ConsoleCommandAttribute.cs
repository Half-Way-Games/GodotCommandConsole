using System;
using System.Reflection;
namespace HWG.CommandConsole.Commands;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ConsoleCommandAttribute : Attribute
{
    public string Description = string.Empty;
    public string Prefix = string.Empty;
}

public sealed record CommandInfo
{
    public string          Name        { get; init; }
    public string          FullName    { get; init; }
    public string          Prefix      { get; init; }
    public string          Description { get; init; }
    public MethodInfo      Method      { get; init; }
    public ParameterInfo[] Parameters  { get; init; }
    
    public static string FormatDefaultValue(object value)
    {
        return value switch
        {
            null => "null",
            string text => $"\"{text}\"",
            char character => $"'{character}'",
            bool boolean => boolean ? "true" : "false",
            _ => value.ToString() ?? ""
        };
    }
}