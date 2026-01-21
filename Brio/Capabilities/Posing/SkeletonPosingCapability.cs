using Brio.Capabilities.Actor;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Posing;
using Brio.Game.Posing.Skeletons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Capabilities.Posing;

public class SkeletonPosingCapability : ActorCharacterCapability
{
    private readonly SkeletonService _skeletonService;
    private readonly PosingService _posingService;


    public Skeleton? CharacterSkeleton { get; private set; }
    public Skeleton? MainHandSkeleton { get; private set; }
    public Skeleton? OffHandSkeleton { get; private set; }
    public Skeleton? PropSkeleton { get; private set; }
    public Skeleton? OrnamentSkeleton { get; private set; }

    public bool CharacterHasTail { get; private set; }
    public bool CharacterIsIVCS { get; private set; }
    public bool CharacterIsDawntrail { get; private set; }

    public IReadOnlyList<(Skeleton Skeleton, PoseInfoSlot Slot)> Skeletons => [.. new[] { (CharacterSkeleton, PoseInfoSlot.Character), (MainHandSkeleton, PoseInfoSlot.MainHand), (OffHandSkeleton, PoseInfoSlot.OffHand), (PropSkeleton, PoseInfoSlot.Prop), (OrnamentSkeleton, PoseInfoSlot.Ornament) }.Where(s => s.Item1 != null).Cast<(Skeleton Skeleton, PoseInfoSlot Slot)>()];

    public PoseInfo PoseInfo { get; set; } = new PoseInfo();

    private readonly List<Action<Bone, BonePoseInfo>> _transitiveActions = [];


    public SkeletonPosingCapability(ActorEntity parent, SkeletonService skeletonService, PosingService posingService) : base(parent)
    {
        _skeletonService = skeletonService;
        _posingService = posingService;

        _skeletonService.SkeletonUpdateStart += OnSkeletonUpdateStart;
        _skeletonService.SkeletonUpdateEnd += OnSkeletonUpdateEnd;
    }

    public void ResetPose()
    {
        PoseInfo.Clear();
    }

    public void RegisterTransitiveAction(Action<Bone, BonePoseInfo> action)
    {
        _transitiveActions.Add(action);
    }

    public void ExecuteTransitiveActions(Bone bone, BonePoseInfo poseInfo)
    {
        _transitiveActions.ForEach(a => a(bone, poseInfo));
    }

    public void ImportSkeletonPose(PoseFile poseFile, PoseImporterOptions options, bool expressionPhase = false)
    {
        var importer = new PoseImporter(poseFile, options, expressionPhase);
        RegisterTransitiveAction(importer.ApplyBone);
    }

    public void ExportSkeletonPose(PoseFile poseFile)
    {
        var skeleton = CharacterSkeleton;
        if(skeleton != null)
        {
            foreach(var bone in CharacterSkeleton!.Bones)
            {
                if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                    continue;

                poseFile.Bones[bone.Name] = bone.LastRawTransform;
            }
        }

        var mainHandSkeleton = MainHandSkeleton;
        if(mainHandSkeleton != null)
        {
            foreach(var bone in mainHandSkeleton!.Bones)
            {
                if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                    continue;

                poseFile.MainHand[bone.Name] = bone.LastRawTransform;
            }
        }

        var offHandSkeleton = OffHandSkeleton;
        if(offHandSkeleton != null)
        {
            foreach(var bone in offHandSkeleton!.Bones)
            {
                if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                    continue;

                poseFile.OffHand[bone.Name] = bone.LastRawTransform;
            }
        }

        var propSkeleton = PropSkeleton;
        if(propSkeleton != null)
        {
            foreach(var bone in propSkeleton!.Bones)
            {
                if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                    continue;

                poseFile.Prop[bone.Name] = bone.LastRawTransform;
            }
        }

        var ornamentSkeleton = OrnamentSkeleton;
        if(ornamentSkeleton != null)
        {
            foreach(var bone in ornamentSkeleton!.Bones)
            {
                if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                    continue;

                poseFile.Ornament[bone.Name] = bone.LastRawTransform;
            }
        }
    }

