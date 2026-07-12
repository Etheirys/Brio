using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Posing;
using Brio.Services.Timeline;
using Dalamud.Game.ClientState.Objects.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Capabilities.Timeline;

public class ActorTimelineCapability : ActorCharacterCapability, ITimelineHost
{
    public static readonly BonePoseInfoId ModelTransformTrackId = new("ModelTransform", 0, PoseInfoSlot.Unknown);

    private readonly PosingService _posingService;
    private readonly TimelineService _timelineService;
    private readonly PoseImporterOptions _importerOptions;
    private readonly Dictionary<BonePoseInfoId, Transform> _defaultPose = [];

    public List<TimelineTrack> Tracks { get; private set; } = [];

    public string Name => Actor.FriendlyName;
    public string CaptureHint => "Select bones in the posing overlay, then capture.";

    public EntityId OwnerId => Actor.Id;

    public IReadOnlyList<TimelineCaptureChannel> CaptureChannels { get; }

    private SkeletonPosingCapability Skeleton => Actor.GetCapability<SkeletonPosingCapability>();

    public ActorTimelineCapability(ActorEntity parent, PosingService posingService, TimelineService timelineService) : base(parent)
    {
        _posingService = posingService;
        _timelineService = timelineService;
        _importerOptions = new PoseImporterOptions(new BoneFilter(_posingService), TransformComponents.All, false);

        CaptureChannels =
        [
            new("Position", "Capture position", f => CaptureKeyframe(f, TransformComponents.Position)),
            new("Rotation", "Capture rotation", f => CaptureKeyframe(f, TransformComponents.Rotation)),
            new("Scale", "Capture scale", f => CaptureKeyframe(f, TransformComponents.Scale)),
            new("All", "Capture position, rotation and scale", f => CaptureKeyframe(f, TransformComponents.All)),
            new("Auto", "Capture only the components that changed", CaptureAuto),
            new("Capture Pose", "Capture every bone in the current pose", CapturePose),
            new("Model Transform", "Capture the actor's position, rotation and scale", CaptureModelTransform)
        ];

        _timelineService.Register(this);
    }


    public void CaptureAuto(int frame)
    {
        if(!Actor.TryGetCapability<PosingCapability>(out var posing))
            return;

        var skeleton = Skeleton;
        var captured = false;

        foreach(var boneId in posing.SelectedBones)
        {
            var bone = skeleton.GetBone(boneId);
            if(bone is null)
                continue;

            var track = GetOrCreateTrack(boneId, bone.FriendlyName);
            _defaultPose.TryAdd(boneId, bone.LastRawTransform);

            var components = DetectChangedComponents(track, frame, bone.LastRawTransform);
            if(components == TransformComponents.None)
                continue;

            track.AddPoseKeyframe(frame, bone.LastRawTransform, components, _timelineService.NewKeyframeMode);
            captured = true;
        }

        if(captured)
            RebuildHierarchy();
    }
    public void CaptureKeyframe(int frame, TransformComponents components)
    {
        if(!Actor.TryGetCapability<PosingCapability>(out var posing))
            return;

        var skeleton = Skeleton;
        var captured = false;

        foreach(var boneId in posing.SelectedBones)
        {
            var bone = skeleton.GetBone(boneId);
            if(bone is null)
                continue;

            var track = GetOrCreateTrack(boneId, bone.FriendlyName);
            _defaultPose.TryAdd(boneId, bone.LastRawTransform);
            track.AddPoseKeyframe(frame, bone.LastRawTransform, components, _timelineService.NewKeyframeMode);
            captured = true;
        }

        if(captured)
            RebuildHierarchy();
    }
    public void CapturePose(int frame)
    {
        var skeleton = Skeleton;
        var captured = false;

        foreach(var (skel, slot) in skeleton.Skeletons)
        {
            foreach(var bone in skel.Bones)
            {
                if(bone.IsPartialRoot && !bone.IsSkeletonRoot)
                    continue;

                var boneId = new BonePoseInfoId(bone.Name, bone.PartialId, slot);
                var track = GetOrCreateTrack(boneId, bone.FriendlyName);
                _defaultPose.TryAdd(boneId, bone.LastRawTransform);
                track.AddPoseKeyframe(frame, bone.LastRawTransform, TransformComponents.All, _timelineService.NewKeyframeMode);
                captured = true;
            }
        }

        if(captured)
            RebuildHierarchy();
    }
    public void CaptureModelTransform(int frame)
    {
        if(!Actor.TryGetCapability<ModelPosingCapability>(out var modelPosing))
            return;

        var track = GetOrCreateTrack(ModelTransformTrackId, "Model Transform");
        _defaultPose.TryAdd(ModelTransformTrackId, modelPosing.Transform);
        track.AddPoseKeyframe(frame, modelPosing.Transform, TransformComponents.All, _timelineService.NewKeyframeMode);
        RebuildHierarchy();
    }

    private TransformComponents DetectChangedComponents(TimelineTrack track, int frame, Transform current)
    {
        if(track.Keyframes.Count == 0)
            return TransformComponents.All;

        Transform? defaultTransform = _defaultPose.TryGetValue(track.BoneId, out var def) ? def : (Transform?)null;
        var sampled = track.Sample(frame, defaultTransform) ?? current;

        var components = TransformComponents.None;
        if(!current.Position.IsApproximatelySame(sampled.Position))
            components |= TransformComponents.Position;
        if(!current.Rotation.IsApproximatelySame(sampled.Rotation))
            components |= TransformComponents.Rotation;
        if(!current.Scale.IsApproximatelySame(sampled.Scale))
            components |= TransformComponents.Scale;

        return components;
    }

