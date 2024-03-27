using Brio.Core;
using ImGuizmoNET;

namespace Brio.Game.Posing;

internal class PosingService
{
    public PosingOperation Operation { get; set; } = PosingOperation.Rotate;

    public PosingCoordinateMode CoordinateMode { get; set; } = PosingCoordinateMode.Local;

    public BoneCategories BoneCategories { get; } = new();

    public BoneFilter OverlayFilter { get; }

    public PoseImporterOptions DefaultImporterOptions { get; }

    public PosingService()
    {
        OverlayFilter = new BoneFilter(this);

        DefaultImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation, false);
        DefaultImporterOptions.BoneFilter.DisableCategory("weapon");
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
    Scale
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
        _ => OPERATION.ROTATE
    };
}
