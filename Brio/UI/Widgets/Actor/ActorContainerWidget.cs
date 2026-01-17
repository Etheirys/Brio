using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

public class ActorContainerWidget(ActorContainerCapability capability) : Widget<ActorContainerCapability>(capability)
{
    public override string HeaderName => "Actors";
    public override WidgetFlags Flags
    {
        get
        {
            WidgetFlags flags = WidgetFlags.DefaultOpen | WidgetFlags.DrawBody | WidgetFlags.DrawQuickIcons;

            if(Capability.CanControlCharacters)
                flags |= WidgetFlags.DrawPopup | WidgetFlags.CanHide;

            return flags;
        }
    }

    private ActorEntity? _selectedActor;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(!Capability.CanControlCharacters))
        {
            bool hasSelection = _selectedActor != null;

            if(ImBrio.FontIconButton("containerwidget_spawnbasic", FontAwesomeIcon.Plus, "Spawn"))
            {
                Capability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_spawnattachments", FontAwesomeIcon.PlusSquare, "Spawn with Companion slot"))
            {
                Capability.CreateCharacter(true, true);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("lifetimewidget_spawn_prop", FontAwesomeIcon.Cubes, "Spawn Prop"))
            {
                Capability.SpawnNewProp(true);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_clone", FontAwesomeIcon.Clone, "Clone", hasSelection))
            {
                Capability.CloneActor(_selectedActor!, false);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_destroy", FontAwesomeIcon.Trash, "Destroy", hasSelection))
            {
                Capability.DestroyCharacter(_selectedActor!);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_target", FontAwesomeIcon.Bullseye, "Target", hasSelection))
            {
                Capability.Target(_selectedActor!);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_selectinhierarchy", FontAwesomeIcon.FolderTree, "Select in Hierarchy", hasSelection))
            {
                Capability.SelectInHierarchy(_selectedActor!);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("containerwidget_destroyall", FontAwesomeIcon.Bomb, "Destroy All"))
            {
                Capability.DestroyAll();
            }
        }
    }

    public override void DrawBody()
    {
        if(ImGui.BeginListBox($"###actorcontainerwidget_{Capability.Entity.Id}_list", new Vector2(-1, 150 * ImGuiHelpers.GlobalScale)))
        {
            foreach(var child in Capability.Entity.Children)
            {
                if(child is ActorEntity actorEntity)
                {
                    bool isSelected = actorEntity.Equals(_selectedActor);
                    if(ImGui.Selectable($"{child.FriendlyName}###actorcontainerwidget_{Capability.Entity.Id}_item_{actorEntity.Id}", isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _selectedActor = actorEntity;
                    }
                }
            }

            ImGui.EndListBox();
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.BeginMenu("New...###containerwidgetpopup_new"))
        {
            if(ImGui.MenuItem("Spawn###containerwidgetpopup_spawnbasic"))
            {
                Capability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
            }
            if(ImGui.MenuItem("Spawn with Companion###containerwidgetpopup_spawncompanion"))
            {
                Capability.CreateCharacter(true, true);
            }
            if(ImGui.MenuItem("Spawn Prop###containerwidgetpopup_spawnprop"))
            {
                Capability.CreateProp(true);
            }

            ImGui.EndMenu();
        }

        if(ImGui.BeginMenu("Destroy All Actors###containerwidgetpopup_destroy"))
        {
            if(ImGui.MenuItem("Confirm Destruction##containerwidgetpopup_destroyall"))
            {
                Capability.DestroyAll();
            }
            ImGui.EndMenu();
        }
    }
}
