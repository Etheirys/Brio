using Brio.Capabilities.World;
using Brio.Game.World;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.World.Lights;

public class LightContainerWidget(LightContainerCapability capability) : Widget<LightContainerCapability>(capability)
{
    public override string HeaderName => "Lights";

    public override WidgetFlags Flags => WidgetFlags.DrawQuickIcons | WidgetFlags.DrawPopup;


    public override void DrawQuickIcons()
    {

    }

    public override void DrawPopup()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImGui.MenuItem("Open Light Window###containerwidgetpopup_openWindow"))
            {
                Capability.OpenLightWindow();
            }

            if(ImGui.BeginMenu("New...###containerwidgetpopup_new"))
            {
                if(ImGui.MenuItem("Spot Light###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.SpotLight);
                }
                if(ImGui.MenuItem("Area Light###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.AreaLight);
                }
                if(ImGui.MenuItem("Flat Light###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.FlatLight);
                }
                ImGui.EndMenu();
            }

            if(ImGui.BeginMenu("Destroy All Lights###containerwidgetpopup_destroy"))
            {
                if(ImGui.MenuItem("Confirm Destruction##containerwidgetpopup_destroyall"))
                {
                    Capability.LightingService.DestroyAllLights();
                }
                ImGui.EndMenu();
            }
        }
    }
}
