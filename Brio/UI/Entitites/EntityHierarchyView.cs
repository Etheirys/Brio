using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Entitites;

public class EntityHierarchyView(EntityManager entityManager, GPoseService gPoseService)
{
    private float buttonWidth => ImGui.GetWindowContentRegionMax().X;
    private readonly float offsetWidth = 18f;

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
                var disable = gPoseService.IsGPosing == false && item.Flags.HasFlag(EntityFlags.AllowOutSideGpose) == false;
                using(ImRaii.Disabled(disable))
                    DrawEntity(item, selectedEntityId);
            }
        }
    }

    private void DrawEntity(Entity entity, EntityId? selectedEntityId, float lastOffset = 0, bool isChild = false)
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
              
                float width = buttonWidth;
                if(entity.ContextButtonCount >= 1)
                    width -= ((30 * ImGuiHelpers.GlobalScale) * entity.ContextButtonCount);
                else
                    width -= 5;

                if(ImGui.Button($"###{entity.Id}_invs_button", new(width, 24 * ImGuiHelpers.GlobalScale)))
                {
                    Select(entity);
                }
                if(ImGui.IsItemHovered())
                {
                    if(entity.Flags.HasFlag(EntityFlags.AllowDoubleClick) && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        entity.OnDoubleClick();
                    }
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

            using(ImRaii.PushColor(ImGuiCol.Button, TheameManager.CurrentTheame.Accent.AccentColor, isSelected))
            {
                using(ImRaii.Disabled(true))
                {
                    ImGui.Button($"###tab_{entity.Id}");
                }
            }
        }

        DrawNode(entity);

        if(entity.Flags.HasFlag(EntityFlags.HasContextButton))
        {
            ImGui.SameLine();

            entity.DrawContextButton();
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
            {
                DrawEntity(child, selectedEntityId, lastOffset == 0 ? 3 : lastOffset);
            }
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
