using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities;
using Brio.Game.Actor.Extensions;
using Brio.Game.Actor.Interop;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.Posing.Skeletons;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.Havok;
using System;
using System.Collections.Generic;
using System.Linq;
using static FFXIVClientStructs.Havok.hkaPose;
using GameSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

namespace Brio.Game.Posing;

internal unsafe class SkeletonService : IDisposable
{
    public delegate void SkeletonUpdateEvent();
    public event SkeletonUpdateEvent? SkeletonUpdateStart;
    public event SkeletonUpdateEvent? SkeletonUpdateEnd;

    private delegate nint UpdateBonePhysicsDelegate(nint a1);
    private readonly Hook<UpdateBonePhysicsDelegate> _updateBonePhysicsHook = null!;

    private delegate void FinalizeSkeletonsDelegate(nint a1);
    private readonly Hook<FinalizeSkeletonsDelegate> _finalizeSkeletonsHook = null!;

    private readonly EntityManager _entityManager;
    private readonly ObjectMonitorService _monitorService;
    private readonly GPoseService _gPoseService;
    private readonly IKService _ikService;
    private readonly IFramework _framework;

    private readonly List<Skeleton> _skeletons = [];
    private readonly Dictionary<Skeleton, SkeletonPosingCapability> _skeletonToPosingCapability = [];

    private readonly List<Skeleton> _skeletonsToUpdate = [];

    private const int PoseCount = 4;


    public SkeletonService(EntityManager entityManager, ObjectMonitorService monitorService, GPoseService gPoseService, IKService ikService, IFramework framework, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;
        _monitorService = monitorService;
        _gPoseService = gPoseService;
        _ikService = ikService;
        _framework = framework;


        var updateBonePhysicsAddress = "40 53 48 83 EC ?? 80 B9 ?? ?? ?? ?? ?? 48 8B D9 75 ?? 48 83 C1";
        _updateBonePhysicsHook = hooking.HookFromAddress<UpdateBonePhysicsDelegate>(scanner.ScanText(updateBonePhysicsAddress), UpdateBonePhysicsDetour);
        _updateBonePhysicsHook.Enable();

        var finalizeSkeletonsHook = "48 8B 0D 31 83 10 02 E9 14 7D 32"; // Framework.TaskRenderGraphicsRender
        _finalizeSkeletonsHook = hooking.HookFromAddress<FinalizeSkeletonsDelegate>(scanner.ScanText(finalizeSkeletonsHook), FinalizeSkeletonsHook);
        _finalizeSkeletonsHook.Enable();

        _monitorService.CharacterBaseMaterialsUpdated += OnCharacterBaseMaterialsUpdate;
        _monitorService.CharacterBaseDestroyed += OnCharacterBaseCleanup;

        RefreshSkeletonCache();
    }

    public unsafe void RegisterForFrameUpdate(Skeleton? skeleton, SkeletonPosingCapability posingCapability)
    {
        if(skeleton != null)
            _skeletonToPosingCapability[skeleton] = posingCapability;
    }

    public Skeleton? GetSkeleton(BrioCharacterBase* charaBase)
    {
        return _skeletons.FirstOrDefault(x => x!.CharacterBase == charaBase, null);
    }

    public Skeleton? GetSkeleton(GameSkeleton* skeleton)
    {
        return _skeletons.FirstOrDefault(x => x!.GameSkeleton == skeleton, null);
    }

    private void ApplyBrioTransforms(Skeleton skeleton, SkeletonPosingCapability posingCapability)
    {
        for(int partialIdx = 0; partialIdx < skeleton.Partials.Count; ++partialIdx)
        {
            var partial = skeleton.Partials[partialIdx];
            var pose = partial.GetBestPose();
            if(pose != null)
            {
                var boneLength = pose->Skeleton->Bones.Length;
                for(int boneIdx = 0; boneIdx < boneLength; ++boneIdx)
                {
                    var bone = partial.GetBone(boneIdx);

                    if(bone == null)
                        continue;

                    var bonePoseInfo = posingCapability.GetBonePose(bone);

                    // Apply existing stacks
                    var snapshotCount = bonePoseInfo.Stacks.Count;
                    foreach(var info in bonePoseInfo.Stacks)
                    {
                        ApplySnapshot(pose, bone, info);
                    }

                    var modelSpace = pose->AccessBoneModelSpace(boneIdx, PropagateOrNot.DontPropagate);
                    bone.LastTransform = modelSpace;
                    bone.LastRawTransform = modelSpace;

                    // Transitive actions
                    posingCapability.ExecuteTransitiveActions(bone, bonePoseInfo);


                    // Apply new stacks
                    for(int i = snapshotCount; i < bonePoseInfo.Stacks.Count; i++)
                    {
                        var info = bonePoseInfo.Stacks[i];
                        ApplySnapshot(pose, bone, info);
                    }
                }
            }
        }
    }

