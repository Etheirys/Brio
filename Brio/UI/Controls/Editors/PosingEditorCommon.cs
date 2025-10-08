using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Game.Posing;
using Brio.Input;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public static class PosingEditorCommon
{
    public static void DrawSelectionName(PosingCapability posing)
    {
        ImGui.Text(posing.Selected.DisplayName);

        if(posing.Actor.IsProp == false)
        {
            ImGui.SetWindowFontScale(0.75f);
            ImGui.TextDisabled(posing.Selected.Subtitle);
            ImGui.SetWindowFontScale(1.0f);
        }

        BonePoseInfoId? selectedIsBone = posing.IsSelectedBone();
        using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
        {
            if(selectedIsBone.HasValue)
            {
                Game.Posing.Skeletons.Bone? bone = posing.SkeletonPosing.GetBone(selectedIsBone.Value);
                if(bone != null && bone.Skeleton.IsValid && bone.Freeze)
                {
                    ImGui.Text("This bone's transform values are frozen.");
                }
            }
            else
            {
                if(posing.ModelPosing.Freeze)
                {
                    ImGui.Text("This actor's transform values are frozen.");
                }
            }
        }
    }

    public static void DrawImportOptionEditor(PoseImporterOptions options, PosingService posingService, bool compact = false)
    {
        DrawBoneFilterEditor(options.BoneFilter, null);

        if(compact == false)
        {
            ImGui.Separator();

            var selected = options.TransformComponents.HasFlag(TransformComponents.Position);
            if(ImGui.Checkbox("Position", ref selected))
            {
                if(selected)
                    options.TransformComponents |= TransformComponents.Position;
                else
                    options.TransformComponents &= ~TransformComponents.Position;
            }

            selected = options.TransformComponents.HasFlag(TransformComponents.Rotation);
            if(ImGui.Checkbox("Rotation", ref selected))
            {
                if(selected)
                    options.TransformComponents |= TransformComponents.Rotation;
                else
                    options.TransformComponents &= ~TransformComponents.Rotation;
            }

            selected = options.TransformComponents.HasFlag(TransformComponents.Scale);
            if(ImGui.Checkbox("Scale", ref selected))
            {
                if(selected)
                    options.TransformComponents |= TransformComponents.Scale;
                else
                    options.TransformComponents &= ~TransformComponents.Scale;
            }

            ImGui.Separator();

            selected = options.ApplyModelTransform;
            if(ImGui.Checkbox("Model Transform", ref selected))
            {
                options.ApplyModelTransform = selected;
            }
        }
    }

    public static void DrawBoneFilterEditor(BoneFilter filter, PosingService? posingService)
    {
        if(ImBrio.FontIconButton("select_all", FontAwesomeIcon.Check, "Select All"))
        {
            filter.EnableAll();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("select_none", FontAwesomeIcon.Minus, "Select None"))
        {
            filter.DisableAll();
        }

        ImGui.SameLine();

        if(posingService is not null)
        {
            if(ImBrio.ToggelFontIconButton("keep_gizmo", FontAwesomeIcon.LocationCrosshairs, new(0), posingService.GizmoStaysWhenAllBonesAreDisabled, hoverText: "Keep gizmo active even when all items in the filter are disabled"))
            {
                posingService.GizmoStaysWhenAllBonesAreDisabled = !posingService.GizmoStaysWhenAllBonesAreDisabled;
            }

            ImGui.Separator();
        }

        foreach(var category in filter.AllCategories)
        {
            var isEnabled = filter.IsCategoryEnabled(category);

            if(category.Type is BoneCategories.BoneCategoryTypes.Category)
            {
                isEnabled = filter.IsSubCategoryEnabled(category.Id);
                ImGui.Separator();
            }

            if(ImGui.Checkbox(category.Name, ref isEnabled))
            {
                if(category.Type is BoneCategories.BoneCategoryTypes.Category)
                {
                    if(isEnabled)
                        filter.EnableSubCategory(category.Id);
                    else
                        filter.DisableSubCategory(category.Id);
                }
                else
                {
                    if(isEnabled)
                        filter.EnableCategory(category);
                    else
                        filter.DisableCategory(category);
                }
            }
            if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                filter.EnableOnly(category);
            }
        
            if(category.Type is BoneCategories.BoneCategoryTypes.Category)
            {
                ImGui.Separator();
            }
        }
    }

    public static void DrawMirrorModeSelect(PosingCapability? posing, Vector2 buttonSize)
    {
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            var hasMirror = posing?.Selected.Match(
                boneSelect => posing.SkeletonPosing.GetBonePose(boneSelect).GetMirrorBone() != null,
                _ => false,
                _ => false
            ) ?? false;

            using(ImRaii.Disabled(posing?.Selected.Value is None || !hasMirror))
            {
                posing?.Selected.Switch(
                    boneSelect =>
                    {
                        var poseInfo = posing.SkeletonPosing.GetBonePose(boneSelect);
                        switch(poseInfo.MirrorMode)
                        {
                            case PoseMirrorMode.None:
                                if(ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize) || InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_ToggleLink))
                                    poseInfo.MirrorMode = PoseMirrorMode.Copy;
                                break;
                            case PoseMirrorMode.Copy:
                                if(ImGui.Button($"{FontAwesomeIcon.Link.ToIconString()}###mirror_mode", buttonSize) || InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_ToggleLink))
                                    poseInfo.MirrorMode = PoseMirrorMode.Mirror;
                                break;
                            case PoseMirrorMode.Mirror:
                                if(ImGui.Button($"{FontAwesomeIcon.YinYang.ToIconString()}###mirror_mode", buttonSize) || InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_ToggleLink))
                                    poseInfo.MirrorMode = PoseMirrorMode.None;
                                break;
                        }
                    },
                    _ => { ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize); },
                    _ => { ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize); }
                );

                if(posing is null)
                {
                    ImGui.BeginDisabled();
                    ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize);
                    ImGui.EndDisabled();
                }
            }
        }

        if(ImGui.IsItemHovered())
        {
            if(posing?.Selected.Value is BonePoseInfoId poseInfo)
            {
                switch(posing.SkeletonPosing.GetBonePose(poseInfo).MirrorMode)
                {
                    case PoseMirrorMode.None:
                        ImGui.SetTooltip("Link: None");
                        break;
                    case PoseMirrorMode.Copy:
                        ImGui.SetTooltip("Link: Copy");
                        break;
                    case PoseMirrorMode.Mirror:
                        ImGui.SetTooltip("Link: Mirror");
                        break;
                }
            }
        }
    }

    public static void DrawIKSelect(PosingCapability posing, Vector2 buttonSize)
    {
        if(posing.Selected.Value is BonePoseInfoId boneId)
        {
            bool enabled = false;

            var bone = posing.SkeletonPosing.GetBone(boneId);
            bool isValid = bone != null && bone.Skeleton.IsValid && bone.EligibleForIK;

            if(isValid)
            {
                var bonePose = posing.SkeletonPosing.GetBonePose(boneId);

                var ik = bonePose.DefaultIK;
                enabled = ik.Enabled && BrioStyle.EnableStyle;

                using var popup = ImRaii.Popup("transform_ik_popup");
                {
                    if(popup.Success && bonePose != null)
                    {
                        BoneIKEditor.Draw(bonePose, posing);
                    }
                }
            }

            using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, enabled))
            {
                if(ImGui.Button("IK", buttonSize))
                    ImGui.OpenPopup("transform_ik_popup");

                ImBrio.AttachToolTip("Inverse Kinematics");
            }
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.Button("IK", buttonSize);
            ImGui.EndDisabled();
            ImBrio.AttachToolTip("Inverse Kinematics");
        }
    }
}
