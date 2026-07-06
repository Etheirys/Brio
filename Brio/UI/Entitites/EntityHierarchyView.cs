using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.Input;
using Brio.Services;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Entitites;

public class EntityHierarchyView(EntityManager entityManager, GPoseService gPoseService)
{
    private const float OffsetWidth = 18f;
    private static float ButtonWidth => ImGui.GetWindowContentRegionMax().X;

    private readonly HashSet<EntityId> _collapsedFolders = [];

    private EntityId? _draggedEntityId;
    private EntityId? _lastSelectedId;

    public void Draw(Entity root, Entity? debug)
    {
        if(root.IsVisible is false)
            return;

        var selectedEntityId = entityManager.SelectedEntityById;

        if(_lastSelectedId != null && selectedEntityId != null && !_lastSelectedId.Equals(selectedEntityId))
        {
            // The change must have come from outside of this control
            _lastSelectedId = selectedEntityId;
        }

        if(ImGui.IsWindowHovered())
        {
            if(InputManagerService.ActionKeysPressed(InputAction.Interface_SelectAllActors))
            {
                entityManager.ClearSelectedEntities();
                foreach(var e in entityManager.TryGetAllActors())
                {
                    entityManager.AddSelectedEntity(e.Id);
                }
            }
        }

        using(ImRaii.PushId($"entity_hierarchy_{root.Id}"))
        {
            if(debug is not null)
            {
                DrawEntity(debug, selectedEntityId);
            }

            DrawEntity(root, selectedEntityId, drawChildren: false);

            foreach(var item in root.Children)
            {
                var disable = gPoseService.IsGPosing == false && item.Flags.HasFlag(EntityFlags.AllowOutsideGpose) == false;
                try
                {
                    using(ImRaii.Disabled(disable))
                        DrawEntity(item, selectedEntityId);
                }
                catch(Exception ex)
                {
                    Brio.Log.Error($"Error drawing entity {item.FriendlyName} ({item.Id}): {ex}");
                }
            }
        }
    }

    private void DrawEntity(Entity entity, EntityId? selectedEntityId, float lastOffset = 0, bool drawChildren = true)
    {
        bool isSelected = false;
        bool hasChildren = false;
        bool hasOffset = false;
        bool isMutiSelected = false;

        if(lastOffset > 0)
            hasOffset = true;
        if(entity.Children.Count > 0)
            hasChildren = true;
        if(selectedEntityId != null && entity.Id.Equals(selectedEntityId))
            isSelected = true;

        var currentSelected = entityManager.SelectedEntity;
        var currentSupportsMultiSelect = currentSelected?.Flags.HasFlag(EntityFlags.AllowMultiSelect) ?? false;

        var entityAllowsMultiSelect = entity.Flags.HasFlag(EntityFlags.AllowMultiSelect);

        if(entityManager.SelectedEntities.Contains(entity.Id) && entityAllowsMultiSelect)
            isMutiSelected = true;

        if(entity.Flags.HasFlag(EntityFlags.DisableDraw))
        {
            DrawChildren(entity, selectedEntityId, 3, hasChildren, drawChildren);
            return;
        }

        if(entity.Flags.HasFlag(EntityFlags.IsFolder))
        {
            DrawFolder(entity, selectedEntityId, lastOffset, hasChildren, drawChildren);
            return;
        }

        using(ImRaii.PushColor(ImGuiCol.Button, 0))
        using(ImRaii.PushColor(ImGuiCol.ButtonActive, 0))
        {
            var invsButtonPos = ImGui.GetCursorPos();
            float width = ButtonWidth;

            if(entity.ContextButtonCount >= 1)
                width -= (33 * ImGuiHelpers.GlobalScale * entity.ContextButtonCount);
            else
                width -= 5;

            if(ImGui.Button($"###{entity.Id}_invs_button", new(width, 24 * ImGuiHelpers.GlobalScale)))
            {
                if(InputManagerService.ActionKeysPressed(InputAction.Brio_Ctrl) && entityAllowsMultiSelect)
                {
                    var diableSelection = entity.Flags.HasFlag(EntityFlags.DisableSelection);

                    if(!currentSupportsMultiSelect && currentSelected != null)
                    {
                        if(diableSelection is false)
                            Select(entity);
                    }
                    else if(diableSelection is false)
                    {
                        if(entityManager.SelectedEntities.Contains(entity.Id))
                            entityManager.RemoveSelectedEntity(entity.Id);
                        else
                            entityManager.AddSelectedEntity(entity.Id);
                    }
                }
                else
                {
                    Select(entity);
                }
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

            if(ImGui.BeginDragDropSource())
            {
                _draggedEntityId = entity.Id;
                unsafe
                {
                    ImGui.SetDragDropPayload("BRIO_ENTITY", [134]);
                }

                using(ImRaii.PushFont(UiBuilder.IconFont))
                    ImGui.Text(entity.Icon.ToIconString());

                ImGui.SameLine();
                ImGui.Text(entity.FriendlyName);
                ImGui.EndDragDropSource();
            }

            ImGui.SetCursorPos(invsButtonPos);
        }

        if(hasOffset)
        {
            var curPos = ImGui.GetCursorPos();

            ImGui.SetCursorPos(new Vector2(curPos.X + lastOffset, curPos.Y));
            lastOffset += OffsetWidth;
        }

        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, isSelected || isMutiSelected))
        {
            using(ImRaii.Disabled(true))
            {
                ImGui.Button($"###tab_{entity.Id}", new Vector2(8 * ImGuiHelpers.GlobalScale, 24 * ImGuiHelpers.GlobalScale));
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

        DrawChildren(entity, selectedEntityId, lastOffset, hasChildren, drawChildren);
    }

    private void DrawFolder(Entity entity, EntityId? selectedEntityId, float lastOffset, bool hasChildren, bool drawChildren)
    {
        bool isFolderCollapsed = _collapsedFolders.Contains(entity.Id);

        bool hasOffset = false;
        if(lastOffset > 0)
            hasOffset = true;

        var arrow = isFolderCollapsed
            ? FontAwesomeIcon.CaretRight.ToIconString()
            : FontAwesomeIcon.CaretDown.ToIconString();

        var startPos = ImGui.GetCursorPos();
        float rowHeight = 24 * ImGuiHelpers.GlobalScale;

        using(ImRaii.PushColor(ImGuiCol.ButtonActive, 0))
        using(ImRaii.PushColor(ImGuiCol.Button, 0))
        {
            float folderBtnWidth = entity.ContextButtonCount >= 1
                ? ButtonWidth - (33 * ImGuiHelpers.GlobalScale * entity.ContextButtonCount)
                : ButtonWidth - 5;
            if(ImGui.Button($"###{entity.Id}_invs_button", new(folderBtnWidth, rowHeight)))
            {
            }
        }

        if(ImGui.IsItemHovered())
        {
            if(entity.Flags.HasFlag(EntityFlags.AllowDoubleClick) && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                entity.OnDoubleClick();
            }
        }
        if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            if(isFolderCollapsed)
                _collapsedFolders.Remove(entity.Id);
            else
                _collapsedFolders.Add(entity.Id);
        }
        if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup($"context_popup{entity.Id}");
        }