    public void Apply(float frame)
    {
        if(Tracks.Count == 0)
            return;

        var skeleton = Skeleton;
        var groups = new Dictionary<TransformComponents, PoseData>();

        foreach(var track in Tracks)
        {
            if(track.BoneId.Equals(ModelTransformTrackId))
            {
                if(Actor.TryGetCapability<ModelPosingCapability>(out var modelPosing))
                {
                    Transform? defaultModelTransform = _defaultPose.TryGetValue(ModelTransformTrackId, out var defModel) ? defModel : (Transform?)null;
                    var sampledModelTransform = track.Sample(frame, defaultModelTransform);
                    if(sampledModelTransform is not null)
                        modelPosing.Transform = sampledModelTransform.Value;
                }
                continue;
            }

            var mask = GetKeyedComponents(track);
            if(mask == TransformComponents.None)
                continue;

            Transform? defaultTransform = _defaultPose.TryGetValue(track.BoneId, out var def) ? def : (Transform?)null;
            var sampled = track.Sample(frame, defaultTransform);
            if(sampled is null)
                continue;

            if(!groups.TryGetValue(mask, out var pose))
                groups[mask] = pose = new PoseData();

            SlotDictionary(pose, track.BoneId.Slot)[track.Name] = sampled.Value;
        }

        if(groups.Count == 0)
            return;

        skeleton.ResetPose();
        foreach(var (mask, pose) in groups)
            skeleton.ImportSkeletonPose(pose, new PoseImporterOptions(_importerOptions.BoneFilter, mask, false));
    }

    private static TransformComponents GetKeyedComponents(TimelineTrack track)
    {
        var mask = TransformComponents.None;
        foreach(var keyframe in track.Keyframes)
            mask |= keyframe.Components;
        return mask;
    }

    public void RemoveTrack(TimelineTrack track)
    {
        Tracks.Remove(track);
        RebuildHierarchy();
    }

    private TimelineTrack GetOrCreateTrack(BonePoseInfoId boneId, string displayName)
    {
        var track = Tracks.FirstOrDefault(t => t.BoneId.Equals(boneId));
        if(track is null)
        {
            track = new TimelineTrack(boneId, displayName);
            Tracks.Add(track);
        }
        return track;
    }

    private void RebuildHierarchy()
    {
        var skeleton = Skeleton;

        var byId = new Dictionary<BonePoseInfoId, TimelineTrack>();
        foreach(var track in Tracks)
            byId[track.BoneId] = track;

        var childrenMap = new Dictionary<BonePoseInfoId, List<TimelineTrack>>();
        var rootTracks = new List<TimelineTrack>();

        foreach(var track in Tracks)
        {
            TimelineTrack? parentTrack = null;
            var current = skeleton.GetBone(track.BoneId)?.Parent;
            while(current is not null)
            {
                var currentId = new BonePoseInfoId(current.Name, current.PartialId, track.BoneId.Slot);
                if(byId.TryGetValue(currentId, out var candidate) && !candidate.BoneId.Equals(track.BoneId))
                {
                    parentTrack = candidate;
                    break;
                }
                current = current.Parent;
            }

            track.ParentName = parentTrack?.Name ?? string.Empty;
            if(parentTrack is null)
            {
                rootTracks.Add(track);
            }
            else
            {
                if(!childrenMap.TryGetValue(parentTrack.BoneId, out var list))
                    childrenMap[parentTrack.BoneId] = list = [];
                list.Add(track);
            }
        }

        foreach(var track in Tracks)
            track.HasChildren = childrenMap.ContainsKey(track.BoneId);

        var sorted = new List<TimelineTrack>();
        var visited = new HashSet<BonePoseInfoId>();

        void AddNode(TimelineTrack node, int depth)
        {
            if(!visited.Add(node.BoneId))
                return;

            node.Depth = depth;
            sorted.Add(node);
            if(childrenMap.TryGetValue(node.BoneId, out var children))
                foreach(var child in children.OrderBy(c => c.Name))
                    AddNode(child, depth + 1);
        }

        foreach(var root in rootTracks.OrderBy(t => t.Name))
            AddNode(root, 0);

        Tracks = sorted;
    }

    private static Dictionary<string, PoseData.Bone> SlotDictionary(PoseData pose, PoseInfoSlot slot) => slot switch
    {
        PoseInfoSlot.MainHand => pose.MainHand,
        PoseInfoSlot.OffHand => pose.OffHand,
        PoseInfoSlot.Prop => pose.Prop,
        PoseInfoSlot.Ornament => pose.Ornament,
        _ => pose.Bones
    };

    public override void Dispose()
    {
        _timelineService.Unregister(this);

        if(Actor.TryGetCapability<SkeletonPosingCapability>(out var skeleton))
            skeleton.ResetPose();

        Tracks.Clear();
        _defaultPose.Clear();

        base.Dispose();
    }

    public static ActorTimelineCapability? CreateIfEligible(IServiceProvider provider, ActorEntity entity)
    {
        if(entity.GameObject is ICharacter)
            return ActivatorUtilities.CreateInstance<ActorTimelineCapability>(provider, entity);

        return null;
    }
}
