#if TOOLS
using Godot;
using Godot.Collections;
namespace HWG.CommandConsole;

[Tool]
public partial class Plugin : EditorPlugin
{
    private static readonly StringName ConsoleAction = "dev_console_toggle";

    public override void _EnterTree()
    {
        if (!InputMap.HasAction(ConsoleAction))
        {
            var inputEvent = new InputEventKey
            {
                Keycode = Key.Quoteleft
            };
            
            InputMap.AddAction(ConsoleAction);
            InputMap.ActionAddEvent(ConsoleAction, inputEvent);

            var actionSettings = new Dictionary
            {
                ["deadzone"] = 0.2f,
                ["events"] = new Array<InputEvent> { inputEvent }
            };

            ProjectSettings.SetSetting("input/" + ConsoleAction, actionSettings);
            Log.Info($"Command console added custom input action '{ConsoleAction}' bound to '`'");
        }
        
        AddAutoloadSingleton("DevConsole", "res://addons/command_console/scenes/DevConsole.tscn");
    }

    public override void _ExitTree()
    {
        RemoveAutoloadSingleton("DevConsole");
    }
}
#endif