using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Entitites;

internal class EntityHierarchyView(EntityManager entityManager)
{
    private readonly float buttonWidth = ImGui.GetTextLineHeight() * 13f;
    private readonly float offsetWidth = 16f;

    private EntityId? _lastSelectedId;

    public void Draw(Entity root)
    {
        if(root.IsVisible is false)
            return;

        var selectedEntityId = entityManager.SelectedEntityId;

        if(_lastSelectedId != null && selectedEntityId != null && !_lastSelectedId.Equals(selectedEntityId))
        {
            // The change must have come from outside of this control
            _lastSelectedId = selectedEntityId;
        }

        using(ImRaii.PushId($"entity_hierarchy_{root.Id}"))
        {
            foreach(var item in root.Children)
            {
                DrawEntity(item, selectedEntityId);
            }
        }
    }

    private void DrawEntity(Entity entity, EntityId? selectedEntityId, float lastOffset = 0)
    {
        bool isSelected = false;
        bool hasChildren = false;

        if(entity.Children.Count > 0)
            hasChildren = true;
        if(selectedEntityId != null && entity.Id.Equals(selectedEntityId))
            isSelected = true;

        using(ImRaii.PushColor(ImGuiCol.ButtonActive, 0))
        {
            using(ImRaii.PushColor(ImGuiCol.Button, 0))
            {
                var invsButtonPos = ImGui.GetCursorPos();

                if(ImGui.Button($"###{entity.Id}_invs_button", new(buttonWidth, 0)))
                {
                    Select(entity);
                }
                if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"context_popup{entity.Id}");
                }

                ImGui.SetCursorPos(invsButtonPos);
            }

            if(lastOffset > 0)
            {
                var curPos = ImGui.GetCursorPos();

                ImGui.SetCursorPos(new Vector2(curPos.X + (lastOffset), curPos.Y));
                lastOffset += offsetWidth;
            }

            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.GizmoRed, isSelected))
            {
                using(ImRaii.Disabled(true))
                {
                    ImGui.Button($"###tab_{entity.Id}");
                }
            }
        }

        DrawNode(entity);

        if(entity is ActorEntity actor)
        {
            ImGui.SameLine();
      
            var aac = actor.GetCapability<ActorAppearanceCapability>();

            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.GizmoRed, aac.IsHidden))
            {
                string toolTip = aac.IsHidden ? $"Show {aac.Actor.FriendlyName}" : $"Hide {aac.Actor.FriendlyName}";
                if(ImBrio.FontIconButtonRight($"###{entity.Id}_hideActor", aac.IsHidden ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye, 1f, toolTip, bordered: false))
                {
                    aac.ToggleHide();
                }
            }

        }

        using(var popup = ImRaii.Popup($"context_popup{entity.Id}"))
        {
            if(popup.Success)
            {
                foreach(var v in entity.Capabilities)
                {
                    if(v.Widget is not null && v.Widget.Flags.HasFlag(WidgetFlags.DrawPopup))
                    {
                        v.Widget.DrawPopup();
                    }
                }
            }
        }

        if(hasChildren)
        {
            foreach(var child in entity.Children)
                DrawEntity(child, selectedEntityId, lastOffset == 0 ? 3 : lastOffset);
        }
    }

    private static void DrawNode(Entity entity)
    {
        var nodeStartPos = ImGui.GetCursorPos();

        ImGui.SameLine();
        ImGui.Text(" ");

        ImGui.SameLine();
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Text($"{entity.Icon.ToIconString()}");
        }

        ImGui.SameLine();
        ImGui.Text(entity.FriendlyName);

        ImGui.SetCursorPos(nodeStartPos);
    }

    private void Select(Entity entity)
    {
        _lastSelectedId = entity.Id;
        entityManager.SetSelectedEntity(entity);
    }
}
