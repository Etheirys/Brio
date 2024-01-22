using ImGuiNET;

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
    public ImGuiKey DisableGizmoHotkey { get; set; } = ImGuiKey.LeftShift;
    public ImGuiKey DisableSkeletonHotkey { get; set; } = ImGuiKey.LeftCtrl;
    public ImGuiKey HideOverlayHotkey { get; set; } = ImGuiKey.LeftAlt;

    // Graphical Posing
    public bool GraphicalSidesSwapped { get; set; } = false;

    // Hooks
    public bool DisableGPoseMouseSelect { get; set; } = false;

    // Targeting
    public bool GPoseTargetChangesWithBrio { get; set; } = false;
    public bool BrioTargetChangesWithGPose { get; set; } = false;

    // Undo / Redo
    public int UndoStackSize { get; set; } = 50;
}
