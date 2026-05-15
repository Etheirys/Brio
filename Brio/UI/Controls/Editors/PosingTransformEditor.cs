using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.Posing;
using Brio.Input;
using Brio.Services;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using OneOf.Types;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class PosingTransformEditor
{
    private Transform? _trackingTransform;
    private Vector3? _trackingEuler;
    private List<(EntityId id, PosingCapability capability, Transform transform)>? _groupedPendingSnapshot = null;
    private readonly ITransformableEditor _modelTransformEditor = new();

    public void Draw(string id, PosingCapability posingCapability, bool compactMode = false)
    {
        var selected = posingCapability.Selected;

        Vector2 style = new Vector2(4, 5);
        if(compactMode)
            style = new Vector2(4, 3);

        BonePoseInfoId? selectedIsBone = posingCapability.IsSelectedBone();
        bool isBone = false;
        Game.Posing.Skeletons.Bone? realBone = null;

        using var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, style);

        using(ImRaii.PushId(id))
        {
            if(selectedIsBone.HasValue && !posingCapability.Actor.IsProp)
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

                ImBrio.VerticalPadding(3);

                if(ImBrio.FontIconButton("transformOffset", FontAwesomeIcon.GaugeSimpleHigh, "Adjust how much each transform value changes per drag step."))
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

                        PosingEditorCommon.DrawIKSelect(posingCapability, new Vector2(25 * ImGuiHelpers.GlobalScale));

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

                        using(ImRaii.Disabled(parentBone is null))
                        {
                            if(ImBrio.FontIconButton(FontAwesomeIcon.LevelUpAlt))
                                posingCapability.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
                        }
                        ImBrio.AttachToolTip("Select Parent");
                    }
                }

                ImGui.SameLine();

                using(ImRaii.Disabled(selectedIsBone.HasValue)) // This is borken to all hell
                    if(ImBrio.FontIconButton("copypaste", FontAwesomeIcon.Clipboard, "Copy & Paste Transform"))
                        ImGui.OpenPopup("CopyPastePopup");
                if(selectedIsBone.HasValue)
                    ImBrio.AttachToolTip("Copy & Paste is currently only available for Model Transform");

                ImGui.SameLine();

                using(ImRaii.Disabled(!posingCapability.CanResetBone(realBone)))
                {
                    if(ImBrio.FontIconButtonRight("resetTransform", FontAwesomeIcon.Retweet, 1, tooltip: "Reset Bone"))
                    {
                        posingCapability.ResetSelectedBone();
                    }
                }

                if(selectedIsBone.HasValue)
                {

                }
                else
                {
                    var modTransform = posingCapability.ModelPosing.Transform;
                    if(Clipboard.DrawCopyPastePopup(ref modTransform, 1))
                    {
                        posingCapability.ModelPosing.Transform = modTransform;
                        posingCapability.Snapshot(false, false);
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
        var offset = posingCapability.ConfigurationService.Configuration.Interface.DefaultBoneTransformMovementSpeed;
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
            ImBrio.VerticalPadding(2);
            (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_0", ref realEuler, offset * 100, FontAwesomeIcon.ArrowsSpin, "Rotation", enableExpanded: compactMode);
            ImBrio.VerticalPadding(2);
            (var sdidChange, var sanyActive) = ImBrio.DragFloat3($"###_transformScale_0", ref realTransform.Scale, offset, FontAwesomeIcon.ExpandAlt, "Scale", enableExpanded: compactMode);
            ImBrio.VerticalPadding(2);

            didChange |= pdidChange |= rdidChange |= sdidChange;
            anyActive |= panyActive |= ranyActive |= sanyActive;

            realTransform.Rotation = realEuler.ToQuaternion();
            var toApply = before + realTransform.CalculateDiff(beforeMods);

            if(didChange && bone is not null && bonePose is not null)
            {
                if(posingCapability.IsMultiSelecting)
                {
                    var delta = realTransform.CalculateDiff(beforeMods);
                    foreach(var selectedBoneId in posingCapability.SelectedBones)
                    {
                        var targetBone = posingCapability.SkeletonPosing.GetBone(selectedBoneId);
                        if(targetBone != null && !targetBone.Freeze)
                        {
                            var targetBonePose = posingCapability.SkeletonPosing.GetBonePose(selectedBoneId);
                            var targetBoneTransform = targetBone.LastTransform;
                            var updatedTransform = targetBoneTransform + delta;
                            targetBonePose.Apply(updatedTransform, targetBoneTransform);
                            targetBonePose.DefaultPropagation = propagate;
                        }
                    }
                }
                else
                {
                    posingCapability.SkeletonPosing.GetBonePose(bone).Apply(toApply, before);
                    bonePose.DefaultPropagation = propagate;
                }
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
        if(!Brio.TryGetService(out EntityManager entityManager))
        {
            DrawModelTransformEditorSingle(posingCapability, compactMode);
            return;
        }

        var allTransformables = entityManager.GetAllSelectedTransformables();

        if(allTransformables.Count > 1)
        {
            DrawModelTransformEditorMulti(allTransformables, posingCapability, compactMode);
        }
        else
        {
            DrawModelTransformEditorSingle(posingCapability, compactMode);
        }
    }

    private void DrawModelTransformEditorSingle(PosingCapability posingCapability, bool compactMode = false)
    {
        var offset = posingCapability.ModelPosing.TransformOffset;

        if(!posingCapability.Actor.IsProp)
        {
            _modelTransformEditor.Draw("model_transform_single", posingCapability.Actor, offset, compactMode);
            return;
        }

        var before = posingCapability.ModelPosing.Transform;
        var realTransform = _trackingTransform ?? before;
        var realEuler = _trackingEuler ?? before.Rotation.ToEuler();

        using(ImRaii.Disabled(posingCapability.ModelPosing.IsTransformFrozen == true))
        {
            bool didChange = false;
            bool anyActive = false;

            (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transformPosition_1", ref realTransform.Position, offset, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position", enableExpanded: compactMode);
            ImBrio.VerticalPadding(2);
            (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_1", ref realEuler, offset * 100, FontAwesomeIcon.ArrowsSpin, "Rotation", enableExpanded: compactMode);
            ImBrio.VerticalPadding(2);

            ImBrio.Icon(FontAwesomeIcon.ExpandAlt);
            ImGui.SameLine();
            Vector2 size = new(0, 0) { X = ImBrio.GetRemainingWidth() + ImGui.GetStyle().ItemSpacing.X };
            float entryWidth = size.X - (ImGui.GetStyle().ItemSpacing.X * 2);
            ImGui.SetNextItemWidth(entryWidth);
            (var sanyActive, var sdidChange) = ImBrio.DragFloat($"##transformScale", ref realTransform.Scale.X, offset / 10);

            ImBrio.VerticalPadding(2);

            didChange |= pdidChange |= rdidChange |= sdidChange;
            anyActive |= panyActive |= ranyActive |= sanyActive;

            realTransform.Rotation = realEuler.ToQuaternion();

            if(didChange)
                posingCapability.ModelPosing.Transform = realTransform;

            if(anyActive)
            {
                _trackingTransform = realTransform;
                _trackingEuler = realEuler;
            }
            else
            {
                if(_trackingEuler.HasValue || _trackingTransform.HasValue)
                    posingCapability.Snapshot(false, false);

                _trackingTransform = null;
                _trackingEuler = null;
            }
        }
    }

    private void DrawModelTransformEditorMulti(List<(EntityId id, ITransformable target, Transform snapshot)> selected, PosingCapability primaryCapability, bool compactMode = false)
    {
        var centroid = TransformHelper.GetCentroidForGivenTransforms(selected.Select(t => t.target.Transform));

        var offset = primaryCapability.ModelPosing.TransformOffset;
        var primaryTransform = _trackingTransform ?? primaryCapability.ModelPosing.Transform;
        var beforeMods = primaryTransform;
        var realEuler = _trackingEuler ?? primaryTransform.Rotation.ToEuler();

        bool anyFrozen = selected.Any(t => t.target.IsTransformFrozen);

        using(ImRaii.Disabled(anyFrozen))
        {
            bool didChange = false;
            bool anyActive = false;

            (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transformPosition_1", ref primaryTransform.Position, offset, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position (Group)", enableExpanded: compactMode);
            ImBrio.VerticalPadding(2);
            (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_1", ref realEuler, offset * 100, FontAwesomeIcon.ArrowsSpin, "Rotation (Pivot)", enableExpanded: compactMode);
            ImBrio.VerticalPadding(2);
            (var sdidChange, var sanyActive) = ImBrio.DragFloat3($"###_transformScale_1", ref primaryTransform.Scale, offset, FontAwesomeIcon.ExpandAlt, "Scale (Group)", enableExpanded: compactMode);
            ImBrio.VerticalPadding(2);

            didChange |= pdidChange || rdidChange || sdidChange;
            anyActive |= panyActive || ranyActive || sanyActive;

            primaryTransform.Rotation = realEuler.ToQuaternion();

            if(didChange)
            {
                if(_groupedPendingSnapshot == null &&
                    Brio.TryGetService<HistoryService>(out HistoryService? historyService) &&
                    Brio.TryGetService<EntityManager>(out EntityManager? entityManager))
                {
                    _groupedPendingSnapshot = entityManager.GetAllSelectedActors();
                }

                var delta = primaryTransform.CalculateDiff(beforeMods);

                TransformHelper.ApplyDeltaToMultiple(selected, delta, centroid, rdidChange);

                if(pdidChange || rdidChange)
                    centroid = TransformHelper.GetCentroidForGivenTransforms(selected.Select(t => t.target.Transform));
            }

            if(anyActive)
            {
                _trackingTransform = primaryTransform;
                _trackingEuler = realEuler;
            }
            else
            {
                if(_trackingEuler.HasValue || _trackingTransform.HasValue)
                {
                    if(_groupedPendingSnapshot != null && _groupedPendingSnapshot.Count > 0)
                    {
                        if(Brio.TryGetService<HistoryService>(out HistoryService? historyService))
                            historyService.Snapshot(_groupedPendingSnapshot);

                        _groupedPendingSnapshot = null;
                    }

                    TransformHelper.SnapshotAll(selected.Select(t => t.target));
                }

                _trackingTransform = null;
                _trackingEuler = null;
            }
        }
    }

    private unsafe static void DrawTransformOffset(PosingCapability posingCapability)
    {
        using var popup = ImRaii.Popup("transformOffset");
        if(popup.Success)
        {
            BonePoseInfoId? selectedIsBone = posingCapability.IsSelectedBone();

            if(selectedIsBone.HasValue && !posingCapability.Actor.IsProp)
            {
                var bone = posingCapability.SkeletonPosing.GetBone(selectedIsBone);
                if(bone is not null)
                {
                    if(!bone.IsBoneAdjustmentSet)
                    {
                        bone.BoneAdjustmentOffset = posingCapability.ConfigurationService.Configuration.Interface.DefaultBoneTransformMovementSpeed;
                        bone.IsBoneAdjustmentSet = true;
                    }

                    ImBrio.DragFloat($"##transformSpeed_1", ref bone.BoneAdjustmentOffset, 0.001f, 10f, 0.01f, "Offset", 50);
                    bool freezeTransforms = bone.Freeze;
                    if(ImGui.Checkbox("Freeze Transforms", ref freezeTransforms))
                    {
                        bone.Freeze = freezeTransforms;
                    }
                }
            }
            else
            {
                ImBrio.DragFloat($"##transformSpeed_1", ref posingCapability.ModelPosing.TransformOffset, 0.001f, 10f, 0.01f, "Offset", 50);
                bool freezeTransforms = posingCapability.ModelPosing.IsTransformFrozen;
                if(ImGui.Checkbox("Freeze Transforms", ref freezeTransforms))
                {
                    posingCapability.ModelPosing.IsTransformFrozen = freezeTransforms;
                }
            }
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
