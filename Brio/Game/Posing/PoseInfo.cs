using Brio.Core;
using Brio.Game.Posing.Skeletons;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Game.Posing;

internal class PoseInfo
{
    private readonly Dictionary<BonePoseInfoId, BonePoseInfo> _poses = [];

    public BonePoseInfo GetPoseInfo(BonePoseInfoId id)
    {
        if (_poses.TryGetValue(id, out var pose))
            return pose;

        return _poses[id] = new BonePoseInfo(id, this);
    }

    public bool IsOveridden => _poses.Any(x => x.Value.HasStacks);

    public unsafe BonePoseInfo GetPoseInfo(Bone bone, PoseInfoSlot slot = PoseInfoSlot.Character) => GetPoseInfo(new BonePoseInfoId(bone.Name, bone.PartialId, slot));

    public void Clear()
    {
        _poses.Clear();
    }

    public PoseInfo Clone()
    {
        var clone = new PoseInfo();
        foreach (var pose in _poses)
        {
            clone._poses.Add(pose.Key, pose.Value.Clone(clone));
        }
        return clone;
    }
}

internal class BonePoseInfo(BonePoseInfoId id, PoseInfo parent)
{
    public BonePoseInfoId Id { get; } = id;
    public PoseInfo Parent { get; } = parent;

    public string Name => Id.BoneName;
    public int Partial => Id.Partial;
    public PoseInfoSlot Slot => Id.Slot;

    public TransformComponents DefaultPropagation { get; set; } = TransformComponents.Position | TransformComponents.Rotation;

    public IReadOnlyList<BonePoseTransformInfo> Stacks => _stacks;

    public PoseMirrorMode MirrorMode { get; set; } = PoseMirrorMode.None;

    private readonly List<BonePoseTransformInfo> _stacks = [];

    public bool HasStacks => _stacks.Any();

    public void Apply(Transform transform, Transform? original = null, TransformComponents? propagation = null, TransformComponents applyTo = TransformComponents.All, PoseMirrorMode? mirrorMode = null, bool forceNewStack = false)
    {
        var prop = propagation ?? DefaultPropagation;
        var calc = original.HasValue ? transform.CalculateDiff(original.Value) : transform;
        var transformIndex = GetTransformIndex(prop, forceNewStack);
        mirrorMode ??= MirrorMode;

        var existing = _stacks[transformIndex].Transform;

        calc.Filter(applyTo);
        if (Transform.Identity.IsApproximatelySame(calc + existing))
            return;

        if (mirrorMode == PoseMirrorMode.Copy)
        {
            GetMirrorBone()?.Apply(calc, null, prop, applyTo, PoseMirrorMode.None, forceNewStack);
        }
        else if (mirrorMode == PoseMirrorMode.Mirror)
        {
            var inverted = calc.Inverted();
            GetMirrorBone()?.Apply(inverted, null, prop, applyTo, PoseMirrorMode.None, forceNewStack);
        }

        _stacks[transformIndex] = new(prop, _stacks[transformIndex].Transform + calc);
    }

    public BonePoseInfo Clone(PoseInfo parent)
    {
        var clone = new BonePoseInfo(Id, parent)
        {
            DefaultPropagation = DefaultPropagation,
            MirrorMode = MirrorMode
        };

        clone._stacks.AddRange(_stacks.ToList());

        return clone;
    }

    public BonePoseInfo? GetMirrorBone()
    {
        var mirror = Id.GetMirrorBone();
        if (mirror.HasValue)
            return Parent.GetPoseInfo(mirror.Value);

        return null;
    }

    private int GetTransformIndex(TransformComponents components, bool forceNewStack)
    {
        if (_stacks.Count == 0)
        {
            _stacks.Add(new(components, Transform.Identity));
            return 0;
        }

        if (!forceNewStack)
        {
            var entry = _stacks[^1];
            if (entry.PropagateComponents == components)
                return _stacks.Count - 1;
        }

        _stacks.Add(new(components, Transform.Identity));
        return _stacks.Count - 1;
    }
}

internal enum PoseInfoSlot
{
    Character,
    MainHand,
    OffHand,
    Prop,
    Unknown
}

internal enum PoseMirrorMode
{
    None,
    Mirror,
    Copy
}

internal record struct BonePoseInfoId(string BoneName, int Partial, PoseInfoSlot Slot)
{
    public override readonly string ToString() => $"{BoneName}/{Partial}/{(int)Slot}";

    public readonly BonePoseInfoId? GetMirrorBone()
    {
        if (BoneName.EndsWith("_r"))
        {
            var mirrorName = BoneName.Substring(0, BoneName.Length - 2) + "_l";
            return new BonePoseInfoId(mirrorName, Partial, Slot);
        }

        if (BoneName.EndsWith("_l"))
        {
            var mirrorName = BoneName.Substring(0, BoneName.Length - 2) + "_r";
            return new BonePoseInfoId(mirrorName, Partial, Slot);
        }

        return null;
    }
}
internal record struct BonePoseTransformInfo(TransformComponents PropagateComponents, Transform Transform);
