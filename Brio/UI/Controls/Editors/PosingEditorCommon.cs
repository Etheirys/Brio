using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Game.Posing;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal static class PosingEditorCommon
{
    public static void DrawSelectionName(PosingCapability posing)
    {
        ImGui.Text(posing.Selected.DisplayName);

        ImGui.SetWindowFontScale(0.75f);
        ImGui.TextDisabled(posing.Selected.Subtitle);
        ImGui.SetWindowFontScale(1.0f);
    }

    public static void DrawImportOptionEditor(PoseImporterOptions options)
    {
        DrawBoneFilterEditor(options.BoneFilter);

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

    public static void DrawBoneFilterEditor(BoneFilter filter)
    {
        if(ImBrio.FontIconButton("select_all", Dalamud.Interface.FontAwesomeIcon.Check, "Select All"))
        {
            filter.EnableAll();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("select_none", Dalamud.Interface.FontAwesomeIcon.Minus, "Select None"))
        {
            filter.DisableAll();
        }

        ImGui.Separator();

        foreach(var category in filter.AllCategories)
        {
            var isEnabled = filter.IsCategoryEnabled(category);
            if(ImGui.Checkbox(category.Name, ref isEnabled))
            {
                if(isEnabled)
                    filter.EnableCategory(category);
                else
                    filter.DisableCategory(category);
            }
            if(ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                filter.EnableOnly(category);
            }
        }
    }

    public static void DrawMirrorModeSelect(PosingCapability posing, Vector2 buttonSize)
    {
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            var hasMirror = posing.Selected.Match(
                boneSelect => posing.SkeletonPosing.GetBonePose(boneSelect).GetMirrorBone() != null,
                _ => false,
                _ => false
            );

            using(ImRaii.Disabled(posing.Selected.Value is None || !hasMirror))
            {
                posing.Selected.Switch(
                    boneSelect =>
                    {
                        var poseInfo = posing.SkeletonPosing.GetBonePose(boneSelect);
                        switch(poseInfo.MirrorMode)
                        {
                            case PoseMirrorMode.None:
                                if(ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize))
                                    poseInfo.MirrorMode = PoseMirrorMode.Copy;
                                break;
                            case PoseMirrorMode.Copy:
                                if(ImGui.Button($"{FontAwesomeIcon.Link.ToIconString()}###mirror_mode", buttonSize))
                                    poseInfo.MirrorMode = PoseMirrorMode.Mirror;
                                break;
                            case PoseMirrorMode.Mirror:
                                if(ImGui.Button($"{FontAwesomeIcon.YinYang.ToIconString()}###mirror_mode", buttonSize))
                                    poseInfo.MirrorMode = PoseMirrorMode.None;
                                break;
                        }
                    },
                    _ => { ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize); },
                    _ => { ImGui.Button($"{FontAwesomeIcon.Unlink.ToIconString()}###mirror_mode", buttonSize); }
                );
            }
        }

        if(ImGui.IsItemHovered())
        {
            if(posing.Selected.Value is BonePoseInfoId poseInfo)
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

    public static void DrawIKSelect(PosingCapability posing)
    {
        DrawIKSelect(posing, Vector2.Zero);
    }

    public static void DrawIKSelect(PosingCapability posing, Vector2 buttonSize)
    {
        if(posing.Selected.Value is BonePoseInfoId boneId)
        {
            var bone = posing.SkeletonPosing.GetBone(boneId);
            bool isValid = bone != null && bone.Skeleton.IsValid && bone.EligibleForIK;

            if(isValid)
            {
                var bonePose = posing.SkeletonPosing.GetBonePose(boneId);
                //var ik = bonePose.DefaultIK;
                //bool enabled = ik.Enabled;
       
                if(ImGui.Button("IK", buttonSize))
                    ImGui.OpenPopup("transform_ik_popup");

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Inverse Kinematics");

                using var popup = ImRaii.Popup("transform_ik_popup");

                if(popup.Success && bonePose != null)
                {
                    BoneIKEditor.Draw(bonePose, posing);
                }

                return;
            }
        }

        ImGui.BeginDisabled();
        ImGui.Button("IK", buttonSize);
        ImGui.EndDisabled();
    }
}
