namespace Brio.Config;

internal class SceneImportConfiguration
{
    public bool ApplyModelTransform { get; set; } = true;

    public ScenePoseTransformType PositionTransformType { get; set; } = ScenePoseTransformType.Difference;
    public ScenePoseTransformType RotationTransformType { get; set; } = ScenePoseTransformType.Absolute;
    public ScenePoseTransformType ScaleTransformType { get; set; } = ScenePoseTransformType.Absolute;
}

internal enum ScenePoseTransformType
{
    Ignore,
    Difference,
    Absolute
}
