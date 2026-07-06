using Brio.Game.Posing;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Config;

public class PosingConfiguration
{
    // Overlay
    public bool OverlayDefaultsOn { get; set; } = false;
    public bool AllowGizmoAxisFlip { get; set; } = true;

    public PosingOperation LastGizmoOperation { get; set; } = PosingOperation.Rotate;

    public float BoneCircleSize { get; set; } = 6.300f;

    public uint LightCircleNormalColor { get; set; } = 0xFF00D9FC;
    public uint LightCircleHoveredColor { get; set; } = 0xFF2CE2FF;
    public uint LightCircleSelectedColor { get; set; } = 0xFF00D9FC;

    public uint BoneCircleNormalColor { get; set; } = 0xFFFFFFFF;
    public uint BoneCircleInactiveColor { get; set; } = 0x55555555;
    public uint BoneCircleHoveredColor { get; set; } = 0xFFFF0073;
    public uint BoneCircleSelectedColor { get; set; } = 0xFFF82B56;
    public float SkeletonLineThickness { get; set; } = 0.010f;
    public uint SkeletonLineActiveColor { get; set; } = 0xFFFFFFFF;
    public uint SkeletonLineInactiveColor { get; set; } = 0x55555555;
    public bool ShowSkeletonLines { get; set; } = true;
    public bool SkeletonLineToCircle { get; set; } = true;
    public bool HideGizmoWhenAdvancedPosingOpen { get; set; } = false;
    public bool HideToolbarWhenAdvandedPosingOpen { get; set; } = false;
    public bool HideSkeletonWhenGizmoActive { get; set; } = false;

    public bool UsePerCategoryLineColors { get; set; } = false;
    public Dictionary<string, uint> BoneCategoryLineColors { get; set; } = [];

    public bool ModelTransformStandout { get; set; } = true;
    public uint ModelTransformCircleStandOutColor { get; set; } = 0xFFE02B70;

    public uint WorldObjectOverlayColor { get; set; } = 0xFFFCD900;

    // Graphical Posing
    public bool GraphicalSidesSwapped { get; set; } = false;
    public bool ShowGenitaliaInAdvancedPoseWindow { get; set; } = false;

    // Hooks
    public bool DisableGPoseMouseSelect { get; set; } = true;
    public bool HideNameOnGPoseSettingsWindow { get; set; } = true;

    // Targeting
    public bool GPoseTargetChangesWithBrio { get; set; } = false;
    public bool BrioTargetChangesWithGPose { get; set; } = true;
    public bool AutoSelectTransformOnEntitySelect { get; set; } = false;

    public bool AutoSelectLightWhenClickingOnALight { get; set; } = true;
    public bool IfLightWindowisOpenDontUseSceneManager { get; set; } = false;
    public bool IfCameraWindowisOpenDontUseSceneManager { get; set; } = false;

    // Undo / Redo
    public int UndoStackSize { get; set; } = 50;

    public bool FreezeActorOnPoseImport { get; set; } = false;
   
    public bool FreeCameraHasMovementEnabledByDefault { get; set; } = true;
    public bool IsAdvancedGizmoEnabled { get; set; } = true;

    public bool UseOverlayOffset { get; set; } = true;
    public Dictionary<string, Vector3> BoneOverlayOffsets { get; set; } = [];
}
