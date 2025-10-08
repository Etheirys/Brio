using Brio.Core;
using Brio.Game.Posing.Skeletons;
using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Game.Posing;

public class PoseInfo
{
    private readonly Dictionary<BonePoseInfoId, BonePoseInfo> _poses = [];

    public BonePoseInfo GetPoseInfo(BonePoseInfoId id)
    {
        if(_poses.TryGetValue(id, out var pose))
            return pose;

        return _poses[id] = new BonePoseInfo(id, this);
    }

    public bool IsOverridden => _poses.Any(x => x.Value.HasStacks);

    public bool HasIKStacks => _poses.Any(x => x.Value.Stacks.Any(s => s.IKInfo.Enabled));

    public Dictionary<string, int> StackCounts
    {
        get
        {
            Dictionary<string, int> counts = [];
            foreach(var pose in _poses)
            {
                if(pose.Value.Stacks.Count > 0)
                    counts[pose.Key.BoneName] = pose.Value.Stacks.Count;
            }
            return counts;
        }
    }

    public unsafe BonePoseInfo GetPoseInfo(Bone bone, PoseInfoSlot slot = PoseInfoSlot.Character) => GetPoseInfo(new BonePoseInfoId(bone.Name, bone.PartialId, slot));

    public void Clear()
    {
        foreach(var pose in _poses)
        {
            pose.Value.ClearStacks();
        }
    }

    public PoseInfo Clone()
    {
        var clone = new PoseInfo();
        foreach(var pose in _poses)
        {
            clone._poses.Add(pose.Key, pose.Value.Clone(clone));
        }
        return clone;
    }
}

public class BonePoseInfo(BonePoseInfoId id, PoseInfo parent)
{
    public BonePoseInfoId Id { get; } = id;
    public PoseInfo Parent { get; } = parent;

    public string Name => Id.BoneName;
    public int Partial => Id.Partial;
    public PoseInfoSlot Slot => Id.Slot;

    public TransformComponents DefaultPropagation { get; set; } = TransformComponents.Position | TransformComponents.Rotation;

    public BoneIKInfo DefaultIK { get; set; } = BoneIKInfo.CalculateDefault(id.BoneName);

    public IReadOnlyList<BonePoseTransformInfo> Stacks => _stacks;

    public PoseMirrorMode MirrorMode { get; set; } = PoseMirrorMode.None;

    private readonly List<BonePoseTransformInfo> _stacks = [];

    public bool HasStacks => _stacks.Count != 0;

    public Transform? Apply(Transform transform, Transform? original = null, TransformComponents? propagation = null, TransformComponents applyTo = TransformComponents.All, BoneIKInfo? ikInfo = null, PoseMirrorMode? mirrorMode = null, bool forceNewStack = false)
    {
        var prop = propagation ?? DefaultPropagation;
        ikInfo ??= DefaultIK;
        var calc = original.HasValue ? transform.CalculateDiff(original.Value) : transform;

        if(calc.IsApproximatelySame(Transform.Identity))
            return null;

        var transformIndex = GetTransformIndex(prop, ikInfo.Value, forceNewStack);
        mirrorMode ??= MirrorMode;

        var existing = _stacks[transformIndex].Transform;

        calc.Filter(applyTo);

        if(Transform.Identity.IsApproximatelySame(calc + existing))
            return null;

        if(mirrorMode == PoseMirrorMode.Copy)
        {
            GetMirrorBone()?.Apply(calc, null, prop, applyTo, ikInfo.Value, PoseMirrorMode.None, forceNewStack);
        }
        else if(mirrorMode == PoseMirrorMode.Mirror)
        {
            var inverted = calc.Inverted();
            GetMirrorBone()?.Apply(inverted, null, prop, applyTo, ikInfo.Value, PoseMirrorMode.None, forceNewStack);
        }

        var finaleTransform = _stacks[transformIndex].Transform + calc;

        if(finaleTransform.IsRotationNaN())
        {
            finaleTransform.Rotation = Quaternion.Identity;
            Brio.Log.Warning($"IsRotationNaN !!!!!!!!!");
        }
        else if(finaleTransform.IsPositionNaN())
        {
            finaleTransform.Position = Vector3.Zero;
            Brio.Log.Warning($"IsPositionNaN !!!!!!!!!");
        }
        else if(finaleTransform.IsScaleNaN())
        {
            finaleTransform.Scale = Vector3.Zero;
            Brio.Log.Warning($"IsScaleNaN !!!!!!!!!");
        }

        _stacks[transformIndex] = new(prop, ikInfo.Value, finaleTransform);

        return finaleTransform;
    }

