﻿namespace Brio.Input;
public enum InputAction
{
    // Interface
    Interface_ToggleBrioWindow,
    Interface_ToggleBindPromptWindow,
    Interface_IncrementSmallModifier,
    Interface_IncrementLargeModifier,
    Interface_StopCutscene,
    Interface_StartAllActorsAnimations,
    Interface_StopAllActorsAnimations,

    // Posing
    Posing_ToggleOverlay,
    Posing_Undo,
    Posing_Redo,
    Posing_Esc,
    Posing_DisableGizmo,
    Posing_DisableSkeleton,
    Posing_HideOverlay,
    Posing_Translate,
    Posing_Rotate,
    Posing_Scale,
    Posing_Universal,

    // Free Camera
    FreeCamera_Forward,
    FreeCamera_Backward,
    FreeCamera_Left,
    FreeCamera_Right,
    FreeCamera_Up,
    FreeCamera_UpAlt,
    FreeCamera_Down,
    FreeCamera_DownAlt,
    FreeCamera_IncreaseCamMovement,
    FreeCamera_DecreaseCamMovement
}

public enum InputOverlayAction
{
    Interface_ToggleBrioWindow,
    Interface_ToggleBindPromptWindow,
    Interface_IncrementSmallModifier,
    Interface_IncrementLargeModifier,
    Interface_StopCutscene,
    Interface_StartAllActorsAnimations,
    Interface_StopAllActorsAnimations
}
