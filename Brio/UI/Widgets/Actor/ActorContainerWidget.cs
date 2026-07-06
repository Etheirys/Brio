using Brio.Capabilities.Actor;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

public class ActorContainerWidget(ActorContainerCapability capability) : Widget<ActorContainerCapability>(capability)
{
    public override string HeaderName => "Actors";
    public override WidgetFlags Flags
    {
        get
        {
            WidgetFlags flags = WidgetFlags.DrawQuickIcons;

            if(Capability.CanControlCharacters)
                flags |= WidgetFlags.DrawPopup | WidgetFlags.CanHide;

            return flags;
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.BeginMenu("New...###containerwidgetpopup_new"))
        {
            if(ImGui.MenuItem("Actor###containerwidgetpopup_spawnbasic"))
            {
                Capability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
            }
            if(ImGui.MenuItem("Actor with Companion###containerwidgetpopup_spawncompanion"))
            {
                Capability.CreateCharacter(true, true);
            }

            ImGui.Separator();

            if(ImGui.MenuItem("Prop###containerwidgetpopup_spawnprop"))
            {

            }

            if(ImGui.MenuItem("Furniture Item###containerwidgetpopup_spawnfur"))
            {

            }

            if(ImGui.MenuItem("World Object###containerwidgetpopup_spawnworld"))
            {

            }

            if(ImGui.MenuItem("VFX###containerwidgetpopup_spawnVFX"))
            {

            }

            ImGui.EndMenu();
        }

        if(ImGui.BeginMenu("Add from World...###containerwidgetpopup_add"))
        {
            if(ImGui.BeginMenu("Actor...###containerwidgetpopup_addActor"))
            {
                var playerPosition = Capability.ObjectMonitorService.ObjectTable.LocalPlayer?.Position ?? Vector3.Zero; // I hate this
                var overworldActors = Capability.ObjectMonitorService.GetOverworldActors().OrderBy(actor => Vector3.DistanceSquared(playerPosition, actor.Position));

                if(!overworldActors.Any())
                {
                    ImGui.TextDisabled("No world actors found");
                }

                foreach(var actor in overworldActors)
                {
                    if(actor == null || !actor.IsValid())
                        return;

                    var distanceText = $" [{Vector3.Distance(playerPosition, actor.Position):0.0}]";

                    if(ImGui.MenuItem(string.IsNullOrWhiteSpace(actor?.Name.ToString()) ? $"Unknown {distanceText}##actor_containerwidgetpopup_{actor!.GameObjectId}" : $"{actor.Name} {distanceText}##actor_containerwidgetpopup_{actor.GameObjectId}"))
                    {
                        Capability.AddFromWorld(actor);
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndMenu();
            }
            ImGui.EndMenu();
        }

        if(ImGui.BeginMenu("Destroy All...###containerwidgetpopup_destroy"))
        {
            if(ImGui.BeginMenu("Actors###containerwidgetpopup_destroyActors"))
            {
                if(ImGui.MenuItem("Confirm Destruction##containerwidgetpopup_destroyallActors"))
                {
                    Capability.DestroyAll();
                }
                ImGui.EndMenu();
            }
            ImGui.EndMenu();
        }

        ImGui.Separator();
    }
}