    // From LivePose (Thank You Caraxi!) https://github.com/Caraxi/LivePose/blob/69afd7ba4f46611ac6055266f2524d1ac1d22454/LivePose/UI/Windows/Specialized/PosingOverlayToolbarWindow.cs#L337
    public void ResetIK()
    {
        var pose = new PoseFile();
        ExportSkeletonPose(pose);
        foreach(var p in pose.Bones.Keys)
        {
            var bBone = GetBone(p, PoseInfoSlot.Character);
            if(bBone == null) continue;
            var bonePoseInfo = GetBonePose(bBone);

            bonePoseInfo.ClearStacks();
            bonePoseInfo.DefaultIK = BoneIKInfo.CalculateDefault(p);
        }

        ResetPose();
        ImportSkeletonPose(pose, new PoseImporterOptions(new BoneFilter(_posingService), TransformComponents.All, false));
    }

    public unsafe BonePoseInfo GetBonePose(BonePoseInfoId bone)
    {
        return PoseInfo.GetPoseInfo(bone);
    }

    public unsafe BonePoseInfo GetBonePose(Bone bone)
    {
        if(CharacterSkeleton != null && CharacterSkeleton == bone.Skeleton)
        {
            return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Character);
        }

        if(MainHandSkeleton != null && MainHandSkeleton == bone.Skeleton)
        {
            return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.MainHand);
        }

        if(OffHandSkeleton != null && OffHandSkeleton == bone.Skeleton)
        {
            return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.OffHand);
        }

        if(PropSkeleton != null && PropSkeleton == bone.Skeleton)
        {
            return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Prop);
        }

        if(OrnamentSkeleton != null && OrnamentSkeleton == bone.Skeleton)
        {
            return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Ornament);
        }

        return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Unknown);
    }

    public Bone? GetBone(BonePoseInfoId? id)
    {
        if(id == null)
            return null;

        return id.Value.Slot switch
        {
            PoseInfoSlot.Character => CharacterSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
            PoseInfoSlot.MainHand => MainHandSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
            PoseInfoSlot.OffHand => OffHandSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
            PoseInfoSlot.Prop => PropSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
            PoseInfoSlot.Ornament => OrnamentSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
            _ => null,
        };
    }

    public Bone? GetBone(string name, PoseInfoSlot slot)
    {
        return slot switch
        {
            PoseInfoSlot.Character => CharacterSkeleton?.GetFirstVisibleBone(name),
            PoseInfoSlot.MainHand => MainHandSkeleton?.GetFirstVisibleBone(name),
            PoseInfoSlot.OffHand => OffHandSkeleton?.GetFirstVisibleBone(name),
            PoseInfoSlot.Prop => PropSkeleton?.GetFirstVisibleBone(name),
            PoseInfoSlot.Ornament => OrnamentSkeleton?.GetFirstVisibleBone(name),
            _ => null,
        };
    }

    private unsafe void UpdateCache()
    {
        CharacterSkeleton = _skeletonService.GetSkeleton(Character.GetCharacterBase());
        MainHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.MainHand));
        OffHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.OffHand));
        PropSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.Prop));
        OrnamentSkeleton = _skeletonService.GetSkeleton(Character.GetOrnamentBase());

        _skeletonService.RegisterForFrameUpdate(CharacterSkeleton, this);
        _skeletonService.RegisterForFrameUpdate(MainHandSkeleton, this);
        _skeletonService.RegisterForFrameUpdate(OffHandSkeleton, this);
        _skeletonService.RegisterForFrameUpdate(PropSkeleton, this);
        _skeletonService.RegisterForFrameUpdate(OrnamentSkeleton, this);

        CharacterHasTail = CharacterSkeleton?.GetFirstVisibleBone("n_sippo_a") != null;
        CharacterIsIVCS = CharacterSkeleton?.GetFirstVisibleBone("iv_ko_c_l") != null;
        CharacterIsDawntrail = CharacterSkeleton?.GetFirstVisibleBone("j_f_bero_01") != null;
    }

    public bool FilterFaceBones(BonePoseInfoId obj)
    {
        var skeleton = obj.Slot switch
        {
            PoseInfoSlot.Character => CharacterSkeleton,
            _ => null
        };

        var bone = skeleton?.GetFirstVisibleBone(obj.BoneName);
        if(bone == null) return false;

        return bone.IsFaceBone;
    }

    public bool FilterNonFaceBones(BonePoseInfoId obj) => !FilterFaceBones(obj);

    private void OnSkeletonUpdateStart()
    {
        UpdateCache();
    }

    private void OnSkeletonUpdateEnd()
    {
        _transitiveActions.Clear();
    }

    public override void Dispose()
    {
        _skeletonService.SkeletonUpdateStart -= OnSkeletonUpdateStart;
        _skeletonService.SkeletonUpdateEnd -= OnSkeletonUpdateEnd;

        _transitiveActions.Clear();

        PoseInfo.Clear();
        base.Dispose();
    }
}
