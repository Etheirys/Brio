using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.UI.Widgets.Core;
using System.Linq;

namespace Brio.UI.Entitites;

internal class EntityHierarchyView(EntityManager entityManager)
{
    private EntityId? _lastSelectedId;
    private bool _scrollToSelected;

    public void Draw(Entity root)
    {
        var selectedEntityId = entityManager.SelectedEntityId;

        if (_lastSelectedId != null && selectedEntityId != null && !_lastSelectedId.Equals(selectedEntityId))
        {
            // The change must have come from outside of this control, so we need to scroll to the new selection
            _scrollToSelected = true;
            _lastSelectedId = selectedEntityId;
        }

        using (ImRaii.PushId($"entity_hierarchy_{root.Id}"))
        {
            DrawEntity(root, selectedEntityId);
        }
    }

    private void DrawEntity(Entity entity, EntityId? selectedEntityId)
    {
        if (!entity.IsVisible)
            return;

        bool didRightClick = false;

        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        if (entity.Flags.HasFlag(EntityFlags.DefaultOpen))
            flags |= ImGuiTreeNodeFlags.DefaultOpen;

        if (entity.Children.Count == 0)
            flags |= ImGuiTreeNodeFlags.Leaf;

        if (selectedEntityId != null && entity.Id.Equals(selectedEntityId))
        {
            if (_scrollToSelected)
            {
                ImGui.SetScrollHereY();
                _scrollToSelected = false;
            }
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        using (var treeNode = ImRaii.TreeNode($"###treenode_{entity.Id}", flags))
        {
            bool isNodeOpen = treeNode.Success;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                Select(entity);
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Select(entity);
                didRightClick = true;
            }

            DrawNode(entity);

            if (didRightClick)
            {
                ImGui.OpenPopup($"context_popup");
            }

            using (var popup = ImRaii.Popup("context_popup"))
            {
                if (popup.Success)
                {
                    bool didDrawAnything = false;

                    foreach (var v in entity.Capabilities.ToList())
                    {
                        if (v.Widget != null)
                        {
                            bool hasPopup = v.Widget.Flags.HasFlag(WidgetFlags.DrawPopup);
                            didDrawAnything |= hasPopup;
                            if (hasPopup)
                                v.Widget.DrawPopup();
                        }
                    }

                    if (!didDrawAnything)
                        ImGui.CloseCurrentPopup();
                }
            }

            if (isNodeOpen)
            {
                if (entity.Children.Count > 0)
                    foreach (var child in entity.Children.ToList())
                        DrawEntity(child, selectedEntityId);
            }

        }
    }

    private void DrawNode(Entity entity)
    {
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Text(entity.Icon.ToIconString());
        }
        ImGui.SameLine();
        ImGui.Text(entity.FriendlyName);

    }

    private void Select(Entity entity)
    {
        _lastSelectedId = entity.Id; // Make sure we don't scroll if it was set inside this control
        entityManager.SetSelectedEntity(entity);
    }
}
