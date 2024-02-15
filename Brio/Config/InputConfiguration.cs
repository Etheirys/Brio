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
        { KeyBindEvents.Interface_IncrementSmallModifier, new(VirtualKey.NO_KEY) },
        { KeyBindEvents.Interface_IncrementLargeModifier, new(VirtualKey.NO_KEY) },
        { KeyBindEvents.Posing_ToggleOverlay, new(VirtualKey.O, true) },
        { KeyBindEvents.Posing_Undo, new(VirtualKey.Z, true) },
        { KeyBindEvents.Posing_Redo, new(VirtualKey.Y, true) },

        { KeyBindEvents.Posing_DisableGizmo, new(VirtualKey.SHIFT) },
        { KeyBindEvents.Posing_DisableSkeleton, new(VirtualKey.CONTROL) },
        { KeyBindEvents.Posing_HideOverlay, new(VirtualKey.MENU) },
    };

    public bool ShowPromptsInGPose { get; set; } = true;
}
