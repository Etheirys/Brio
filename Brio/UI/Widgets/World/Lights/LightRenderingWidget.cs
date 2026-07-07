using Brio.Capabilities.World;
using Brio.UI.Widgets.Core;

namespace Brio.UI.Widgets.World.Lights;

public class LightRenderingWidget(LightRenderingCapability lightRenderingCapability) : Widget<LightRenderingCapability>(lightRenderingCapability)
{
    public override string HeaderName => "Light Properties";

    public override WidgetFlags Flags => WidgetFlags.CanHide;
}
