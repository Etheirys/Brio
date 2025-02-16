using Brio.Capabilities.Posing;
using Brio.Game.Posing;
using Brio.Game.Posing.Skeletons;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OneOf.Types;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class BoneSearchControl
{
    private string _searchTerm = string.Empty;
    public void Draw(string id, PosingCapability posing)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 10);

        using(ImRaii.PushId(id))
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText("###search_term", ref _searchTerm, 256);

            using(var child = ImRaii.Child("###bone_search_editor_child", new Vector2(400, ImGui.GetTextLineHeight() * 25f), true))
            {
                if(child.Success)
                {

                    bool rootSelected = posing.Selected.Value is None || posing.Selected.Value is ModelTransformSelection;
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick;

                    if(rootSelected)
                        flags |= ImGuiTreeNodeFlags.Selected;

                    using(var node = ImRaii.TreeNode("Model", flags))
                    {
                        if(node.Success)
                        {
                            if(ImGui.IsItemClicked())
                                posing.Selected = PosingSelectionType.ModelTransform;

                            if(posing.SkeletonPosing.CharacterSkeleton != null)
                            {
                                using(var skeleton = ImRaii.TreeNode("Character", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick))
                                {
                                    if(skeleton.Success)
                                    {
                                        DrawBone(posing.SkeletonPosing.CharacterSkeleton.RootBone, posing, PoseInfoSlot.Character);
                                    }
                                }
                            }

                            if(posing.SkeletonPosing.MainHandSkeleton != null)
                            {
                                using(var skeleton = ImRaii.TreeNode("Main Hand", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick))
                                {
                                    if(skeleton.Success)
                                    {
                                        DrawBone(posing.SkeletonPosing.MainHandSkeleton.RootBone, posing, PoseInfoSlot.MainHand);
                                    }
                                }
                            }

                            if(posing.SkeletonPosing.OffHandSkeleton != null)
                            {
                                using(var skeleton = ImRaii.TreeNode("Off Hand", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick))
                                {
                                    if(skeleton.Success)
                                    {
                                        DrawBone(posing.SkeletonPosing.OffHandSkeleton.RootBone, posing, PoseInfoSlot.OffHand);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        ImGui.PopStyleVar();
    }

    private void DrawBone(Bone bone, PosingCapability posing, PoseInfoSlot slot)
    {
        var bonePoseInfoId = new BonePoseInfoId(bone.Name, bone.PartialId, slot);

        bool selected = posing.Selected.Value is BonePoseInfoId selectedBonePoseInfoid && selectedBonePoseInfoid == bonePoseInfoId;

        bool leaf = bone.Children.Count == 0;

        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick;

        bool treeIncludesTerm = TreeIncludesTerm(bone, _searchTerm, false);

        if(leaf || !treeIncludesTerm)
            flags |= ImGuiTreeNodeFlags.Leaf;

        if(selected)
            flags |= ImGuiTreeNodeFlags.Selected;

        treeIncludesTerm = TreeIncludesTerm(bone, _searchTerm, true);
        if(!treeIncludesTerm)
            return;

        if(!bone.IsHidden)
        {
            using(var node = ImRaii.TreeNode($"{bone.FriendlyName}###{bonePoseInfoId}", flags))
            {
                if(node.Success)
                {
                    if(ImGui.IsItemClicked())
                    {
                        posing.Selected = bonePoseInfoId;
                    }

                    foreach(var child in bone.Children)
                    {
                        DrawBone(child, posing, slot);
                    }
                }
            }
        }
        else
        {
            foreach(var child in bone.Children)
            {
                DrawBone(child, posing, slot);
            }
        }
    }

    private bool TreeIncludesTerm(Bone bone, string term, bool includeCurrent)
    {
        if(string.IsNullOrWhiteSpace(term))
            return true;

        if(includeCurrent)
            if(bone.FriendlyDescriptor.Contains(term, StringComparison.OrdinalIgnoreCase))
                return true;

        foreach(var child in bone.Children)
        {
            if(TreeIncludesTerm(child, term, true))
                return true;
        }

        return false;
    }
}
