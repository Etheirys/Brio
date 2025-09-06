using Brio.Core;
using Brio.Game.Actor.Interop;
using FFXIVClientStructs.Havok.Animation.Rig;
using System;
using System.Collections.Generic;
using static FFXIVClientStructs.Havok.Animation.Rig.hkaPose;
using GameSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

namespace Brio.Game.Posing.Skeletons;

public class Skeleton : IDisposable
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
            foreach(var bone in Bones)
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
        for(int partialIdx = 0; partialIdx < partialCount; partialIdx++)
        {
            var partial = &gameSkeleton->PartialSkeletons[partialIdx];
            var newPartial = new PartialSkeleton(this, partialIdx);
            Partials.Add(newPartial);

            for(int poseIdx = 0; poseIdx < MaxPoses; poseIdx++)
            {
                var pose = partial->GetHavokPose(poseIdx);
                if(pose != null)
                {
                    newPartial.Poses.Add((nint)pose);
                    var boneCount = pose->Skeleton->Bones.Length;
                    for(int boneIdx = 0; boneIdx < boneCount; boneIdx++)
                    {

                        var rawBone = pose->Skeleton->Bones[boneIdx];
                        var boneName = rawBone.Name.String!;
                        var parentIndex = pose->Skeleton->ParentIndices[boneIdx];

                        var bone = newPartial.GetOrCreateBone(boneIdx);
                        bone.Name = boneName;

                        if(!Bones.Contains(bone))
                            Bones.Add(bone);

                        if(parentIndex < 0)
                        {
                            if(partialIdx == 0)
                            {
                                RootPartial = newPartial;
                                RootBone = bone;
                                bone.IsSkeletonRoot = true;
                            }

                            bone.IsPartialRoot = true;
                            newPartial.RootBones.Add(bone);
                        }
                        else
                        {
                            var parentBone = newPartial.GetOrCreateBone(parentIndex);
                            bone.Parent = parentBone;
                            if(parentBone.Children.Contains(bone) == false)
                                parentBone.Children.Add(bone);
                        }
                    }
                }

            }

            if(partialIdx != 0)
            {
                if(newPartial.RootBones.Count == 1)
                {
                    // Single-root partials are mapped by connected bone indices
                    var parentBone = Partials[0].GetOrCreateBone(partial->ConnectedParentBoneIndex);
                    var bone = newPartial.GetOrCreateBone(partial->ConnectedBoneIndex);

                    bone.Parent = parentBone;
                    if(!parentBone.Children.Contains(bone))
                        parentBone.Children.Add(bone);
                }
                else
                {
                    // Multi-root partials are mapped through their names
                    foreach(Bone bone in newPartial.RootBones)
                    {
                        var parentBone = Partials[0].GetBone(bone.Name);
                        if(parentBone != null)
                        {
                            bone.Parent = parentBone;
                            if(!parentBone.Children.Contains(bone))
                                parentBone.Children.Add(bone);
                        }
                    }
                }
            }
        }

        IsValid = true;
    }

    public unsafe static Skeleton? Create(GameSkeleton* gameSkeleton)
    {
        if(gameSkeleton == null)
            return null;

        return new Skeleton(gameSkeleton);
    }

    public unsafe static Skeleton? Create(BrioCharacterBase* charaBase)
    {
        if(charaBase == null)
            return null;

        if(charaBase->CharacterBase.Skeleton == null)
            return null;

        return new Skeleton(charaBase->CharacterBase.Skeleton);
    }

    public unsafe void UpdateCachedTransforms(CacheTypes cacheTypes = CacheTypes.All)
    {
        foreach(var partial in Partials)
        {
            if(partial.Poses.Count == 0)
                continue;

            hkaPose* pose = (hkaPose*)partial.Poses[0];

            if(pose == null || pose->Skeleton == null || pose->Skeleton->Bones.Data == null)
                continue;

            var boneCount = pose->Skeleton->Bones.Length;
            foreach(var (id, bone) in partial.Bones)
            {
                if(boneCount <= bone.Index)
                    continue;

                Transform pos = pose->AccessBoneModelSpace(id, PropagateOrNot.DontPropagate);

                if(cacheTypes.HasFlag(CacheTypes.LastRawTransform))
                    bone.LastRawTransform = pos;

                if(cacheTypes.HasFlag(CacheTypes.LastTransform))
                    bone.LastTransform = pos;
            }

        }
    }

    public void ClearAttachments()
    {
        AttachedTo = null;
        foreach(var bone in Bones)
        {
            bone.Attachments.Clear();
        }
    }

    public Bone? GetFirstVisibleBone(string name)
    {
        foreach(var partial in Partials)
        {
            var bone = partial.GetBone(name);
            if(bone != null && !bone.IsHidden)
                return bone;
        }
        return null;
    }

    public void Dispose()
    {
        IsValid = false;
    }
}

[Flags]
public enum CacheTypes
{
    None = 0,
    LastTransform = 1 << 0,
    LastRawTransform = 1 << 1,
    All = LastTransform | LastRawTransform,
}

public enum SkeletonType
{
    Character,
    MainHandWeapon,
    OffHandWeapon
}
