using System;
using System.Collections.Generic;
using System.Linq;
namespace HWG.CommandConsole.Console;

public sealed record ConsoleInputParseResult
{
    public required string OriginalInput { get; init; }
    public required IReadOnlyList<string> Tokens { get; init; }
    public bool EndsWithWhitespace {get; init;}
    public bool HasUnclosedQuotes {get; init;}
    
    public string CommandName => Tokens.Count > 0 ? Tokens[0] : "";

    public IReadOnlyList<string> Arguments => Tokens.Count > 1 ? Tokens.Skip(1).ToArray() : [];

    public bool IsTypingCommandName => Tokens.Count == 0 || Tokens.Count == 1 && !EndsWithWhitespace;

    public int CurrentArgumentIndex
    { get {
        if (IsTypingCommandName)
            return -1;
        
        int argumentCount = Math.Max(0, Tokens.Count - 1);

        if (EndsWithWhitespace)
            return argumentCount;
        
        return Math.Max(0,  argumentCount - 1);
    }
    }

    public string CurrentToken
    { get {
        if (Tokens.Count == 0 || EndsWithWhitespace)
            return "";

        return Tokens[^1];
    }
    }
}

public static class ConsoleInputParser
{
    public static ConsoleInputParseResult Parse(string input)
    {
        input ??= "";

        var tokens = new List<string>();
        var current = "";
        bool inQuotes = false;
        bool escapeNext = false;
        bool tokenStarted = false;

        foreach (char character in input)
        {
            if (escapeNext)
            {
                current += character;
                tokenStarted = true;
                escapeNext = false;
                continue;
            }

            switch (character)
            {
                case '\\' when inQuotes:
                    escapeNext = true;
                    tokenStarted = true;
                    break;
                
                case '"':
                    inQuotes = !inQuotes;
                    tokenStarted = true;
                    break;
                
                case ' ' or '\t' when !inQuotes:
                    if (tokenStarted)
                    {
                        tokens.Add(current);
                        current = "";
                        tokenStarted = false;
                    }
                    break;
                
                default:
                    current += character;
                    tokenStarted = true;
                    break;
            }
        }
        
        if (tokenStarted)
            tokens.Add(current);

        return new ConsoleInputParseResult
        {
            OriginalInput = input,
            Tokens = tokens,
            EndsWithWhitespace = input.Length > 0 && char.IsWhiteSpace(input[^1]),
            HasUnclosedQuotes = inQuotes
        };
    }
}