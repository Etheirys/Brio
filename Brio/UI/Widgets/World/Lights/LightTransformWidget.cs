using Brio.Capabilities.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.World.Lights;

public class LightTransformWidget(LightTransformCapability lightGizmoCapability) : Widget<LightTransformCapability>(lightGizmoCapability)
{
    public override string HeaderName => "Light Transform";

    public override WidgetFlags Flags => WidgetFlags.DrawBody | WidgetFlags.DefaultOpen | WidgetFlags.CanHide;


    bool state = false;
    public unsafe override void DrawBody()
    {
        LightEditor.DrawLightTransform(Capability, ref state);
    }
}
