using Brio.Capabilities.World;
using Brio.UI.Widgets.Core;
using ImGuiNET;

namespace Brio.UI.Widgets.World;

public class WorldRenderingWidget(WorldRenderingCapability worldRenderingCapability) : Widget<WorldRenderingCapability>(worldRenderingCapability)
{
    public override string HeaderName => "Rendering";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

    public override void DrawBody()
    {
        var isWaterFrozen = Capability.WorldRenderingService.IsWaterFrozen;

        if(ImGui.Checkbox("Freeze Water", ref isWaterFrozen))
        {
            Capability.WorldRenderingService.IsWaterFrozen = isWaterFrozen;
        }
    }
}