        if(entity is FolderEntity folderEntity && folderEntity.IsEditable)
        {
            if(ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    var released = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
                    var payload = ImGui.AcceptDragDropPayload("BRIO_ENTITY");

                    if(payload.IsNull == false && _draggedEntityId.HasValue && released &&
                        entityManager.TryGetEntity(_draggedEntityId.Value, out var draggedEntity))
                    {
                        Brio.Log.Verbose($"Moving entity {draggedEntity.FriendlyName} into folder {entity.FriendlyName}");

                        entityManager.MoveEntity(draggedEntity, entity);
                        _collapsedFolders.Remove(entity.Id); // Auto-expand on drop

                        _draggedEntityId = null;
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }

        ImGui.SetCursorPos(startPos);

        using(ImRaii.PushFont(UiBuilder.IconFont))
            ImGui.Button($"{arrow}###{entity.Id}_folder_toggle", new Vector2(24 * ImGuiHelpers.GlobalScale, rowHeight));

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

        if(isFolderCollapsed is false)
        {
            if(hasOffset)
            {
                lastOffset += OffsetWidth;
            }

            DrawChildren(entity, selectedEntityId, lastOffset, hasChildren, drawChildren);
        }
    }

    public void DrawChildren(Entity entity, EntityId? selectedEntityId, float lastOffset, bool hasChildren, bool drawChildren)
    {
        if(entity.Flags.HasFlag(EntityFlags.DisableChildren) == false && hasChildren && drawChildren)
        {
            foreach(var child in entity.Children.ToList())
            {
                DrawEntity(child, selectedEntityId, lastOffset == 0 ? 8 : lastOffset);
            }
        }
    }

    private static void DrawNode(Entity entity)
    {
        var nodeStartPos = ImGui.GetCursorPos();

        ImGui.SameLine();
        ImGui.Text(" ");

        ImGui.SameLine();

        ImBrio.Icon(entity.Icon);

        ImGui.SameLine();

        float contextButtonsWidth = entity.ContextButtonCount >= 1
            ? 33f * ImGuiHelpers.GlobalScale * entity.ContextButtonCount
            : 0f;

        ImBrio.DrawTruncateTextToWidth(entity.FriendlyName, ImGui.GetContentRegionAvail().X - contextButtonsWidth);

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(entity.FriendlyName);

        ImGui.SetCursorPos(nodeStartPos);
    }

    private void Select(Entity entity)
    {
        _lastSelectedId = entity.Id;
        entityManager.SetSelectedEntity(entity);
    }
}
