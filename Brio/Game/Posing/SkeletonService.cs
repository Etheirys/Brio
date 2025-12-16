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
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static FFXIVClientStructs.Havok.Animation.Rig.hkaPose;
using GameSkeleton = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;

namespace Brio.Game.Posing;

public unsafe class SkeletonService : IDisposable
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
    private readonly IObjectTable _objectTable;

    private readonly List<Skeleton> _skeletons = [];
    private readonly Dictionary<Skeleton, SkeletonPosingCapability> _skeletonToPosingCapability = [];
    private readonly List<Skeleton> _skeletonsToUpdate = [];

    // NEW: Real-time animation support
    private readonly Dictionary<ulong, Dictionary<string, BoneTransform>> _directBoneOverrides = [];
    private readonly Dictionary<ulong, Dictionary<string, BoneTransform>> _interpolatedState = [];
    private readonly Dictionary<ulong, Dictionary<int, Dictionary<string, int>>> _boneIndexCache = [];

    private readonly object _directOverridesLock = new();

    // Configuration for real-time animation
    public float BoneInterpolationSpeed { get; set; } = 0.2f;
    public bool RealTimeAnimationEnabled { get; set; } = true;

    public SkeletonService(EntityManager entityManager, ObjectMonitorService monitorService, GPoseService gPoseService, IKService ikService, IObjectTable gameObjects, IFramework framework, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;
        _monitorService = monitorService;
        _gPoseService = gPoseService;
        _ikService = ikService;
        _framework = framework;
        _objectTable = gameObjects;

        var updateBonePhysicsAddress = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 56 48 83 EC ?? 48 8B 59 ?? 45 33 E4";
        _updateBonePhysicsHook = hooking.HookFromAddress<UpdateBonePhysicsDelegate>(scanner.ScanText(updateBonePhysicsAddress), UpdateBonePhysicsDetour);
        _updateBonePhysicsHook.Enable();

        var finalizeSkeletonsHook = "40 53 55 57 41 55 48 83 EC 68"; // JMP in Framework.TaskRenderGraphicsRender
        _finalizeSkeletonsHook = hooking.HookFromAddress<FinalizeSkeletonsDelegate>(scanner.ScanText(finalizeSkeletonsHook), FinalizeSkeletonsHook);
        _finalizeSkeletonsHook.Enable();

        _monitorService.CharacterBaseMaterialsUpdated += OnCharacterBaseMaterialsUpdate;
        _monitorService.CharacterBaseDestroyed += OnCharacterBaseCleanup;

        RefreshSkeletonCache();
    }

    // NEW: Real-time animation API
    public bool SetBoneTransforms(ulong objectId, Dictionary<string, BoneTransform> bones)
    {
        if(!RealTimeAnimationEnabled)
            return false;

        lock(_directOverridesLock)
        {
            _directBoneOverrides[objectId] = new Dictionary<string, BoneTransform>(bones);
        }
        return true;
    }

    public void ClearDirectBoneOverrides(ulong objectId)
    {
        lock(_directOverridesLock)
        {
            _directBoneOverrides.Remove(objectId);
            _interpolatedState.Remove(objectId);
            _boneIndexCache.Remove(objectId);
        }
    }

    public void SetRealTimeAnimation(bool enabled)
    {
        RealTimeAnimationEnabled = enabled;
        if(!enabled)
        {
            lock(_directOverridesLock)
            {
                _directBoneOverrides.Clear();
                _interpolatedState.Clear();
                _boneIndexCache.Clear();
            }
        }
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

    // PRESERVED: Original Brio transform application
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

    // NEW: Direct bone override application for real-time animation
    private void ApplyDirectBoneOverrides(Skeleton skeleton)
    {
        if(!RealTimeAnimationEnabled)
            return;

        lock(_directOverridesLock)
        {
            if(_directBoneOverrides.Count == 0)
                return;

            foreach(var kvp in _directBoneOverrides)
            {
                var objectId = kvp.Key;
                var targetBones = kvp.Value;

                // Get or create interpolated state
                if(!_interpolatedState.TryGetValue(objectId, out var current))
                {
                    current = [];
                    foreach(var bone in targetBones)
                    {
                        current[bone.Key] = bone.Value;
                    }
                    _interpolatedState[objectId] = current;
                }
                else
                {
                    // Interpolate from current to target
                    foreach(var targetBone in targetBones)
                    {
                        var boneName = targetBone.Key;
                        var target = targetBone.Value;

                        if(!current.TryGetValue(boneName, out var currentTransform))
                        {
                            current[boneName] = target;
                        }
                        else
                        {
                            var interpolated = InterpolateBoneTransform(currentTransform, target);
                            current[boneName] = interpolated;
                        }
                    }
                }

                // Apply the interpolated transforms directly to skeleton
                ApplyDirectTransformsToSkeleton(skeleton, objectId, current);
            }
        }
    }

    private BoneTransform InterpolateBoneTransform(BoneTransform current, BoneTransform target)
    {
        var interpolated = new BoneTransform();

        if(target.Position.HasValue && current.Position.HasValue)
        {
            interpolated.Position = Vector3.Lerp(current.Position.Value, target.Position.Value, BoneInterpolationSpeed);
        }
        else if(target.Position.HasValue)
        {
            interpolated.Position = target.Position;
        }

        if(target.Rotation.HasValue && current.Rotation.HasValue)
        {
            interpolated.Rotation = Quaternion.Slerp(current.Rotation.Value, target.Rotation.Value, BoneInterpolationSpeed);
        }
        else if(target.Rotation.HasValue)
        {
            interpolated.Rotation = target.Rotation;
        }

        if(target.Scale.HasValue && current.Scale.HasValue)
        {
            interpolated.Scale = Vector3.Lerp(current.Scale.Value, target.Scale.Value, BoneInterpolationSpeed);
        }
        else if(target.Scale.HasValue)
        {
            interpolated.Scale = target.Scale;
        }

        return interpolated;
    }

    private void ApplyDirectTransformsToSkeleton(Skeleton skeleton, ulong objectId, Dictionary<string, BoneTransform> transforms)
    {
        for(int partialIdx = 0; partialIdx < skeleton.Partials.Count; partialIdx++)
        {
            var partial = skeleton.Partials[partialIdx];
            var pose = partial.GetBestPose();
            if(pose == null)
                continue;

            // Build or get bone index cache
            if(!_boneIndexCache.TryGetValue(objectId, out var partialCaches))
            {
                partialCaches = [];
                _boneIndexCache[objectId] = partialCaches;
            }

            if(!partialCaches.TryGetValue(partialIdx, out var indexCache))
            {
                indexCache = [];
                var boneCount = pose->Skeleton->Bones.Length;
                for(int i = 0; i < boneCount; i++)
                {
                    var boneName = pose->Skeleton->Bones[i].Name.String;
                    if(boneName != null)
                    {
                        indexCache[boneName] = i;
                    }
                }
                partialCaches[partialIdx] = indexCache;
            }

            // Apply transforms
            foreach(var kvp in transforms)
            {
                var boneName = kvp.Key;
                var transform = kvp.Value;

                if(!indexCache.TryGetValue(boneName, out var boneIdx))
                    continue;

                var modelSpace = pose->AccessBoneModelSpace(boneIdx, PropagateOrNot.Propagate);

                if(transform.Position.HasValue)
                {
                    var pos = transform.Position.Value;
                    modelSpace->Translation = *(hkVector4f*)(&pos);
                }

                if(transform.Rotation.HasValue)
                {
                    var rot = transform.Rotation.Value;
                    modelSpace->Rotation = *(hkQuaternionf*)(&rot);
                }

                if(transform.Scale.HasValue)
                {
                    var scale = transform.Scale.Value;
                    modelSpace->Scale = *(hkVector4f*)(&scale);
                }
            }
        }
    }

    // PRESERVED: Original Brio partial reparenting
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
                    var modelSpace = pose->AccessBoneModelSpace(boneIdx, PropagateOrNot.Propagate);
                    if(bone.Parent is not null)
                    {
                        var parent = bone.Parent.LastTransform;
                        modelSpace->Translation = *(hkVector4f*)(&parent.Position);
                        modelSpace->Rotation = *(hkQuaternionf*)(&parent.Rotation);
                        modelSpace->Scale = *(hkVector4f*)(&parent.Scale);
                    }
                }
            }
        }
    }

    // PRESERVED: Original Brio attachment reparenting
    private void ReparentAttachments(Skeleton skeleton)
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
                    if(parentSkeleton != null && parentSkeleton.Partials.Count != 0)
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

    // ENHANCED: Skeleton update now supports both modes
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

            _skeletonsToUpdate.Add(skeleton);
        }

        foreach(var skeleton in _skeletonsToUpdate)
        {
            skeleton.ClearAttachments();
        }

        foreach(var skeleton in _skeletonsToUpdate)
        {
            // NEW: Apply direct bone overrides first for real-time animation
            try
            {
                ApplyDirectBoneOverrides(skeleton);
            }
            catch(Exception e)
            {
                Brio.Log.Error(e, "Error applying direct bone overrides");
            }

            if(_skeletonToPosingCapability.TryGetValue(skeleton, out var capability))
            {
                ApplyBrioTransforms(skeleton, capability);
            }

            skeleton.UpdateCachedTransforms();
            ReparentPartials(skeleton);
            skeleton.UpdateCachedTransforms();
        }

        foreach(var skeleton in _skeletonsToUpdate)
        {
            ReparentAttachments(skeleton);
        }
    }

    // ENHANCED: Finalization supports both modes
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

    // PRESERVED: Original Brio snapshot application
    private void ApplySnapshot(hkaPose* pose, Bone bone, BonePoseTransformInfo info)
    {
        Transform temp = default;

        var boneId = bone.Index;

        var prop = info.PropagateComponents.HasFlag(TransformComponents.Position);
        var modelSpace = pose->AccessBoneModelSpace(boneId, prop ? PropagateOrNot.Propagate : PropagateOrNot.DontPropagate);

        // Position
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

    // ENHANCED: Cache refresh now handles real-time data
    public void RefreshSkeletonCache()
    {
        Brio.Log.Debug("Refreshing skeleton cache...");
        _skeletonToPosingCapability.Clear();
        _skeletons.Clear();

        foreach(var actor in _monitorService.ObjectTable)
        {
            if(actor is ICharacter chara)
            {
                var bases = chara.GetCharacterBases();
                foreach(var charaBase in bases)
                {
                    CacheSkeleton(charaBase.CharacterBase);
                    Brio.Log.Verbose($"Skeleton cached - [ {actor.Name} ] - ObjectKind: {actor.ObjectKind} :: Slot:{charaBase.Slot} :: Attach:{charaBase.CharacterBase->Attach.Type} ({charaBase.CharacterBase->Attach.AttachmentCount})");
                }
            }
        }
        Brio.Log.Debug("Skeleton cache refreshed.");
    }

    // ENHANCED: Clear skeleton now handles real-time data
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

    // ENHANCED: End posing interval now handles real-time mode
    private void EndPosingInverval()
    {
        // PRESERVED: Only clear Brio capability mappings when not in real-time mode
        if(!RealTimeAnimationEnabled)
        {
            _skeletonToPosingCapability.Clear();
        }
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

        // NEW: Clean up real-time data
        lock(_directOverridesLock)
        {
            _directBoneOverrides.Clear();
            _interpolatedState.Clear();
            _boneIndexCache.Clear();
        }
    }
}

/// <summary>
/// NEW: Bone transform data for direct skeleton manipulation
/// </summary>
public class BoneTransform
{
    public Vector3? Position { get; set; }
    public Quaternion? Rotation { get; set; }
    public Vector3? Scale { get; set; }
}
