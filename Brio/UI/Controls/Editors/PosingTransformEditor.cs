﻿using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Game.Posing;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class PosingTransformEditor
{
    private Transform? _trackingTransform;
    private Vector3? _trackingEuler;

    public void Draw(string id, PosingCapability posingCapability, bool compactMode = false)
    {
        var selected = posingCapability.Selected;

        Vector2 style = new Vector2(4, 5);
        if(compactMode)
            style = new Vector2(4, 3);

        using(ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, style))
        {
            using(ImRaii.PushId(id))
            {
                BonePoseInfoId? selectedIsBone = posingCapability.IsSelectedBone();
                bool isBone = false;
                Game.Posing.Skeletons.Bone? realBone = null;

                if (selectedIsBone.HasValue && !posingCapability.Actor.IsProp)
                {
                    isBone = true;
                    realBone = posingCapability.SkeletonPosing.GetBone(selectedIsBone.Value);
                    DrawBoneTransformEditor(posingCapability, selectedIsBone.Value, compactMode);
                }
                else
                {
                    DrawModelTransformEditor(posingCapability, compactMode);
                }

                if(posingCapability.Actor.IsProp == false)
                {
                    if(ImBrio.FontIconButton("transformOffset", FontAwesomeIcon.GaugeSimpleHigh, "Transform Offset"))
                    { 
                        ImGui.OpenPopup("transformOffset");
                    }

                    DrawTransformOffset(posingCapability);

                    ImGui.SameLine();

                    using(ImRaii.Disabled(isBone == false))
                    {
                        if(ImBrio.FontIconButton("propagate", FontAwesomeIcon.Compress, "Propagate", realBone?.EligibleForIK == true))
                            ImGui.OpenPopup("transform_propagate_popup");

                        if(compactMode)
                        {
                            ImGui.SameLine();

                            PosingEditorCommon.DrawIKSelect(posingCapability, new Vector2(25));

                            ImGui.SameLine();

                            using(ImRaii.Disabled(posingCapability.Selected.Value is None))
                            {
                                if(ImBrio.FontIconButton("clear_selection", FontAwesomeIcon.MinusSquare, "Clear Selection"))
                                    posingCapability.ClearSelection();
                            }

                            // Select Parent
                            ImGui.SameLine();
                            var parentBone = posingCapability.Selected.Match(
                                   boneSelect => posingCapability.SkeletonPosing.GetBone(boneSelect)?.GetFirstVisibleParent(),
                                   _ => null,
                                   _ => null
                            );

                            using(ImRaii.Disabled(parentBone == null))
                            {
                                if(ImBrio.FontIconButton(FontAwesomeIcon.LevelUpAlt, new Vector2(25)))
                                    posingCapability.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
                            }

                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip("Select Parent");
                        }
                    }
                    ImGui.SameLine();

                    using(ImRaii.Disabled(true))
                        if(ImBrio.FontIconButton("copypaste", FontAwesomeIcon.Clipboard, "Copy/Paste"))
                            ImGui.OpenPopup("transform_CopyPaste_popup");

                    if(isBone == false)
                        using(ImRaii.Disabled(posingCapability.ModelPosing.HasOverride == false))
                        {
                            ImGui.SameLine();

                            if(ImBrio.FontIconButtonRight("resetTransform", FontAwesomeIcon.Recycle, 1, tooltip: "Reset Model Transform"))
                            {
                                posingCapability.ModelPosing.ResetTransform();
                            }
                        }

                    using(var popup = ImRaii.Popup("transform_CopyPaste_popup"))
                    {
                        if(popup.Success)
                            DrawCopyPastePopup();
                    }
                }
            }
        }
    }

    private void DrawBoneTransformEditor(PosingCapability posingCapability, BonePoseInfoId boneId, bool compactMode = false)
    {
        bool didChange = false;
        bool anyActive = false;

        var bone = posingCapability.SkeletonPosing.GetBone(boneId);
        var offset = bone?.BoneAdjustmentOffset ?? 0.01f;
        var bonePose = bone is not null ? posingCapability.SkeletonPosing.GetBonePose(boneId) : null;

        var propagate = bonePose?.DefaultPropagation ?? TransformComponents.None;
        var before = bone?.LastTransform ?? Transform.Identity;
        var realTransform = _trackingTransform ?? before;
        var beforeMods = realTransform;

        var realEuler = _trackingEuler ?? realTransform.Rotation.ToEuler();

        using(ImRaii.Disabled(bone != null && bone.Freeze))
        {
            using(var popup = ImRaii.Popup("transform_propagate_popup"))
            {
                if(popup.Success && bonePose is not null)
                {
                    didChange |= DrawPropagateCheckboxes(ref propagate);
                }
            }

            (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transformPosition_0", ref realTransform.Position, offset, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position", enableExpanded: compactMode);
            (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_0", ref realEuler, offset * 100, FontAwesomeIcon.ArrowsSpin, "Rotation", enableExpanded: compactMode);
            (var sdidChange, var sanyActive) = ImBrio.DragFloat3($"###_transformScale_0", ref realTransform.Scale, offset, FontAwesomeIcon.ExpandAlt, "Scale", enableExpanded: compactMode);

            didChange |= pdidChange |= rdidChange |= sdidChange;
            anyActive |= panyActive |= ranyActive |= sanyActive;

            realTransform.Rotation = realEuler.ToQuaternion();
            var toApply = before + realTransform.CalculateDiff(beforeMods);

            if(didChange && bone is not null && bonePose is not null)
            {
                posingCapability.SkeletonPosing.GetBonePose(bone).Apply(toApply, before);
                bonePose.DefaultPropagation = propagate;
            }

            if(anyActive)
            {
                _trackingTransform = realTransform;
                _trackingEuler = realEuler;
            }
            else
            {
                if(_trackingEuler.HasValue || _trackingTransform.HasValue)
                {
                    posingCapability.Snapshot(false, false);
                }

                _trackingTransform = null;
                _trackingEuler = null;
            }
        }
    }

    private void DrawModelTransformEditor(PosingCapability posingCapability, bool compactMode = false)
    {
        var before = posingCapability.ModelPosing.Transform;
        var isProp = posingCapability.Actor.IsProp;
        var offset = posingCapability.ModelPosing.TransformOffset;
        var realTransform = _trackingTransform ?? before;
        var realEuler = _trackingEuler ?? before.Rotation.ToEuler();

        using(ImRaii.Disabled(posingCapability.ModelPosing.Freeze == true))
        { 
            bool didChange = false;
            bool anyActive = false;

            (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transformPosition_1", ref realTransform.Position, offset, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position", enableExpanded: compactMode);
            (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_1", ref realEuler, offset * 100, FontAwesomeIcon.ArrowsSpin, "Rotation", enableExpanded: compactMode);

            bool sdidChange = false;
            bool sanyActive = false;

            if(isProp)
            {
                ImBrio.Icon(FontAwesomeIcon.ExpandAlt);

                ImGui.SameLine();

                Vector2 size = new(0, 0)
                {
                    X = ImBrio.GetRemainingWidth() + ImGui.GetStyle().ItemSpacing.X
                };

                float entryWidth = (size.X - (ImGui.GetStyle().ItemSpacing.X * 2));
                ImGui.SetNextItemWidth(entryWidth);

                (sanyActive, sdidChange) = ImBrio.DragFloat($"##transformScale", ref realTransform.Scale.X, offset / 10);
            }
            else
                (sdidChange, sanyActive) = ImBrio.DragFloat3($"###_transformScale_1", ref realTransform.Scale, offset, FontAwesomeIcon.ExpandAlt, "Scale", enableExpanded: compactMode);

            didChange |= pdidChange |= rdidChange |= sdidChange;
            anyActive |= panyActive |= ranyActive |= sanyActive;

            realTransform.Rotation = realEuler.ToQuaternion();

            if(didChange)
            {
                posingCapability.ModelPosing.Transform = realTransform;
            }

            if(anyActive)
            {
                _trackingTransform = realTransform;
                _trackingEuler = realEuler;
            }
            else
            {
                if(_trackingEuler.HasValue || _trackingTransform.HasValue)
                {
                    posingCapability.Snapshot(false, false);
                }

                _trackingTransform = null;
                _trackingEuler = null;
            }
        }
    }

    private static void DrawCopyPastePopup()
    {
        if(ImGui.Button("Copy"))
        {

        }
        if(ImGui.Button("Paste"))
        {

        }
    }

    private static void DrawTransformBoneOffset(PosingCapability posingCapability, BonePoseInfoId boneId)
    {
        using var popup = ImRaii.Popup("transformOffset");
        if(popup.Success)
        {
            var bone = posingCapability.SkeletonPosing.GetBone(boneId);
            if(bone is not null)
            {
                ImBrio.DragFloat($"##transformSpeed_1", ref bone.BoneAdjustmentOffset, 0.001f, 10, 0.01f, "Offset", 50);
                bool freezeTransforms = bone.Freeze;
                if(ImGui.Checkbox("Freeze Transforms", ref freezeTransforms))
                {
                    bone.Freeze = freezeTransforms;
                }
            }
        }
    }

    private static void DrawTransformModelOffset(PosingCapability posingCapability)
    {
        using var popup = ImRaii.Popup("transformOffset");
        if(popup.Success)
        {
            ImBrio.DragFloat($"##transformSpeed_1", ref posingCapability.ModelPosing.TransformOffset, 0.001f, 10, 0.01f, "Offset", 50);
            bool freezeTransforms = posingCapability.ModelPosing.Freeze;
            if(ImGui.Checkbox("Freeze Transforms", ref freezeTransforms))
            {
                posingCapability.ModelPosing.Freeze = freezeTransforms;
            }
        }
    }
    private unsafe static void DrawTransformOffset(PosingCapability posingCapability)
    {
        BonePoseInfoId? selectedIsBone = posingCapability.IsSelectedBone();

        if(selectedIsBone.HasValue && !posingCapability.Actor.IsProp)
        {
            DrawTransformBoneOffset(posingCapability, selectedIsBone.Value);
        }
        else
        {
            DrawTransformModelOffset(posingCapability);
        }
    }

    private static bool DrawPropagateCheckboxes(ref TransformComponents propagate)
    {
        var didChange = false;

        bool propBool = propagate.HasFlag(TransformComponents.Position);
        if(ImGui.Checkbox("P###propagate_position", ref propBool))
        {
            didChange |= true;
            propagate = propBool ? propagate | TransformComponents.Position : propagate & ~TransformComponents.Position;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Propagate Positions");

        ImGui.SameLine();
        propBool = propagate.HasFlag(TransformComponents.Rotation);
        if(ImGui.Checkbox("R###propagate_rotation", ref propBool))
        {
            didChange |= true;
            propagate = propBool ? propagate | TransformComponents.Rotation : propagate & ~TransformComponents.Rotation;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Propagate Rotations");

        ImGui.SameLine();

        propBool = propagate.HasFlag(TransformComponents.Scale);
        if(ImGui.Checkbox("S###propagate_scale", ref propBool))
        {
            didChange |= true;
            propagate = propBool ? propagate | TransformComponents.Scale : propagate & ~TransformComponents.Scale;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Propagate Scales");

        return didChange;
    }
}
