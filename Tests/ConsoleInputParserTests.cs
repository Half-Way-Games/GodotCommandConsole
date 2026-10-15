using GdUnit4;
using HWG.CommandConsole.Console;
namespace CommandConsole.Tests;

[TestSuite]
public class ConsoleInputParserTests
{
    public void Parse_SimpleCommand_ReturnsCorrectTokens()
    {
        string input = "spawn enemy 5";
        
        ConsoleInputParseResult result = ConsoleInputParser.Parse(input);
        
        Assertions.AssertString(result.CommandName).IsEqual("spawn");
        Assertions.AssertArray(result.Arguments).ContainsExactly("enemy", "5");
        Assertions.AssertBool(result.EndsWithWhitespace).IsFalse();
        Assertions.AssertBool(result.HasUnclosedQuotes).IsFalse();
    }

    [TestCase]
    public void Parse_QuotedArguments_PreservesSpacesInsideQuotes()
    {
        string input = "say \"hello world\" 42";

        ConsoleInputParseResult result = ConsoleInputParser.Parse(input);

        Assertions.AssertString(result.CommandName).IsEqual("say");
        Assertions.AssertArray(result.Arguments).ContainsExactly("hello world", "42");
        Assertions.AssertBool(result.HasUnclosedQuotes).IsFalse();
    }

    [TestCase]
    public void Parse_UnclosedQuotes_SetsFlagCorrectly()
    {
        string input = "say \"incomplete string";

        ConsoleInputParseResult result = ConsoleInputParser.Parse(input);

        Assertions.AssertBool(result.HasUnclosedQuotes).IsTrue();
        Assertions.AssertString(result.CommandName).IsEqual("say");
    }

    [TestCase("help ", true)]
    [TestCase("help", false)]
    public void Parse_EndsWithWhitespace_SetsFlagCorrectly(string input, bool expectedFlag)
    {
        ConsoleInputParseResult result = ConsoleInputParser.Parse(input);

        Assertions.AssertBool(result.EndsWithWhitespace).IsEqual(expectedFlag);
        Assertions.AssertString(result.CommandName).IsEqual("help");
    }

    [TestCase("", "", -1)]
    [TestCase("help", "help", -1)]
    [TestCase("help ", "help" , 0)]
    [TestCase("give player item", "give", 1)]
    [TestCase("give player item ", "give", 2)]
    public void Parse_CurrentArgumentIndex_CalculatesCorrectly(string input, string expectedCommand, int expectedArgIndex)
    {
        ConsoleInputParseResult result = ConsoleInputParser.Parse(input);

        Assertions.AssertString(result.CommandName).IsEqual(expectedCommand);
        Assertions.AssertInt(result.CurrentArgumentIndex).IsEqual(expectedArgIndex);
    }
}