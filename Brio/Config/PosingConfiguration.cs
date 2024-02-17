using Brio.Input;
using Dalamud.Game.ClientState.Keys;

namespace Brio.Config;

internal class PosingConfiguration
{
    // Overlay
    public bool OverlayDefaultsOn { get; set; } = false;
    public bool AllowGizmoAxisFlip { get; set; } = true;
    public float BoneCircleSize { get; set; } = 7f;
    public uint BoneCircleNormalColor { get; set; } = 0xFFFFFFFF;
    public uint BoneCircleInactiveColor { get; set; } = 0x55555555;
    public uint BoneCircleHoveredColor { get; set; } = 0xFF00FFFF;
    public uint BoneCircleSelectedColor { get; set; } = 0xFF0050FF;
    public float SkeletonLineThickness { get; set; } = 3f;
    public uint SkeletonLineActiveColor { get; set; } = 0xFFFFFFFF;
    public uint SkeletonLineInactiveColor { get; set; } = 0x55555555;
    public bool ShowSkeletonLines { get; set; } = true;
    public bool HideGizmoWhenAdvancedPosingOpen { get; set; } = false;
    public bool HideToolbarWhenAdvandedPosingOpen { get; set; } = false;
    public bool HideSkeletonWhenGizmoActive { get; set; } = false;

    // Graphical Posing
    public bool GraphicalSidesSwapped { get; set; } = false;
    public bool ShowGenitaliaInAdvancedPoseWindow { get; set; } = false;

    // Hooks
    public bool DisableGPoseMouseSelect { get; set; } = false;

    // Targeting
    public bool GPoseTargetChangesWithBrio { get; set; } = false;
    public bool BrioTargetChangesWithGPose { get; set; } = false;

    // Undo / Redo
    public int UndoStackSize { get; set; } = 50;
}
