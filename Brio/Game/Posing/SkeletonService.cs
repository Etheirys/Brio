using Brio.Capabilities.Posing;
using Brio.Entities;
using Brio.Game.Actor.Extensions;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.Havok;
using System;
using System.Collections.Generic;
using static FFXIVClientStructs.Havok.hkaPose;
using Brio.Core;
using Brio.Game.Posing.Skeletons;
using Brio.Game.Actor.Interop;
using Brio.Game.Core;
using System.Linq;
using GameSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;
using Brio.Game.GPose;

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
    private readonly IFramework _framework;

    private readonly List<Skeleton> _skeletons = [];
    private readonly Dictionary<Skeleton, SkeletonPosingCapability> _skeletonToPosingCapability = [];

    private readonly List<Skeleton> _skeletonsToUpdate = [];

    private const int PoseCount = 4;


    public SkeletonService(EntityManager entityManager, ObjectMonitorService monitorService, GPoseService gPoseService, IFramework framework, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;
        _monitorService = monitorService;
        _gPoseService = gPoseService;
        _framework = framework;


        var updateBonePhysicsAddress = "40 53 48 83 EC ?? 80 B9 ?? ?? ?? ?? ?? 48 8B D9 75 ?? 48 83 C1";
        _updateBonePhysicsHook = hooking.HookFromAddress<UpdateBonePhysicsDelegate>(scanner.ScanText(updateBonePhysicsAddress), UpdateBonePhysicsDetour);
        _updateBonePhysicsHook.Enable();

        // This happens early in the render, and it's the step which locks the skeletons (once tail size etc is known).
        // We could also do it after Framework.TaskRenderGraphicsRender but that's tricky to hook.
        var finalizeSkeletonsHook = "48 85 C9 74 ?? 53 48 83 EC ?? 48 8D 05 ?? ?? ?? ?? 48 89 74 24";
        _finalizeSkeletonsHook = hooking.HookFromAddress<FinalizeSkeletonsDelegate>(scanner.ScanText(finalizeSkeletonsHook), FinalizeSkeletonsHook);
        _finalizeSkeletonsHook.Enable();

        _monitorService.CharacterBaseMaterialsUpdated += OnCharacterBaseMaterialsUpdate;
        _monitorService.CharacterBaseDestroyed += OnCharacterBaseCleanup;

        RefreshSkeletonCache();
    }

    public unsafe void RegisterForFrameUpdate(Skeleton? skeleton, SkeletonPosingCapability posingCapability)
    {
        if (skeleton != null)
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
        for (int partialIdx = 0; partialIdx < skeleton.Partials.Count; ++partialIdx)
        {
            var partial = skeleton.Partials[partialIdx];
            var pose = partial.GetBestPose();
            if (pose != null)
            {
                var boneLength = pose->Skeleton->Bones.Length;
                for (int boneIdx = 0; boneIdx < boneLength; ++boneIdx)
                {
                    var bone = partial.GetBone(boneIdx);

                    if (bone == null)
                        continue;

                    var bonePoseInfo = posingCapability.GetBonePose(bone);

                    // Apply existing stacks
                    var snapshotCount = bonePoseInfo.Stacks.Count;
                    foreach (var info in bonePoseInfo.Stacks)
                    {
                        ApplySnapshot(pose, boneIdx, info);
                    }

                    var modelSpace = pose->AccessBoneModelSpace(boneIdx, PropagateOrNot.DontPropagate);
                    bone.LastTransform = modelSpace;

                    // Transitive actions
                    posingCapability.ExecuteTransitiveActions(bone, bonePoseInfo);


                    // Apply new stacks
                    for (int i = snapshotCount; i < bonePoseInfo.Stacks.Count; i++)
                    {
                        var info = bonePoseInfo.Stacks[i];
                        ApplySnapshot(pose, boneIdx, info);
                    }


                }
            }
        }
    }

    private void ReparentPartials(Skeleton skeleton)
    {
        for (int partialIdx = 0; partialIdx < skeleton.Partials.Count; ++partialIdx)
        {
            var partial = skeleton.Partials[partialIdx];
            var pose = partial.GetBestPose();

            if (pose == null)
                continue;

            var boneLength = pose->Skeleton->Bones.Length;
            for (int boneIdx = 0; boneIdx < boneLength; ++boneIdx)
            {
                var bone = partial.GetBone(boneIdx);

                if (bone == null)
                    continue;

                if ((bone.IsPartialRoot && !bone.IsSkeletonRoot))
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
        if (attach->AttachmentCount > 0)
        {
            var attachedPtr = attach->Parent;

            if (attachedPtr != null)
            {
                var attachedBone = attach->Attachments[0].BoneIdx;

                GameSkeleton* attachedSkeleton = attach->Type switch
                {
                    AttachType.CharacterBase => ((BrioCharacterBase*)attachedPtr)->CharacterBase.Skeleton,
                    AttachType.Skeleton => (GameSkeleton*)attachedPtr,
                    _ => null
                };

                if (attachedSkeleton != null)
                {
                    var parentSkeleton = _skeletons.FirstOrDefault(x => x!.GameSkeleton == attachedSkeleton, null);
                    if (parentSkeleton != null && parentSkeleton.Partials.Any())
                    {
                        var parentPartial = parentSkeleton.Partials[0];
                        var parentBone = parentPartial.GetBone(attachedBone);
                        if (parentBone != null)
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

        if (!_gPoseService.IsGPosing)
            return;

        _skeletonsToUpdate.Clear();

        BeginPosingInterval();

        foreach (var skeleton in _skeletons)
        {
            if (!skeleton.IsValid)
                continue;

            if (skeleton.CharacterBase == null)
                continue;

            if (!_skeletonToPosingCapability.ContainsKey(skeleton))
                continue;

            _skeletonsToUpdate.Add(skeleton);

        }

        foreach (var skeleton in _skeletonsToUpdate)
        {
            skeleton.ClearAttachments();
        }

        foreach (var skeleton in _skeletonsToUpdate)
        {
            ApplyBrioTransforms(skeleton, _skeletonToPosingCapability[skeleton]);
            skeleton.UpdateCachedTransforms();
            ReparentPartials(skeleton);
        }

        foreach (var skeleton in _skeletonsToUpdate)
        {
            ReparentAttachments(skeleton);
        }
    }

    private void FinalizeSkeletonUpdate()
    {
        if (!_gPoseService.IsGPosing)
            return;

        foreach (var skeleton in _skeletonsToUpdate)
        {
            // We take one final view now the engine is done touching skeletons.
            // Notably, the tail size is updated during the render rather than the physics update (or before).
            // It's too late to manipulate what ends up in the game scene at this point.
            skeleton.UpdateCachedTransforms();
        }

        EndPosingInverval();
    }

    private void ApplySnapshot(hkaPose* pose, int boneId, BonePoseTransformInfo info)
    {
        Transform temp = default;

        var trans = info.Transform;
        trans.Filter(info.PropagateComponents);

        // Position
        bool prop = info.PropagateComponents.HasFlag(TransformComponents.Position);
        var modelSpace = pose->AccessBoneModelSpace(boneId, prop ? PropagateOrNot.Propagate : PropagateOrNot.DontPropagate);
        temp = modelSpace;
        temp.Position += info.Transform.Position;
        modelSpace->Translation = *(hkVector4f*)(&temp.Position);

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
        foreach (var actor in _monitorService.ObjectTable)
        {
            if (actor is Character chara)
            {
                var bases = chara.GetCharacterBases();
                foreach (var charaBase in bases)
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
        if (temp != null)
            ClearSkeleton(temp);
    }

    private void ClearSkeleton(GameSkeleton* skeleton)
    {

        var temp = _skeletons.FirstOrDefault(x => x!.GameSkeleton == skeleton, null);
        if (temp != null)
            ClearSkeleton(temp);
    }

    private void CacheSkeleton(GameSkeleton* skeleton)
    {
        ClearSkeleton(skeleton);
        var skele = Skeleton.Create(skeleton);
        if (skele != null)
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
        catch (Exception e)
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
        catch (Exception e)
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
        catch (Exception e)
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
        catch (Exception e)
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

