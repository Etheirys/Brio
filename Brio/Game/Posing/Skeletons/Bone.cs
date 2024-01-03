using Brio.Core;
using Brio.Resources;
using System;
using System.Collections.Generic;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CharacterBase;

namespace Brio.Game.Posing.Skeletons;

internal class Bone(int index, Skeleton skeleton, PartialSkeleton partial)
{
    public int Index = index;
    public string Name = null!;

    public Skeleton Skeleton = skeleton;

    public PartialSkeleton Partial = partial;
    public int PartialId => Partial.Id;

    public string FriendlyName => Localize.Get($"bones.{Name}", Name);

    public string FriendlyDescriptor
    {
        get
        {
            var end = $"{Name} ({PartialId}.{Index})";
            var friendly = Localize.GetNullable($"bones.{Name}");
            if (friendly != null)
                return $"{friendly} - {end}";
            return end;
        }
    }

    public Bone? Parent;

    public bool IsPartialRoot;
    public bool IsSkeletonRoot;

    public List<Bone> Children = [];
    public List<Skeleton> Attachments = [];

    public Transform LastTransform = Transform.Identity;

    public unsafe bool IsHidden
    {
        // Note: These should not be manipulate by users, but they are still needed when importing poses etc
        get
        {
            // If we're a partial root, or the root bone and we're not attached to anything, we should not be displayed
            if (IsPartialRoot && !(IsSkeletonRoot && Skeleton.AttachedTo != null))
                return true;

            // The jaw on partial 0 in humans is a special case, we don't want to display it
            if (Skeleton.CharacterBase != null)
                if (Skeleton.CharacterBase->CharacterBase.GetModelType() == ModelType.Human)
                    if (Name == "j_ago" && PartialId == 0)
                        return true;

            return false;
        }
    }

    public unsafe bool EligibleForIK => Parent != null && !Parent.IsHidden;

    public Bone? GetFirstVisibleParent()
    {
        if (Parent == null)
            return null;

        if (Parent.IsHidden)
            return Parent.GetFirstVisibleParent();

        return Parent;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, PartialId, Index);
    }

    public List<Bone> GetBonesToDepth(int depth, bool stopAtHidden, int? lastPartial = null, List<Bone>? bones = null)
    {
        bones ??= [];
        lastPartial ??= PartialId;

        bones.Add(this);

        if (depth == 0 || Parent == null)
            return bones;

        if(stopAtHidden && Parent.IsHidden)
            return bones;

        if(Parent.PartialId != lastPartial)
            return bones;

        return Parent.GetBonesToDepth(depth - 1, stopAtHidden, lastPartial, bones);
    }
}
