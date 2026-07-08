using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.Input;
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

    private readonly List<(Entity entity, float offset, bool disabled)> _visibleItems = [];
    private readonly HashSet<EntityId> _collapsedFolders = [];

    private EntityId? _draggedEntityId;
    private EntityId? _lastSelectedId;

    public void Draw(Entity root, Entity? debug, Entity? timeline)
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
            BuildVisibleEtities(root, debug, timeline);

            var entityHeight = 24 * ImGuiHelpers.GlobalScale;

            unsafe
            {
                var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
                clipper.Begin(_visibleItems.Count, entityHeight);
                while(clipper.Step())
                {
                    for(int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var (entity, offset, disabled) = _visibleItems[i];

                        try
                        {
                            using(ImRaii.Disabled(disabled))
                            {
                                if(entity.Flags.HasFlag(EntityFlags.IsFolder))
                                {
                                    DrawFolder(entity, selectedEntityId);
                                    continue;
                                }

                                DrawEntity(entity, selectedEntityId, offset);

                            }
                        }
                        catch(Exception ex)
                        {
                            Brio.Log.Error($"Error drawing entity {entity.FriendlyName} ({entity.Id}): {ex}");
                        }
                    }
                }
                clipper.End();
                clipper.Destroy();
            }
        }
    }

    private void BuildVisibleEtities(Entity root, Entity? debug, Entity? timeline)
    {
        _visibleItems.Clear();

        if(debug is not null)
            AppendRow(debug, 0, false);

        if(timeline is not null)
            AppendRow(timeline, 0, false);

        AppendRow(root, 0, false, recurseChildren: false);

        foreach(var child in root.Children)
        {
            var disabled = gPoseService.IsGPosing == false && child.Flags.HasFlag(EntityFlags.AllowOutsideGpose) == false;
            AppendRow(child, 0, disabled);
        }

        void AppendRow(Entity entity, float offset, bool disabled, bool recurseChildren = true)
        {
            var draw = entity.Flags.HasFlag(EntityFlags.DisableDraw) == false;

            if(draw)
            {
                _visibleItems.Add((entity, offset, disabled));

                if(entity.Flags.HasFlag(EntityFlags.IsFolder) && _collapsedFolders.Contains(entity.Id))
                    return;
            }

            if(recurseChildren == false || entity.Flags.HasFlag(EntityFlags.DisableChildren))
                return;

            var childOffset = draw == false ? offset : offset == 0 ? 8 : offset + OffsetWidth;

            foreach(var child in entity.Children.ToList())
                AppendRow(child, childOffset, disabled);
        }
    }

    private void DrawEntity(Entity entity, EntityId? selectedEntityId, float lastOffset)
    {
        bool isSelected = false;
        bool hasOffset = false;
        bool isMutiSelected = false;

        if(lastOffset > 0)
            hasOffset = true;
        if(selectedEntityId != null && entity.Id.Equals(selectedEntityId))
            isSelected = true;

        var currentSelected = entityManager.SelectedEntity;
        var currentSupportsMultiSelect = currentSelected?.Flags.HasFlag(EntityFlags.AllowMultiSelect) ?? false;

        var entityAllowsMultiSelect = entity.Flags.HasFlag(EntityFlags.AllowMultiSelect);

        if(entityManager.SelectedEntities.Contains(entity.Id) && entityAllowsMultiSelect)
            isMutiSelected = true;

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

            if(entity == entityManager.EntityManagerContainer)
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
                            MoveDraggedSelectionTo(draggedEntity, entity);
                            _draggedEntityId = null;
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
            }

            ImGui.SetCursorPos(invsButtonPos);
        }

        if(hasOffset)
        {
            var curPos = ImGui.GetCursorPos();

            ImGui.SetCursorPos(new Vector2(curPos.X + lastOffset, curPos.Y));
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
    }

    private void DrawFolder(Entity entity, EntityId? selectedEntityId)
    {
        bool isFolderCollapsed = _collapsedFolders.Contains(entity.Id);

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
                        MoveDraggedSelectionTo(draggedEntity, entity);
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
    }

    private void MoveDraggedSelectionTo(Entity draggedEntity, Entity newParent)
    {
        var isMultiDrag = entityManager.SelectedEntities.Contains(draggedEntity.Id) && entityManager.SelectedEntities.Count > 1;

        if(isMultiDrag)
        {
            var selected = new List<Entity>();
            foreach(var id in entityManager.SelectedEntities)
                if(entityManager.TryGetEntity(id, out var selectedEntity))
                    selected.Add(selectedEntity);

            entityManager.MoveEntities(selected, newParent);
        }
        else
        {
            entityManager.MoveEntity(draggedEntity, newParent);
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
