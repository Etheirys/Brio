using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

internal class ActorContainerWidget(ActorContainerCapability capability) : Widget<ActorContainerCapability>(capability)
{
    public override string HeaderName => "Actors";
    public override WidgetFlags Flags
    {
        get
        {
            WidgetFlags flags = WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

            if (Capability.CanControlCharacters)
                flags |= WidgetFlags.DrawPopup;

            return flags;
        }
    }

    private ActorEntity? _selectedActor;

    public override void DrawBody()
    {
        if (ImGui.BeginListBox($"###actorcontainerwidget_{Capability.Entity.Id}_list", new Vector2(-1, 150)))
        {
            foreach (var child in Capability.Entity.Children)
            {
                if (child is ActorEntity actorEntity)
                {
                    bool isSelected = actorEntity.Equals(_selectedActor);
                    if (ImGui.Selectable($"{child.FriendlyName}###actorcontainerwidget_{Capability.Entity.Id}_item_{actorEntity.Id}", isSelected, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _selectedActor = actorEntity;
                    }
                }
            }

            ImGui.EndListBox();
        }



        using (ImRaii.Disabled(!Capability.CanControlCharacters))
        {
            bool hasSelection = _selectedActor != null;

            if (ImBrio.FontIconButton("containerwidget_spawnbasic", FontAwesomeIcon.Plus, "Spawn"))
            {
                Capability.CreateCharacter(false, false);
            }

            ImGui.SameLine();

            if (ImBrio.FontIconButton("containerwidget_spawnattachments", FontAwesomeIcon.PlusSquare, "Spawn with Companion slot"))
            {
                Capability.CreateCharacter(true, false);
            }

            ImGui.SameLine();

            if (ImBrio.FontIconButton("containerwidget_clone", FontAwesomeIcon.Clone, "Clone", hasSelection))
            {
                Capability.CloneActor(_selectedActor!, false);
            }

            ImGui.SameLine();

            if (ImBrio.FontIconButton("containerwidget_destroy", FontAwesomeIcon.Trash, "Destroy", hasSelection))
            {
                Capability.DestroyCharacter(_selectedActor!);
            }

            ImGui.SameLine();

            if (ImBrio.FontIconButton("containerwidget_target", FontAwesomeIcon.Bullseye, "Target", hasSelection))
            {
                Capability.Target(_selectedActor!);
            }

            ImGui.SameLine();

            if (ImBrio.FontIconButton("containerwidget_selectinhierarchy", FontAwesomeIcon.FolderTree, "Select in Hierarchy", hasSelection))
            {
                Capability.SelectInHierarchy(_selectedActor!);
            }

            ImGui.SameLine();

            if (ImBrio.FontIconButton("containerwidget_destroyall", FontAwesomeIcon.Bomb, "Destroy All"))
            {
                Capability.DestroyAll();
            }
        }
    }

    public override void DrawPopup()
    {
        if (ImGui.MenuItem("Spawn###containerwidgetpopup_spawnbasic"))
        {
            Capability.CreateCharacter(false, true);
        }

        if (ImGui.MenuItem("Spawn with Companion###containerwidgetpopup_spawncompanion"))
        {
            Capability.CreateCharacter(true, true);
        }

        if (ImGui.MenuItem("Destroy All###containerwidgetpopup_destroyall"))
        {
            Capability.DestroyAll();
        }
    }
}
