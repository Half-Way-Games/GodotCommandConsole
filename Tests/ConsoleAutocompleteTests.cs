using System.Collections.Generic;
using GdUnit4;
using HWG.CommandConsole.Autocomplete;
using HWG.CommandConsole.Commands;
using HWG.CommandConsole.Console;
namespace CommandConsole.Tests;

[TestSuite]
public class ConsoleAutocompleteTests
{
    private static class DummyTestCommands
    {
        [ConsoleCommand(Description = "Restores player health")]
        public static void Heal(int amount = 100) { }
        
        [ConsoleCommand(Description = "Restores health for all entities")]
        public static void HealAll() { }

        [ConsoleCommand(Description = "Kills target entity")]
        public static void Kill() { }

        [ConsoleCommand(Prefix = "player", Description = "Spawns a player")]
        public static void Spawn(string name, int x, int y) { }
    }

    [Before]
    public void SetupSuite()
    {
        Log.LogToGodotConsole(false);
        DevConsole.RegisterAllCommands();
    }

    [TestCase("hea", 2)]
    [TestCase("heal", 2)]
    [TestCase("kill", 1)]
    [TestCase("xyz_nonexistent", 0)]
    public void FillSuggestions_ReturnsExpectedMatchCount(string input, int expectedCount)
    {
        var results = new List<AutocompleteSuggestion>();
        
        ConsoleAutocomplete.FillSuggestions(input, results);

        Assertions.AssertInt(results.Count).IsEqual(expectedCount);
    }

    [TestCase]
    public void FillSuggestions_ExactMatch_RanksHigherThanPrefixMatch()
    {
        var results = new List<AutocompleteSuggestion>();
        
        ConsoleAutocomplete.FillSuggestions("heal", results);

        Assertions.AssertString(results[0].Text).IsEqual("Heal");
        Assertions.AssertString(results[1].Text).IsEqual("HealAll");
    }
}