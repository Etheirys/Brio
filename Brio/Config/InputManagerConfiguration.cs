using System.Collections.Generic;
using Brio.Input;
using Dalamud.Game.ClientState.Keys;

namespace Brio.Config;

public class InputManagerConfiguration
{
    protected static readonly Dictionary<InputAction, KeyConfig> _defaultKeyBindings = new()
    {
        { InputAction.Interface_ToggleBrioWindow, new KeyConfig(VirtualKey.B, false, true, false) },
        { InputAction.Interface_ToggleBindPromptWindow, new KeyConfig(VirtualKey.M, false, true, false) },
        { InputAction.Interface_IncrementSmallModifier, new KeyConfig(VirtualKey.CONTROL) },
        { InputAction.Interface_IncrementLargeModifier, new KeyConfig(VirtualKey.SHIFT) },
        { InputAction.Interface_StopCutscene, new KeyConfig(VirtualKey.B, true) },
        { InputAction.Interface_StartAllActorsAnimations, new KeyConfig(VirtualKey.N, true) },
        { InputAction.Interface_StopAllActorsAnimations, new KeyConfig(VirtualKey.M, true) },

        { InputAction.Posing_ToggleOverlay, new KeyConfig(VirtualKey.O, false, true, false) },
        { InputAction.Posing_Undo, new KeyConfig(VirtualKey.Z, false, true, false) },
        { InputAction.Posing_Redo, new KeyConfig(VirtualKey.Y, false, true, false) },
        { InputAction.Posing_Esc, new KeyConfig(VirtualKey.ESCAPE) },
        { InputAction.Posing_DisableGizmo, new KeyConfig(VirtualKey.SHIFT) },
        { InputAction.Posing_DisableSkeleton, new KeyConfig(VirtualKey.CONTROL) },
        { InputAction.Posing_HideOverlay, new KeyConfig(VirtualKey.MENU) },
        { InputAction.Posing_Translate, new KeyConfig(VirtualKey.NO_KEY) },
        { InputAction.Posing_Rotate, new KeyConfig(VirtualKey.NO_KEY) },
        { InputAction.Posing_Scale, new KeyConfig(VirtualKey.NO_KEY) },

        { InputAction.FreeCamera_Forward, new KeyConfig(VirtualKey.W) },
        { InputAction.FreeCamera_Backward, new KeyConfig(VirtualKey.S) },
        { InputAction.FreeCamera_Left, new KeyConfig(VirtualKey.A) },
        { InputAction.FreeCamera_Right, new KeyConfig(VirtualKey.D) },
        { InputAction.FreeCamera_Up, new KeyConfig(VirtualKey.SPACE) },
        { InputAction.FreeCamera_UpAlt, new KeyConfig(VirtualKey.Q) },
        { InputAction.FreeCamera_Down, new KeyConfig(VirtualKey.CONTROL) },
        { InputAction.FreeCamera_DownAlt, new KeyConfig(VirtualKey.E) },
        { InputAction.FreeCamera_IncreaseCamMovement, new KeyConfig(VirtualKey.SHIFT) },
        { InputAction.FreeCamera_DecreaseCamMovement, new KeyConfig(VirtualKey.MENU) }
    };
    public Dictionary<InputAction, KeyConfig> KeyBindings { get; set; } = new()
    {
        { InputAction.Interface_ToggleBrioWindow, new KeyConfig(VirtualKey.B, false, true, false) },
        { InputAction.Interface_ToggleBindPromptWindow, new KeyConfig(VirtualKey.M, false, true, false) },
        { InputAction.Interface_IncrementSmallModifier, new KeyConfig(VirtualKey.CONTROL) },
        { InputAction.Interface_IncrementLargeModifier, new KeyConfig(VirtualKey.SHIFT) },
        { InputAction.Interface_StopCutscene, new KeyConfig(VirtualKey.B, true) },
        { InputAction.Interface_StartAllActorsAnimations, new KeyConfig(VirtualKey.N, true) },
        { InputAction.Interface_StopAllActorsAnimations, new KeyConfig(VirtualKey.M, true) },

        { InputAction.Posing_ToggleOverlay, new KeyConfig(VirtualKey.O, false, true, false) },
        { InputAction.Posing_Undo, new KeyConfig(VirtualKey.Z, false, true, false) },
        { InputAction.Posing_Redo, new KeyConfig(VirtualKey.Y, false, true, false) },
        { InputAction.Posing_Esc, new KeyConfig(VirtualKey.ESCAPE) },
        { InputAction.Posing_DisableGizmo, new KeyConfig(VirtualKey.SHIFT) },
        { InputAction.Posing_DisableSkeleton, new KeyConfig(VirtualKey.CONTROL) },
        { InputAction.Posing_HideOverlay, new KeyConfig(VirtualKey.MENU) },
        { InputAction.Posing_Translate, new KeyConfig(VirtualKey.NO_KEY) },
        { InputAction.Posing_Rotate, new KeyConfig(VirtualKey.NO_KEY) },
        { InputAction.Posing_Scale, new KeyConfig(VirtualKey.NO_KEY) },
        { InputAction.Posing_Universal, new KeyConfig(VirtualKey.NO_KEY) },

        { InputAction.FreeCamera_Forward, new KeyConfig(VirtualKey.W) },
        { InputAction.FreeCamera_Backward, new KeyConfig(VirtualKey.S) },
        { InputAction.FreeCamera_Left, new KeyConfig(VirtualKey.A) },
        { InputAction.FreeCamera_Right, new KeyConfig(VirtualKey.D) },
        { InputAction.FreeCamera_Up, new KeyConfig(VirtualKey.SPACE) },
        { InputAction.FreeCamera_UpAlt, new KeyConfig(VirtualKey.Q) },
        { InputAction.FreeCamera_Down, new KeyConfig(VirtualKey.CONTROL) },
        { InputAction.FreeCamera_DownAlt, new KeyConfig(VirtualKey.E) },
        { InputAction.FreeCamera_IncreaseCamMovement, new KeyConfig(VirtualKey.SHIFT) },
        { InputAction.FreeCamera_DecreaseCamMovement, new KeyConfig(VirtualKey.MENU) }
    };

    public bool Enable { get; set; } = true;
    public bool ShowPromptsInGPose { get; set; } = false;
    public bool EnableKeyHandlingOnKeyMod { get; set; } = true;
    public bool FlipKeybindsPastNinety { get; set; } = true;

    public void ResetKeyToDefault(InputAction action)
    {
        if(KeyBindings.TryGetValue(action, out KeyConfig? value))
        {
            value.key = _defaultKeyBindings[action].key;
            value.requireShift = _defaultKeyBindings[action].requireShift;
            value.requireCtrl = _defaultKeyBindings[action].requireCtrl;
            value.requireAlt = _defaultKeyBindings[action].requireAlt;
        }
    }
}
