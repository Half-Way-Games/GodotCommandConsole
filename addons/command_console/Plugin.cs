#if TOOLS
using Godot;
using Godot.Collections;
namespace HWG.CommandConsole;

[Tool]
public partial class Plugin : EditorPlugin
{
    private static readonly StringName ConsoleAction = "DevConsole";

    public override void _EnterTree()
    {
        if (!InputMap.HasAction(ConsoleAction))
        {
            InputMap.AddAction(ConsoleAction);
            var inputEvent = new InputEventKey
            {
                Keycode = Key.Quoteleft
            };
            InputMap.ActionAddEvent(ConsoleAction, inputEvent);

            var actionSettings = new Dictionary
            {
                ["deadzone"] = 0.2f,
                ["events"] = new Array<InputEvent> { inputEvent }
            };

            ProjectSettings.SetSetting("input/" + ConsoleAction, actionSettings);
            ProjectSettings.Save();
            Log.Info($"Command console added custom input action '{ConsoleAction}' bound to '`'");
        }
        
        AddAutoloadSingleton("DevConsole", "res://addons/command_console/Scenes/DevConsole.tscn");
    }

    public override void _ExitTree()
    {
        if (InputMap.HasAction(ConsoleAction))
        {
            InputMap.EraseAction(ConsoleAction);
            ProjectSettings.SetSetting("input/" + ConsoleAction, new Variant());
            ProjectSettings.Save();
            
            Log.Info($"Command console removed custom input action '{ConsoleAction}' bound to '`'");
        }
        
        RemoveAutoloadSingleton("DevConsole");
    }
}
#endif