    private void ReparentPartials(Skeleton skeleton)
    {
        for(int partialIdx = 0; partialIdx < skeleton.Partials.Count; ++partialIdx)
        {
            var partial = skeleton.Partials[partialIdx];
            var pose = partial.GetBestPose();

            if(pose == null)
                continue;

            var boneLength = pose->Skeleton->Bones.Length;
            for(int boneIdx = 0; boneIdx < boneLength; ++boneIdx)
            {
                var bone = partial.GetBone(boneIdx);

                if(bone == null)
                    continue;

                if((bone.IsPartialRoot && !bone.IsSkeletonRoot))
                {
                    var parent = bone.Parent!.LastTransform;
                    var modelSpace = pose->AccessBoneModelSpace(boneIdx, PropagateOrNot.Propagate);
                    modelSpace->Translation = *(hkVector4f*)(&parent.Position);
                    modelSpace->Rotation = *(hkQuaternionf*)(&parent.Rotation);
                    modelSpace->Scale = *(hkVector4f*)(&parent.Scale);
                }
            }
        }
    }

    private unsafe void ReparentAttachments(Skeleton skeleton)
    {
        var attach = &skeleton.CharacterBase->Attach;

        // Let the game update the attachment positions
        attach->Task.Execute(null);

        // Now we can reparent them
        if(attach->AttachmentCount > 0)
        {
            var attachedPtr = attach->Parent;

            if(attachedPtr != null)
            {
                var attachedBone = attach->Attachments[0].BoneIdx;

                GameSkeleton* attachedSkeleton = attach->Type switch
                {
                    AttachType.CharacterBase => ((BrioCharacterBase*)attachedPtr)->CharacterBase.Skeleton,
                    AttachType.Skeleton => (GameSkeleton*)attachedPtr,
                    _ => null
                };

                if(attachedSkeleton != null)
                {
                    var parentSkeleton = _skeletons.FirstOrDefault(x => x!.GameSkeleton == attachedSkeleton, null);
                    if(parentSkeleton != null && parentSkeleton.Partials.Any())
                    {
                        var parentPartial = parentSkeleton.Partials[0];
                        var parentBone = parentPartial.GetBone(attachedBone);
                        if(parentBone != null)
                        {
                            skeleton.AttachedTo = parentBone;
                            parentBone.Attachments.Add(skeleton);
                        }
                    }
                }
            }
        }
    }

    private void BeginSkeletonUpdate()
    {
        // This is a very hot path, be careful how much you do here.
        // All the main skeleton stuff like positions, IK and physics is done at this point.

        if(!_gPoseService.IsGPosing)
            return;

        _skeletonsToUpdate.Clear();

        BeginPosingInterval();

        foreach(var skeleton in _skeletons)
        {
            if(skeleton.IsValid is false)
                continue;

            if(skeleton.CharacterBase is null)
                continue;

            if(_skeletonToPosingCapability.ContainsKey(skeleton) is false)
                continue;

            _skeletonsToUpdate.Add(skeleton);

        }

        foreach(var skeleton in _skeletonsToUpdate)
        {
            skeleton.ClearAttachments();
        }

        foreach(var skeleton in _skeletonsToUpdate)
        {
            ApplyBrioTransforms(skeleton, _skeletonToPosingCapability[skeleton]);
            skeleton.UpdateCachedTransforms();
            ReparentPartials(skeleton);
            skeleton.UpdateCachedTransforms();
        }

        foreach(var skeleton in _skeletonsToUpdate)
        {
            ReparentAttachments(skeleton);
        }
    }

    private void FinalizeSkeletonUpdate()
    {
        if(!_gPoseService.IsGPosing)
            return;

        foreach(var skeleton in _skeletonsToUpdate)
        {
            // We take one final view now the engine is done touching skeletons.
            // Notably, the tail size and breast size are updated during the render rather than the physics update (or before).
            // It's too late to manipulate what ends up in the game scene at this point.
            skeleton.UpdateCachedTransforms(CacheTypes.LastTransform);
        }

        EndPosingInverval();
    }

