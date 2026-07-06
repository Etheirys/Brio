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
                Capability.WorldObjectService.SpawnProp(new FFXIVClientStructs.FFXIV.Client.Graphics.Scene.WeaponCreateInfo
                {
                    WeaponModelId =
                    {
                        Id = 9001,
                        Type = 249,
                        Variant = 1,
                        Stain0 = 1,
                        Stain1 = 1,
                    },
                    AnimationVariant = 0,
                });
            }

            if(ImGui.MenuItem("Furniture Item###containerwidgetpopup_spawnfur"))
            {
                Capability.WorldObjectService.SpawnFurniture("bgcommon/hou/outdoor/general/0332/asset/gar_b0_m0332.sgb");
            }

            if(ImGui.MenuItem("World Object###containerwidgetpopup_spawnworld"))
            {
                Capability.WorldObjectService.SpawnBgObject("bg/ffxiv/fst_f1/twn/common/bgparts/f1t0_a0_taru1.mdl");
            }

            if(ImGui.MenuItem("VFX###containerwidgetpopup_spawnVFX"))
            {
                Capability.WorldObjectService.SpawnStaticVfx("bgcommon/world/common/vfx_for_bg/eff/val_obj001_o.avfx");
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
