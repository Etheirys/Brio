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

    public void Draw(string id, PosingCapability posingCapability)
    {
        var selected = posingCapability.Selected;

        using(ImRaii.PushId(id))
        {
            selected.Switch(
                bone =>
                {
                    var realBone = posingCapability.SkeletonPosing.GetBone(bone);
                    if(realBone != null && realBone.Skeleton.IsValid)
                    {
                        DrawBoneTransformEditor(posingCapability, bone);
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

    private void DrawBoneTransformEditor(PosingCapability posingCapability, BonePoseInfoId boneId)
    {
        var bone = posingCapability.SkeletonPosing.GetBone(boneId);
        var bonePose = bone != null ? posingCapability.SkeletonPosing.GetBonePose(boneId) : null;

        var propagate = bonePose?.DefaultPropagation ?? TransformComponents.None;
        var before = bone?.LastTransform ?? Transform.Identity;
        var realTransform = _trackingTransform ?? before;
        var beforeMods = realTransform;

        var realEuler = _trackingEuler ?? realTransform.Rotation.ToEuler();

        bool didChange = false;
        bool anyActive = false;

        didChange |= ImBrio.DragFloat3("P", ref realTransform.Position, 0.1f, "Position");
        anyActive |= ImGui.IsItemActive();

        didChange |= ImBrio.DragFloat3("R", ref realEuler, 5.0f, "Rotation");
        anyActive |= ImGui.IsItemActive();

        didChange |= ImBrio.DragFloat3("S", ref realTransform.Scale, 0.1f, "Scale");
        anyActive |= ImGui.IsItemActive();


        ImGui.Spacing();
        ImGui.Text("Propagate: ");
        ImGui.SameLine();
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

        realTransform.Rotation = realEuler.ToQuaternion();
        var toApply = before + realTransform.CalculateDiff(beforeMods);

        if(didChange && bone != null && bonePose != null)
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
        var realTransform = _trackingTransform ?? before;
        var realEuler = _trackingEuler ?? before.Rotation.ToEuler();

        bool didChange = false;
        bool anyActive = false;

        didChange |= ImBrio.DragFloat3("P", ref realTransform.Position, 0.1f, "Position");
        anyActive |= ImGui.IsItemActive();

        didChange |= ImBrio.DragFloat3("R", ref realEuler, 5.0f, "Rotation");
        anyActive |= ImGui.IsItemActive();

        didChange |= ImBrio.DragFloat3("S", ref realTransform.Scale, 0.1f, "Scale");
        anyActive |= ImGui.IsItemActive();


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
