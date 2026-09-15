global using Log = HWG.CommandConsole.Console.DevConsoleLogger;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;
namespace HWG.CommandConsole.Console;


public static class DevConsoleLogger
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Command
    }
    
    public struct LogEntry
    {
        public DateTime Timestamp { get; init; }
        public LogLevel Level     { get; init; }
        public string   Message   { get; init; }
        public string   Caller    { get;  init; }
    }
    
    public static event Action<LogEntry> MessageLogged;

    public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(LogLevel.Debug, message, memberName, sourceFilePath, sourceLineNumber);
    }
    
    public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(LogLevel.Info, message, memberName, sourceFilePath, sourceLineNumber);
    }
    
    public static void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(LogLevel.Warning, message, memberName, sourceFilePath, sourceLineNumber);
    }

    public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(LogLevel.Error, message, memberName, sourceFilePath, sourceLineNumber);
    }
    
    public static void Command(string message, string category = "COMMAND")
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Command,
            Message = message,
            Caller = category,
        };
        MessageLogged?.Invoke(entry);
    }
    
    private static void LogMessage(LogLevel level, string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
    {
        var className = Path.GetFileNameWithoutExtension(sourceFilePath);
        var caller = $"{className}.{memberName}:{sourceLineNumber}";

        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            Caller = caller,
        };
        
        MessageLogged?.Invoke(entry);
        
        // Also send to Godot's console for development and debugging purposes
        switch (level)
        {
            
            case LogLevel.Debug:
            case LogLevel.Info:
                GD.Print($"[{entry.Timestamp:HH:mm:ss}][{caller}] {message}");
                break;
            case LogLevel.Warning:
                GD.PrintErr($"[{entry.Timestamp:HH:mm:ss}][{caller}] WARNING: {message}");
                break;
            case LogLevel.Error:
                GD.PrintErr($"[{entry.Timestamp:HH:mm:ss}][{caller}] ERROR: {message}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public static string FormatLogEntry(LogEntry entry, bool includeTimestamp = true)
    {
        var colorHex = entry.Level.GetColor();
        string timestamp = includeTimestamp ? $"[{entry.Timestamp:HH:mm:ss}] " : "";
        string caller = entry.Level == LogLevel.Command ? "" : $"[{entry.Caller}] ";
        return $"[color={colorHex}]{timestamp}{caller} {entry.Message}[/color]";
    }
}