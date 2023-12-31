using Brio.Core;
using Brio.Game.Actor.Interop;
using FFXIVClientStructs.Havok;
using System;
using System.Collections.Generic;
using static FFXIVClientStructs.Havok.hkaPose;
using GameSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

namespace Brio.Game.Posing.Skeletons;

internal class Skeleton : IDisposable
{
    public List<PartialSkeleton> Partials { get; } = [];

    public PartialSkeleton RootPartial { get; init; } = null!;

    public Bone RootBone { get; init; } = null!;

    public bool IsValid { get; set; } = false;

    public unsafe BrioCharacterBase* CharacterBase => (BrioCharacterBase*)GameSkeleton->Owner;

    public unsafe GameSkeleton* GameSkeleton { get; init; }

    public List<Bone> Bones { get; set; } = [];

    public List<Skeleton> Attachments
    {
        get
        {
            var result = new List<Skeleton>();
            foreach (var bone in Bones)
                result.AddRange(bone.Attachments);

            return result;
        }
    }

    public Bone? AttachedTo { get; set; } = null;


    private const int MaxPoses = 4;

    public unsafe Skeleton(GameSkeleton* gameSkeleton)
    {
        GameSkeleton = gameSkeleton;

        var partialCount = gameSkeleton->PartialSkeletonCount;
        for (int partialIdx = 0; partialIdx < partialCount; partialIdx++)
        {
            var partial = &gameSkeleton->PartialSkeletons[partialIdx];
            var newPartial = new PartialSkeleton(this, partialIdx);
            Partials.Add(newPartial);

            for (int poseIdx = 0; poseIdx < MaxPoses; poseIdx++)
            {
                var pose = partial->GetHavokPose(poseIdx);
                if (pose != null)
                {
                    newPartial.Poses.Add((nint)pose);
                    var boneCount = pose->Skeleton->Bones.Length;
                    for (int boneIdx = 0; boneIdx < boneCount; boneIdx++)
                    {

                        var rawBone = pose->Skeleton->Bones[boneIdx];
                        var boneName = rawBone.Name.String!;
                        var parentIndex = pose->Skeleton->ParentIndices[boneIdx];

                        var bone = newPartial.GetOrCreateBone(boneIdx);
                        bone.Name = boneName;

                        if (!Bones.Contains(bone))
                            Bones.Add(bone);

                        if (parentIndex < 0)
                        {
                            if (partialIdx == 0)
                            {
                                RootPartial = newPartial;
                                RootBone = bone;
                                bone.IsPartialRoot = true;
                                bone.IsSkeletonRoot = true;
                            }

                            newPartial.RootBone = bone;
                        }
                        else
                        {
                            var parentBone = newPartial.GetOrCreateBone(parentIndex);
                            bone.Parent = parentBone;
                            if (parentBone.Children.Contains(bone) == false)
                                parentBone.Children.Add(bone);
                        }
                    }
                }

            }

            if (partialIdx != 0)
            {
                if (partial->ConnectedBoneIndex >= 0 && partial->ConnectedParentBoneIndex >= 0)
                {
                    var parent = Partials[0].GetOrCreateBone(partial->ConnectedParentBoneIndex);
                    var child = newPartial.GetOrCreateBone(partial->ConnectedBoneIndex);
                    child.IsPartialRoot = true;
                    newPartial.ParentBone = parent;
                    newPartial.RootBone = child;

                    parent.Children.Add(child);
                    child.Parent = parent;
                }
            }
        }

        IsValid = true;
    }

    public unsafe static Skeleton? Create(GameSkeleton* gameSkeleton)
    {
        if (gameSkeleton == null)
            return null;

        return new Skeleton(gameSkeleton);
    }

    public unsafe static Skeleton? Create(BrioCharacterBase* charaBase)
    {
        if (charaBase == null)
            return null;

        if (charaBase->CharacterBase.Skeleton == null)
            return null;

        return new Skeleton(charaBase->CharacterBase.Skeleton);
    }

    public unsafe void UpdateCachedTransforms()
    {
        foreach (var partial in Partials)
        {
            if (partial.Poses.Count == 0)
                continue;

            hkaPose* pose = (hkaPose*)partial.Poses[0];
            var boneCount = pose->Skeleton->Bones.Length;
            for (int boneIdx = 0; boneIdx < boneCount; boneIdx++)
            {
                var bone = partial.Bones[boneIdx];
                Transform pos = pose->AccessBoneModelSpace(boneIdx, PropagateOrNot.DontPropagate);
                bone.LastTransform = pos;
            }
        }
    }

    public void ClearAttachments()
    {
        AttachedTo = null;
        foreach (var bone in Bones)
        {
            bone.Attachments.Clear();
        }
    }

    public Bone? GetFirstVisibleBone(string name)
    {
        foreach (var partial in Partials)
        {
            var bone = partial.GetBone(name);
            if (bone != null && !bone.IsHidden)
                return bone;
        }
        return null;
    }

    public void Dispose()
    {
        IsValid = false;
    }
}

internal enum SkeletonType
{
    Character,
    MainHandWeapon,
    OffHandWeapon
}