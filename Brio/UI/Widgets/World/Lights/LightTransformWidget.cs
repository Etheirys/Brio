using Brio.Capabilities.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.World.Lights;

public class LightTransformWidget(LightTransformCapability lightGizmoCapability) : Widget<LightTransformCapability>(lightGizmoCapability)
{
    public override string HeaderName => "Light Transform";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.CanHide;

    private readonly ITransformableEditor _transformableEditor = new();

    public override void DrawBody()
    {
        LightEditor.DrawLightTransformHeader(Capability);
        _transformableEditor.Draw($"light_transform_{Capability.Entity.Id}", Capability.Light, 0.1f);
    }
}
