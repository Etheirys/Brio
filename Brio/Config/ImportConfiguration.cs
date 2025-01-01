namespace Brio.Config;

internal class ImportConfiguration
{
    public bool ApplyModelTransform { get; set; } = true;

    public PoseImportTransformType PositionTransformType { get; set; } = PoseImportTransformType.Difference;
    public PoseImportTransformType RotationTransformType { get; set; } = PoseImportTransformType.Absolute;
    public PoseImportTransformType ScaleTransformType { get; set; } = PoseImportTransformType.Absolute;
}

internal enum PoseImportTransformType
{
    Ignore,
    Difference,
    Absolute
}
