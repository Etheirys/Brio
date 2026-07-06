using Brio.Capabilities.World;
using Brio.Game.World.Interop;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Linq;

namespace Brio.UI.Widgets.World.Lights;

public class LightContainerWidget(LightContainerCapability capability) : Widget<LightContainerCapability>(capability)
{
    public override string HeaderName => "Lights";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImGui.BeginMenu("Add from World...###containerwidgetpopup_add"))
            {
                if(ImGui.BeginMenu("World Light...###containerwidgetpopup_addWorldLight"))
                {
                    var worldLights = Capability.GetWorldLights().OrderBy(x => x.distance).ToList();
                    if(worldLights.Count == 0)
                    {
                        ImGui.TextDisabled("No world lights found");
                    }
                    else
                    {
                        if(ImGui.MenuItem($"Add All ({worldLights.Count})###containerwidgetpopup_addAllWorldLights"))
                        {
                            Capability.AddAllWorldLights();
                        }
                        ImGui.Separator();
                        foreach(var (light, distance) in worldLights)
                        {
                            if(ImGui.MenuItem($"Light: {distance:F1}y##worldlight_{light}"))
                            {
                                Capability.AddWorldLight(light);
                            }
                        }
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }

            if(ImGui.MenuItem("Open Light Window###containerwidgetpopup_openWindow"))
            {
                Capability.OpenLightWindow();
            }

            if(ImGui.BeginMenu("New...###containerwidgetpopup_new"))
            {
                ImGui.Separator();

                if(ImGui.MenuItem("Spot Light###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.SpotLight);
                }
                if(ImGui.MenuItem("Area Light###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.PointLight);
                }
                if(ImGui.MenuItem("Flat Light###containerwidgetpopup_spawn_SpotLight"))
                {
                    Capability.LightingService.SpawnLight(LightType.FlatLight);
                }
                ImGui.EndMenu();
            }

            if(ImGui.BeginMenu("Destroy All...###containerwidgetpopup_destroy"))
            {
                if(ImGui.BeginMenu("Lights###containerwidgetpopup_destroyLights"))
                {
                    if(ImGui.MenuItem("Confirm Destruction##containerwidgetpopup_destroyallLights"))
                    {
                        Capability.LightingService.DestroyAll();
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenu();
            }
        }
    }
}
