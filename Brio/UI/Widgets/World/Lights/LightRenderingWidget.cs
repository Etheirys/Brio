using Brio.Capabilities.World;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.World.Lights;

public class LightRenderingWidget(LightRenderingCapability lightRenderingCapability) : Widget<LightRenderingCapability>(lightRenderingCapability)
{
    public override string HeaderName => "Light Properties";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.CanHide;

    public override void DrawPopup()
    {
        var togglenText = Capability.GameLight.IsVisible ? $"Turn OFF {Capability.Entity.FriendlyName}" : $"Turn ON {Capability.Entity.FriendlyName}";
        if(ImGui.MenuItem($"{togglenText}###togglelight"))
        {
            Capability.GameLight.ToggleLight();
        }
    }
}
