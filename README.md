# Godot C# Command Console
A reflection-based command console add-on for Godot that allows developers to register custom commands and logs for use at runtime.

<img width="1280" height="436" alt="ExampleCommandUsage" src="https://github.com/user-attachments/assets/7e133aaf-ba39-41c5-8ee6-bedd5b592408" /> 


> [!NOTE]
> This plugin only works with C# scripts and therefore requires the .NET version of Godot. Built with Godot 4.7, it should also be compatible with earlier Godot 4.x versions.

## Installation and Setup
1. Download the latest release (or clone the `main` branch).
2. Move the `addons/` folder into the root (`res://`) directory of your Godot project.
3. Build/compile your C# solution in Godot or your IDE.
4. Enable the plugin in Godot via **Project -> Project Settings -> Plugins -> Command Console**

> [!WARNING]
> Attempting to enable the plugin before building the C# project will cause Godot to throw an error and automatically disable it.

### Updating the Plugin
To update to a new version, delete the existing `addons/command_console/` folder before copying over the new files. Custom settings are saved to your project settings file and will be retained across updates.

## Adding a Command
To create a new command function, import the namespace into your script to gain access to the console command attribute:
```cs
using HWG.CommandConsole.Commands;
```

Decorate any `static` method with the `[ConsoleCommand]` attribute.
- **Prefix** (optional): Groups related commands and prevents naming collisions (e.g., both `Player.TakeDamage` and `Enemy.TakeDamage` can exist simultaneously). Commands with a prefix are invoked using `Prefix.MethodName`.
- **Description** (optional): Provides helpful context in the console ui regarding what the command does.

```cs
[ConsoleCommand(Prefix = "Player", Description = "Revives the local player")]
private static void Revive()
{
    LocalPlayer.ReviveCharacter();
}
```
_Executed in console via:_ `Player.Revive`

### Commands With Arguments
Command methods can accept parameters, which will display in the autocomplete suggestion bar. Pass arguments by separating them with spaces:
```cs
[ConsoleCommand(Prefix = "Player", Description = "Deal damage to the Local Player")]
private static void TakeDamage(float damage)
{
    LocalPlayer.Resources.TakeDamage(damage);
}
```
_Executed in console via:_ `Player.TakeDamage 25.4`

> [!TIP]
> Command methods can have any visibility modifier (`public`, `private`, `internal`) and will still be automatically discovered during reflection registration.

## Using the Log
Write to the console log from anywhere in your project using the global `Log` alias alongside the desired log level (`Debug`, `Info`, `Warning`, or `Error`). Log outputs display in both the runtime command console and Godot's built-in output panel via `GD.Print`.
```cs
Log.Info($"Player health set to: {health}");
Log.Error("Failed to load save file.");
```
Each log call automatically captures the caller method and line number to simplify debugging.

## Command Console Runtime
When enabled, the plugin automatically registers an Autoload scene and binds a custom input action (`dev_console_toggle`) to the backtick/grave accent key (`) by default.
- **Autocomplete:** Typing in the input box opens a list of matching commands sorted by match score and name. Navigate suggestions using the **Up**/**Down** arrow keys and complete them with **Tab** or **Right Arrow**.
- **History:** Pressing **Up**/**Down** on an empty input box navigates previously executed commands.
- **Execute:** Once a command is complete and arguments are set, execute a command with the **Enter**/**Return** key.

### Filtering Logs
Filter console entries by log level (`Debug`, `Info`, `Warning`, or `Error`) using the toggles on the right side of the panel: 

<img width="146" height="233" alt="log filters" src="https://github.com/user-attachments/assets/681c1f24-b9f0-4998-9014-05b461088e52" />

### Adjusting UI Rendering (Z-Index)
The command console renders with a `Z-Index` of `1000` to sit above existing UI elements. To adjust draw order, modify the `Z-Index` on the `ConsoleUI` node inside `res://addons/command_console/scenes/DevConsole.tscn`
