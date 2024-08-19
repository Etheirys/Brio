using Brio.Input;
using Dalamud.Game.ClientState.Keys;
using System.Collections.Generic;

namespace Brio.Config;

internal class InputConfiguration
{
    public Dictionary<KeyBindEvents, KeyBind> Bindings { get; set; } = new()
    {
        // Default bindings
        { KeyBindEvents.Interface_ToggleBrioWindow, new(VirtualKey.B, true) },
        { KeyBindEvents.Interface_ToggleBindPromptWindow, new(VirtualKey.M, true) },
        { KeyBindEvents.Interface_IncrementSmallModifier, new(VirtualKey.CONTROL) },
        { KeyBindEvents.Interface_IncrementLargeModifier, new(VirtualKey.SHIFT) },
        { KeyBindEvents.Posing_ToggleOverlay, new(VirtualKey.O, true) },
        { KeyBindEvents.Posing_Undo, new(VirtualKey.Z, true) },
        { KeyBindEvents.Posing_Redo, new(VirtualKey.Y, true) },

        { KeyBindEvents.Posing_DisableGizmo, new(VirtualKey.SHIFT) },
        { KeyBindEvents.Posing_DisableSkeleton, new(VirtualKey.CONTROL) },
        { KeyBindEvents.Posing_HideOverlay, new(VirtualKey.MENU) },
        { KeyBindEvents.Posing_Translate, new(VirtualKey.NO_KEY) },
        { KeyBindEvents.Posing_Rotate, new(VirtualKey.NO_KEY) },
        { KeyBindEvents.Posing_Scale, new(VirtualKey.NO_KEY) },
        { KeyBindEvents.Interface_StopCutscene, new (VirtualKey.B, shift: true) },
        { KeyBindEvents.Interface_StartAllActorsAnimations, new (VirtualKey.N, shift: true) },
        { KeyBindEvents.Interface_StopAllActorsAnimations, new (VirtualKey.M, shift: true) }
    };

    public bool ShowPromptsInGPose { get; set; } = false;

    public bool EnableKeybinds { get; set; } = true;
}
