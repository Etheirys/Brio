using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Game.Posing;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

internal class PosingTransformEditor
{
    private Transform? _trackingTransform;
    private Vector3? _trackingEuler;

    public void Draw(string id, PosingCapability posingCapability, float? width = null)
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
                        DrawBoneTransformEditor(posingCapability, bone, width);
                    }
                    else
                    {
                        DrawModelTransformEditor(posingCapability, width);
                    }
                },
                _ => DrawModelTransformEditor(posingCapability, width),
                _ => DrawModelTransformEditor(posingCapability, width)
            );
        }
    }

    private void DrawBoneTransformEditor(PosingCapability posingCapability, BonePoseInfoId boneId, float? width)
    {
        if(width.HasValue)
            width -= ImGui.CalcTextSize("XXXX").X;

        var bone = posingCapability.SkeletonPosing.GetBone(boneId);
        var bonePose = bone != null ? posingCapability.SkeletonPosing.GetBonePose(boneId) : null;

        var propagate = bonePose?.DefaultPropagation ?? TransformComponents.None;
        var before = bone?.LastTransform ?? Transform.Identity;
        var realTransform = _trackingTransform ?? before;
        var beforeMods = realTransform;

        var realEuler = _trackingEuler ?? realTransform.Rotation.ToEuler();

        bool didChange = false;
        bool anyActive = false;


        if(width.HasValue)
            ImGui.PushItemWidth(width.Value);

        var text = "No Bone Selected";
        if(bone != null)
            text = bone.FriendlyDescriptor;
        ImGui.Text(text);

        didChange |= ImGui.DragFloat3("###position", ref realTransform.Position, 0.001f);
        anyActive |= ImGui.IsItemActive();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Position");
        ImGui.SameLine();
        bool propBool = propagate.HasFlag(TransformComponents.Position);
        if(ImGui.Checkbox("###propagate_position", ref propBool))
        {
            didChange |= true;
            propagate = propBool ? propagate | TransformComponents.Position : propagate & ~TransformComponents.Position;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Propagate");

        didChange |= ImGui.DragFloat3("###rotation", ref realEuler, 0.1f);
        anyActive |= ImGui.IsItemActive();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Rotation");
        ImGui.SameLine();
        propBool = propagate.HasFlag(TransformComponents.Rotation);
        if(ImGui.Checkbox("###propagate_rotation", ref propBool))
        {
            didChange |= true;
            propagate = propBool ? propagate | TransformComponents.Rotation : propagate & ~TransformComponents.Rotation;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Propagate");

        didChange |= ImGui.DragFloat3("###scale", ref realTransform.Scale, 0.001f);
        anyActive |= ImGui.IsItemActive();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Scale");
        ImGui.SameLine();
        propBool = propagate.HasFlag(TransformComponents.Scale);
        if(ImGui.Checkbox("###propagate_scale", ref propBool))
        {
            didChange |= true;
            propagate = propBool ? propagate | TransformComponents.Scale : propagate & ~TransformComponents.Scale;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Propagate");

        if(width.HasValue)
            ImGui.PopItemWidth();


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
                posingCapability.Snapshot();

            _trackingTransform = null;
            _trackingEuler = null;
        }
    }


    private void DrawModelTransformEditor(PosingCapability posingCapability, float? width)
    {
        var before = posingCapability.ModelPosing.Transform;
        var realTransform = _trackingTransform ?? before;
        var realEuler = _trackingEuler ?? before.Rotation.ToEuler();

        bool didChange = false;
        bool anyActive = false;


        if(width.HasValue)
            ImGui.PushItemWidth(width.Value);

        ImGui.Text("Model Transform");

        didChange |= ImGui.DragFloat3("###position", ref realTransform.Position, 0.001f);
        anyActive |= ImGui.IsItemActive();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Position");


        didChange |= ImGui.DragFloat3("###rotation", ref realEuler, 0.1f);
        anyActive |= ImGui.IsItemActive();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Rotation");


        didChange |= ImGui.DragFloat3("###scale", ref realTransform.Scale, 0.001f);
        anyActive |= ImGui.IsItemActive();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Scale");


        if(width.HasValue)
            ImGui.PopItemWidth();


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
                posingCapability.Snapshot();

            _trackingTransform = null;
            _trackingEuler = null;
        }
    }
}
