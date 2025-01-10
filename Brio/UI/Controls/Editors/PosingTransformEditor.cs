using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Game.Posing;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal class PosingTransformEditor
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
                selected.Switch(
                    bone =>
                    {
                        var realBone = posingCapability.SkeletonPosing.GetBone(bone);
                        if(realBone != null && realBone.Skeleton.IsValid && posingCapability.Actor.IsProp == false)
                        {
                            DrawBoneTransformEditor(posingCapability, bone, compactMode);
                        }
                        else
                        {
                            DrawModelTransformEditor(posingCapability);
                        }
                    },
                    _ => DrawModelTransformEditor(posingCapability),
                    _ => DrawModelTransformEditor(posingCapability)
                );
            }
        }
    }

    private void DrawBoneTransformEditor(PosingCapability posingCapability, BonePoseInfoId boneId, bool compactMode = false)
    {
        var bone = posingCapability.SkeletonPosing.GetBone(boneId);
        var bonePose = bone is not null ? posingCapability.SkeletonPosing.GetBonePose(boneId) : null;

        var propagate = bonePose?.DefaultPropagation ?? TransformComponents.None;
        var before = bone?.LastTransform ?? Transform.Identity;
        var realTransform = _trackingTransform ?? before;
        var beforeMods = realTransform;

        var realEuler = _trackingEuler ?? realTransform.Rotation.ToEuler();

        bool didChange = false;
        bool anyActive = false;

        (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transformPosition_0", ref realTransform.Position, 0.1f, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position");
        (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_0", ref realEuler, 1f, FontAwesomeIcon.ArrowsSpin, "Rotation");
        (var sdidChange, var sanyActive) = ImBrio.DragFloat3($"###_transformScale_0", ref realTransform.Scale, 0.1f, FontAwesomeIcon.ExpandAlt, "Scale");

        didChange |= pdidChange |= rdidChange |= sdidChange;
        anyActive |= panyActive |= ranyActive |= sanyActive;

        ImGui.Spacing();

        if(ImBrio.FontIconButton("propagate", FontAwesomeIcon.Compress, "Propagate", bone?.EligibleForIK == true))
            ImGui.OpenPopup("transform_propagate_popup");

        if(compactMode)
        {
            ImGui.SameLine();

            PosingEditorCommon.DrawIKSelect(posingCapability);
        }

        using(var popup = ImRaii.Popup("transform_propagate_popup"))
        {
            if(popup.Success && bonePose is not null)
            {
                didChange |= DrawPropagateCheckboxes(ref propagate);
            }
        }

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

    private void DrawModelTransformEditor(PosingCapability posingCapability)
    {
        var before = posingCapability.ModelPosing.Transform;
        var isProp = posingCapability.Actor.IsProp;
        var realTransform = _trackingTransform ?? before;
        var realEuler = _trackingEuler ?? before.Rotation.ToEuler();

        bool didChange = false;
        bool anyActive = false;

        (var pdidChange, var panyActive) = ImBrio.DragFloat3($"###_transformPosition_0", ref realTransform.Position, 0.1f, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position");
        (var rdidChange, var ranyActive) = ImBrio.DragFloat3($"###_transformRotation_0", ref realEuler, 5.0f, FontAwesomeIcon.ArrowsSpin, "Rotation");

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

            sdidChange |= ImGui.DragFloat($"##transformScale", ref realTransform.Scale.X, 0.1f / 10);
            sanyActive |= ImGui.IsItemActive();
        }
        else
            (sdidChange, sanyActive) = ImBrio.DragFloat3($"###_transformScale_0", ref realTransform.Scale, 0.1f, FontAwesomeIcon.ExpandAlt, "Scale");

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
