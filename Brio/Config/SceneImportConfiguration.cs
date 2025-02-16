namespace Brio.Config;

public class SceneImportConfiguration
{
    public bool ApplyModelTransform { get; set; } = true;

    public ScenePoseTransformType PositionTransformType { get; set; } = ScenePoseTransformType.Difference;
    public ScenePoseTransformType RotationTransformType { get; set; } = ScenePoseTransformType.Absolute;
    public ScenePoseTransformType ScaleTransformType { get; set; } = ScenePoseTransformType.Absolute;
}

public enum ScenePoseTransformType
{
    Ignore,
    Difference,
    Absolute
}