    public void RemoveLastStack()
    {
        if(_stacks.Count > 0)
            _stacks.RemoveAt(_stacks.Count - 1);
    }

    public void ClearStacks()
    {
        _stacks.Clear();
    }

    public BonePoseInfo Clone(PoseInfo parent)
    {
        var clone = new BonePoseInfo(Id, parent)
        {
            DefaultPropagation = DefaultPropagation,
            MirrorMode = MirrorMode
        };

        clone._stacks.AddRange([.. _stacks]);

        return clone;
    }

    public BonePoseInfo? GetMirrorBone()
    {
        var mirror = Id.GetMirrorBone();
        if(mirror.HasValue)
            return Parent.GetPoseInfo(mirror.Value);

        return null;
    }

    private int GetTransformIndex(TransformComponents components, BoneIKInfo ikInfo, bool forceNewStack)
    {
        if(_stacks.Count == 0)
        {
            _stacks.Add(new(components, ikInfo, Transform.Identity));
            return 0;
        }

        if(!forceNewStack)
        {
            var entry = _stacks[^1];
            if(entry.PropagateComponents == components && entry.IKInfo.Equals(ikInfo))
                return _stacks.Count - 1;
        }

        _stacks.Add(new(components, ikInfo, Transform.Identity));
        return _stacks.Count - 1;
    }
}

public enum PoseInfoSlot
{
    Character,
    MainHand,
    OffHand,
    Prop,
    Unknown
}

public enum PoseMirrorMode
{
    None,
    Mirror,
    Copy
}

public record struct BonePoseInfoId(string BoneName, int Partial, PoseInfoSlot Slot)
{
    public override readonly string ToString() => $"{BoneName}/{Partial}/{(int)Slot}";

    public readonly BonePoseInfoId? GetMirrorBone()
    {
        if(BoneName.EndsWith("_r"))
        {
            var mirrorName = string.Concat(BoneName.AsSpan(0, BoneName.Length - 2), "_l");
            return new BonePoseInfoId(mirrorName, Partial, Slot);
        }

        if(BoneName.EndsWith("_l"))
        {
            var mirrorName = string.Concat(BoneName.AsSpan(0, BoneName.Length - 2), "_r");
            return new BonePoseInfoId(mirrorName, Partial, Slot);
        }

        return null;
    }
}
public record struct BonePoseTransformInfo(TransformComponents PropagateComponents, BoneIKInfo IKInfo, Transform Transform);

public struct BoneIKInfo
{
    public bool Enabled = false;

    public bool EnforceConstraints = true;

    public OneOf<CCDOptions, TwoJointOptions> SolverOptions = new CCDOptions();

    public readonly static BoneIKInfo Disabled = new();

    public static bool CanUseJoint(string boneName) => boneName.StartsWith("j_te") || boneName.StartsWith("j_asi_d");

    public static BoneIKInfo CalculateDefault(string boneName, bool allowJoint = true)
    {
        var result = new BoneIKInfo();

        if(allowJoint && CanUseJoint(boneName))
        {
            if(boneName.StartsWith("j_te"))
            {
                var options = new TwoJointOptions()
                {
                    FirstBone = 2,
                    SecondBone = 1,
                    EndBone = 0,
                    RotationAxis = Vector3.UnitZ
                };
                result.SolverOptions = options;
            }

            if(boneName.StartsWith("j_asi_d"))
            {
                var options = new TwoJointOptions()
                {
                    FirstBone = 3,
                    SecondBone = 1,
                    EndBone = 0,
                    RotationAxis = -Vector3.UnitZ
                };
                result.SolverOptions = options;
            }
        }

        return result;
    }


    public BoneIKInfo()
    {

    }

    public struct CCDOptions()
    {
        public int Depth = 3;
        public int Iterations = 8;
    }

    public struct TwoJointOptions()
    {
        public int FirstBone = -1;
        public int SecondBone = -1;
        public int EndBone = -1;
        public Vector3 RotationAxis = Vector3.Zero;
    }
}
