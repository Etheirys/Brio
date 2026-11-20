using System.Collections.Generic;

namespace Brio.Resources;

public enum CommonPoseKind
{
    ChairSit,
    Standing,
    GroundSit,
    Sleep,
}

public readonly record struct CommonPoseDefinition(
    uint TimelineId,
    CommonPoseKind Kind,
    int VariantIndex,
    uint BaseEmoteId)
{
    public string DisplayName => (Kind, VariantIndex) switch
    {
        (CommonPoseKind.ChairSit, 0) => "Base Chair Sit Pose",
        (CommonPoseKind.ChairSit, _) => string.Format("Chair Sit Pose {0:D2}", VariantIndex),
        (CommonPoseKind.Standing, 0) => "Base Standing Pose",
        (CommonPoseKind.Standing, _) => string.Format("Standing Pose {0:D2}", VariantIndex),
        (CommonPoseKind.GroundSit, 0) => "Base Ground Sit Pose",
        (CommonPoseKind.GroundSit, _) => string.Format("Ground Sit Pose {0:D2}", VariantIndex),
        (CommonPoseKind.Sleep, _) => string.Format("Sleep Pose {0:D2}", VariantIndex),
        _ => string.Format("Timeline {0}", TimelineId),
    };
}

public static class CommonPoseCatalog
{
    public static IReadOnlyList<CommonPoseDefinition> All { get; } =
    [
        new(643, CommonPoseKind.ChairSit, 0, 50),
        new(3132, CommonPoseKind.ChairSit, 1, 50),
        new(3134, CommonPoseKind.ChairSit, 2, 50),
        new(8002, CommonPoseKind.ChairSit, 3, 50),
        new(8004, CommonPoseKind.ChairSit, 4, 50),

        new(3, CommonPoseKind.Standing, 0, 0),
        new(3124, CommonPoseKind.Standing, 1, 0),
        new(3126, CommonPoseKind.Standing, 2, 0),
        new(3182, CommonPoseKind.Standing, 3, 0),
        new(3184, CommonPoseKind.Standing, 4, 0),
        new(7405, CommonPoseKind.Standing, 5, 0),
        new(7407, CommonPoseKind.Standing, 6, 0),

        new(654, CommonPoseKind.GroundSit, 0, 52),
        new(3136, CommonPoseKind.GroundSit, 1, 52),
        new(3138, CommonPoseKind.GroundSit, 2, 52),
        new(3771, CommonPoseKind.GroundSit, 3, 52),

        new(3140, CommonPoseKind.Sleep, 1, 13),
        new(3142, CommonPoseKind.Sleep, 2, 13),
        new(585, CommonPoseKind.Sleep, 3, 13),
    ];
}
