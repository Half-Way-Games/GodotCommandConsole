using System;
using Godot;
namespace HWG.CommandConsole;

public static class TypeExtensions
{
    private static readonly Color DebugColor = new(0.7f, 0.7f, 0.7f);   // Light Gray
    private static readonly Color InfoColor = new(0.8f, 0.9f, 1.0f);    // Light Blue
    private static readonly Color WarningColor = new(1.0f, 0.9f, 0.4f); // Yellow
    private static readonly Color ErrorColor = new(1.0f, 0.5f, 0.5f);   // Light Red
    private static readonly Color CommandColor = new(0.6f, 1.0f, 0.6f); // Light Green

    private static readonly string DebugColorHex = DebugColor.ToHtml();
    private static readonly string InfoColorHex = InfoColor.ToHtml();
    private static readonly string WarningColorHex = WarningColor.ToHtml();
    private static readonly string ErrorColorHex = ErrorColor.ToHtml();
    private static readonly string CommandColorHex = CommandColor.ToHtml();

    public static string GetColor(this Log.LogLevel level)
    {
        return level switch
        {
            Log.LogLevel.Debug => DebugColorHex,
            Log.LogLevel.Info => InfoColorHex,
            Log.LogLevel.Warning => WarningColorHex,
            Log.LogLevel.Error => ErrorColorHex,
            Log.LogLevel.Command => CommandColorHex,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }
    
    public static string GetFriendlyName(this Type type)
    {
        // Handle common C# type aliases
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(long)) return "long";
        if (type == typeof(short)) return "short";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(char)) return "char";
        if (type == typeof(decimal)) return "decimal";
        
        // Handle Godot types (keep as-is)
        if (type == typeof(Vector2)) return "Vector2";
        if (type == typeof(Vector3)) return "Vector3";
        if (type == typeof(Color)) return "Color";
        
        // Default to the actual type name for custom types
        return type.Name;
    }
}