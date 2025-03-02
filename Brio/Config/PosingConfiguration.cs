﻿namespace Brio.Config;

public class PosingConfiguration
{
    // Overlay
    public bool OverlayDefaultsOn { get; set; } = false;
    public bool AllowGizmoAxisFlip { get; set; } = true;
    public float BoneCircleSize { get; set; } = 6.300f;
    public uint BoneCircleNormalColor { get; set; } = 0xFFFFFFFF;
    public uint BoneCircleInactiveColor { get; set; } = 0x55555555;
    public uint BoneCircleHoveredColor { get; set; } = 0xFFFF0073;
    public uint BoneCircleSelectedColor { get; set; } = 0xFFF82B56;
    public float SkeletonLineThickness { get; set; } = 0.010f;
    public uint SkeletonLineActiveColor { get; set; } = 0xFFFFFFFF;
    public uint SkeletonLineInactiveColor { get; set; } = 0x55555555;
    public bool ShowSkeletonLines { get; set; } = true;
    public bool HideGizmoWhenAdvancedPosingOpen { get; set; } = false;
    public bool HideToolbarWhenAdvandedPosingOpen { get; set; } = false;
    public bool HideSkeletonWhenGizmoActive { get; set; } = false;

    public bool ModelTransformStandout { get; set; } = true;
    public uint ModelTransformCircleStandOutColor { get; set; } = 0xFFE02B70;

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

    public bool FreezeActorOnPoseImport { get; set; } = false;
}