    private void ApplySnapshot(hkaPose* pose, Bone bone, BonePoseTransformInfo info)
    {
        Transform temp = default;

        var boneId = bone.Index;

        var trans = info.Transform;
        trans.Filter(info.PropagateComponents);

        // Position
        bool prop = info.PropagateComponents.HasFlag(TransformComponents.Position);
        var modelSpace = pose->AccessBoneModelSpace(boneId, prop ? PropagateOrNot.Propagate : PropagateOrNot.DontPropagate);
        temp = modelSpace;
        temp.Position += info.Transform.Position;
        if(info.IKInfo.Enabled)
        {
            _ikService.SolveIK(pose, info.IKInfo, bone, temp.Position);

            if(!info.IKInfo.EnforceConstraints)
            {
                modelSpace = pose->AccessBoneModelSpace(boneId, prop ? PropagateOrNot.Propagate : PropagateOrNot.DontPropagate);
                modelSpace->Translation = *(hkVector4f*)(&temp.Position);
            }
        }
        else
        {
            modelSpace->Translation = *(hkVector4f*)(&temp.Position);
        }

        // Rotation
        prop = info.PropagateComponents.HasFlag(TransformComponents.Rotation);
        modelSpace = pose->AccessBoneModelSpace(boneId, prop ? PropagateOrNot.Propagate : PropagateOrNot.DontPropagate);
        temp = modelSpace;
        temp.Rotation *= info.Transform.Rotation;
        modelSpace->Rotation = *(hkQuaternionf*)(&temp.Rotation);

        // Scale
        prop = info.PropagateComponents.HasFlag(TransformComponents.Scale);
        modelSpace = pose->AccessBoneModelSpace(boneId, prop ? PropagateOrNot.Propagate : PropagateOrNot.DontPropagate);
        temp = modelSpace;
        temp.Scale += info.Transform.Scale;
        modelSpace->Scale = *(hkVector4f*)(&temp.Scale);
    }

    private void RefreshSkeletonCache()
    {
        Brio.Log.Debug("Refreshing skeleton cache...");
        _skeletonToPosingCapability.Clear();
        _skeletons.Clear();
        foreach(var actor in _monitorService.ObjectTable)
        {
            if(actor is Character chara)
            {
                var bases = chara.GetCharacterBases();
                foreach(var charaBase in bases)
                {
                    CacheSkeleton(charaBase.CharacterBase);
                }
            }
        }
        Brio.Log.Debug("Skeleton cache refreshed.");
    }

    private void ClearSkeleton(Skeleton skeleton)
    {
        _skeletons.Remove(skeleton);
        _skeletonToPosingCapability.Remove(skeleton);
        skeleton.Dispose();
    }

    private void ClearSkeleton(BrioCharacterBase* charaBase)
    {

        var temp = _skeletons.FirstOrDefault(x => x!.CharacterBase == charaBase, null);
        if(temp != null)
            ClearSkeleton(temp);
    }

    private void ClearSkeleton(GameSkeleton* skeleton)
    {

        var temp = _skeletons.FirstOrDefault(x => x!.GameSkeleton == skeleton, null);
        if(temp != null)
            ClearSkeleton(temp);
    }

    private void CacheSkeleton(GameSkeleton* skeleton)
    {
        ClearSkeleton(skeleton);
        var skele = Skeleton.Create(skeleton);
        if(skele != null)
        {
            _skeletons.Add(skele);
        }
    }

    private void CacheSkeleton(BrioCharacterBase* charaBase)
    {
        ClearSkeleton(charaBase);
        CacheSkeleton(charaBase->CharacterBase.Skeleton);
    }

    private void BeginPosingInterval()
    {
        SkeletonUpdateStart?.Invoke();
    }

    private void EndPosingInverval()
    {
        _skeletonToPosingCapability.Clear();
        SkeletonUpdateEnd?.Invoke();
    }

    private void OnCharacterBaseMaterialsUpdate(BrioCharacterBase* charaBase)
    {
        try
        {
            CacheSkeleton(charaBase);
        }
        catch(Exception e)
        {
            Brio.Log.Error(e, "Error during skeleton caching");
        }
    }

    private void OnCharacterBaseCleanup(BrioCharacterBase* charaBase)
    {
        try
        {
            ClearSkeleton(charaBase);
        }
        catch(Exception e)
        {
            Brio.Log.Error(e, "Error during skeleton cleanup");
        }
    }

    private nint UpdateBonePhysicsDetour(nint a1)
    {
        var result = _updateBonePhysicsHook.Original(a1);
        try
        {
            BeginSkeletonUpdate();
        }
        catch(Exception e)
        {
            Brio.Log.Error(e, "Error during skeleton update");
        }
        return result;
    }

    private void FinalizeSkeletonsHook(nint a1)
    {
        _finalizeSkeletonsHook.Original(a1);
        try
        {
            FinalizeSkeletonUpdate();
        }
        catch(Exception e)
        {
            Brio.Log.Error(e, "Error during skeleton finalization");
        }
    }

    public void Dispose()
    {
        _updateBonePhysicsHook.Dispose();
        _finalizeSkeletonsHook.Dispose();
        _monitorService.CharacterBaseMaterialsUpdated -= OnCharacterBaseMaterialsUpdate;
        _monitorService.CharacterBaseDestroyed -= OnCharacterBaseCleanup;
    }
}

