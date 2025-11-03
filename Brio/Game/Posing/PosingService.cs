using Brio.Core;
using Dalamud.Bindings.ImGuizmo;

namespace Brio.Game.Posing;

public class PosingService
{
    public PosingOperation Operation { get; set; } = PosingOperation.Rotate;

    public PosingCoordinateMode CoordinateMode { get; set; } = PosingCoordinateMode.Local;

    public bool GizmoStaysWhenAllBonesAreDisabled { get; set; } = false;

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
        OverlayFilter.DisableCategory("ex");
        OverlayFilter.DisableCategory("weapon");
        OverlayFilter.DisableCategory("clothing");
        OverlayFilter.DisableCategory("legacy");

        DefaultImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation, false);
        DefaultImporterOptions.BoneFilter.DisableCategory("weapon");
        DefaultImporterOptions.BoneFilter.DisableCategory("ex");

        DefaultIPCImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);

        SceneImporterOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);

        BodyOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.Rotation | TransformComponents.Position, false);
        BodyOptions.BoneFilter.DisableCategory("weapon");
        BodyOptions.BoneFilter.DisableCategory("ears");
        BodyOptions.BoneFilter.DisableCategory("hair");
        BodyOptions.BoneFilter.DisableCategory("face");
        BodyOptions.BoneFilter.DisableCategory("eyes");
        BodyOptions.BoneFilter.DisableCategory("lips");
        BodyOptions.BoneFilter.DisableCategory("jaw");
        BodyOptions.BoneFilter.DisableCategory("head");
        BodyOptions.BoneFilter.DisableCategory("legacy");
        BodyOptions.BoneFilter.DisableCategory("ex");

        ExpressionOptions = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);
        ExpressionOptions.BoneFilter.DisableAll();
        ExpressionOptions.BoneFilter.EnableCategory("head");
        ExpressionOptions.BoneFilter.EnableCategory("ears");
        ExpressionOptions.BoneFilter.EnableCategory("hair");
        ExpressionOptions.BoneFilter.EnableCategory("face");
        ExpressionOptions.BoneFilter.EnableCategory("eyes");
        ExpressionOptions.BoneFilter.EnableCategory("lips");
        ExpressionOptions.BoneFilter.EnableCategory("jaw");

        ExpressionOptions2 = new PoseImporterOptions(new BoneFilter(this), TransformComponents.All, false);
        ExpressionOptions2.BoneFilter.DisableAll();
        ExpressionOptions2.BoneFilter.EnableCategory("head");
    }

    public PoseImporterOptions GetNewPoseImporterOptions(TransformComponents transformComponents, bool applyModelTransform)
        => new PoseImporterOptions(new BoneFilter(this), transformComponents, applyModelTransform);
}

public enum PosingCoordinateMode
{
    Local,
    World
}

public enum PosingOperation
{
    Translate,
    Rotate,
    Scale,
    Universal
}

public static class PosingExtensions
{
    public static ImGuizmoMode AsGizmoMode(this PosingCoordinateMode mode) => mode switch
    {
        PosingCoordinateMode.Local => ImGuizmoMode.Local,
        PosingCoordinateMode.World => ImGuizmoMode.World,
        _ => ImGuizmoMode.Local
    };

    public static ImGuizmoOperation AsGizmoOperation(this PosingOperation operation) => operation switch
    {
        PosingOperation.Translate => ImGuizmoOperation.Translate,
        PosingOperation.Rotate => ImGuizmoOperation.Rotate,
        PosingOperation.Scale => ImGuizmoOperation.Scale,
        PosingOperation.Universal => ImGuizmoOperation.Translate | ImGuizmoOperation.Rotate | ImGuizmoOperation.Scale,
        _ => ImGuizmoOperation.Rotate
    };
}
