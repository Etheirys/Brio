using Brio.Core;
using ImGuizmoNET;

namespace Brio.Game.Posing;

internal class PosingService
{
    public PosingCoordinateMode CoordinateMode { get; set; } = PosingCoordinateMode.Local;

    public BoneCategories BoneCategories { get; } = new();

    public BoneFilter OverlayFilter { get; }

    public PoseImporterOptions ImporterOptions { get; }

    public PosingService()
    {
        OverlayFilter = new BoneFilter(this);

        ImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation, false);
        ImporterOptions.BoneFilter.DisableCategory("weapon");
    }
}

internal enum PosingCoordinateMode
{
    Local,
    World
}

internal static class PosingExtensions
{
    public static MODE AsGizmoMode(this PosingCoordinateMode mode) => mode switch
    {
        PosingCoordinateMode.Local => MODE.LOCAL,
        PosingCoordinateMode.World => MODE.WORLD,
        _ => MODE.LOCAL
    };
}