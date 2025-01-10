using Brio.Core;
using ImGuizmoNET;

namespace Brio.Game.Posing;

internal class PosingService
{
    public PosingOperation Operation { get; set; } = PosingOperation.Rotate;

    public PosingCoordinateMode CoordinateMode { get; set; } = PosingCoordinateMode.Local;

    public bool UniversalGizmoOperation { get; set; } = false;

    public BoneCategories BoneCategories { get; } = new();

    public BoneFilter OverlayFilter { get; }

    public PoseImporterOptions DefaultImporterOptions { get; }

    public PoseImporterOptions DefaultIPCImporterOptions { get; }

    public PoseImporterOptions SceneImporterOptions { get; }

    public PoseImporterOptions BodyOptions { get; }

    public PoseImporterOptions ExpressionOptions { get; }
    public PoseImporterOptions ExpressionOptions2 { get; }

    public PosingService()
    {
        OverlayFilter = new BoneFilter(this);

        DefaultImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation, false);
        DefaultImporterOptions.BoneFilter.DisableCategory("weapon");

        DefaultIPCImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);

        SceneImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);

        BodyOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation | TransformComponents.Position, false);
        BodyOptions.BoneFilter.DisableCategory("weapon");
        BodyOptions.BoneFilter.DisableCategory("head");
        BodyOptions.BoneFilter.DisableCategory("face");
        BodyOptions.BoneFilter.DisableCategory("eyes");
        BodyOptions.BoneFilter.DisableCategory("lips");
        BodyOptions.BoneFilter.DisableCategory("jaw");
        BodyOptions.BoneFilter.DisableCategory("head");

        ExpressionOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);
        ExpressionOptions.BoneFilter.DisableAll();
        ExpressionOptions.BoneFilter.EnableCategory("head");
        ExpressionOptions.BoneFilter.EnableCategory("face");
        ExpressionOptions.BoneFilter.EnableCategory("eyes");
        ExpressionOptions.BoneFilter.EnableCategory("lips");
        ExpressionOptions.BoneFilter.EnableCategory("jaw");

        ExpressionOptions2 = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);
        ExpressionOptions2.BoneFilter.DisableAll();
        ExpressionOptions2.BoneFilter.EnableCategory("head");
    }
}

internal enum PosingCoordinateMode
{
    Local,
    World
}

internal enum PosingOperation
{
    Translate,
    Rotate,
    Scale,
    Universal
}

internal static class PosingExtensions
{
    public static MODE AsGizmoMode(this PosingCoordinateMode mode) => mode switch
    {
        PosingCoordinateMode.Local => MODE.LOCAL,
        PosingCoordinateMode.World => MODE.WORLD,
        _ => MODE.LOCAL
    };

    public static OPERATION AsGizmoOperation(this PosingOperation operation) => operation switch
    {
        PosingOperation.Translate => OPERATION.TRANSLATE,
        PosingOperation.Rotate => OPERATION.ROTATE,
        PosingOperation.Scale => OPERATION.SCALE,
        PosingOperation.Universal => OPERATION.TRANSLATE | OPERATION.ROTATE | OPERATION.SCALE,
        _ => OPERATION.ROTATE
    };
}
