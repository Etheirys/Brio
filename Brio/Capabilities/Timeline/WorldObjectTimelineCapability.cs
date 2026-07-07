using Brio.Capabilities.WorldObjects;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.Posing;
using Brio.Game.WorldObjects;
using Brio.Game.WorldObjects.Objects;
using Brio.Services.Timeline;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Capabilities.Timeline;

public class WorldObjectTimelineCapability : WorldObjectCapability, ITimelineHost
{
    public List<TimelineTrack> Tracks { get; } = [];
    public IReadOnlyList<TimelineCaptureChannel> CaptureChannels { get; }

    public string Name => BgObjectEntity.FriendlyName;
    public string CaptureHint => "Pose the object, then capture.";

    private readonly TimelineService _timelineService;
    private readonly TimelineTrack _track;

    private const float AutoDetectTolerance = 0.001f;

    public WorldObjectTimelineCapability(Entity parent, TimelineService timelineService) : base(parent)
    {
        _timelineService = timelineService;

        _track = new TimelineTrack(new BonePoseInfoId("WorldObject", 0, PoseInfoSlot.Unknown), "World Object");
        Tracks.Add(_track);

        // Right now this doesn't work right, the Color doesn't want to update
        if(GameBgObject.ObjectType == WorldObjectType.StaticVfx)
        {
            CaptureChannels =
            [
                new("Transform", "Capture position, rotation and scale", f => CaptureKeyframe(f, WorldObjectComponents.Transform)),
                new("Color", "Capture VFX color", f => CaptureKeyframe(f, WorldObjectComponents.Color)),
                new("All", "Capture transform and color", f => CaptureKeyframe(f, WorldObjectComponents.All)),
                new("Auto", "Capture only the components that changed", CaptureAuto)
            ];
        }
        else
        {
            CaptureChannels =
            [
                new("Transform", "Capture position, rotation and scale", f => CaptureKeyframe(f, WorldObjectComponents.Transform)),
                new("Auto", "Capture only the components that changed", CaptureAuto)
            ];
        }

        _timelineService.Register(this);
    }

    public void CaptureKeyframe(int frame, WorldObjectComponents groups)
    {
        _track.AddWorldObjectKeyframe(frame, CaptureCurrent(), groups, _timelineService.NewKeyframeMode);
    }

    public void CaptureAuto(int frame)
    {
        var current = CaptureCurrent();

        WorldObjectComponents groups;
        if(_track.Keyframes.Count == 0)
        {
            groups = WorldObjectComponents.All;
        }
        else
        {
            var (sampled, _) = SampleWorldObject(frame);
            groups = WorldObjectComponents.None;

            if(!current.Position.IsApproximatelySame(sampled.Position)
                || !current.Rotation.IsApproximatelySame(sampled.Rotation)
                || !current.Scale.IsApproximatelySame(sampled.Scale))
                groups |= WorldObjectComponents.Transform;

            if(GameBgObject.ObjectType == WorldObjectType.StaticVfx && Vector4.Distance(current.Color, sampled.Color) > AutoDetectTolerance)
                groups |= WorldObjectComponents.Color;
        }

        if(groups == WorldObjectComponents.None)
            return;

        CaptureKeyframe(frame, groups);
    }
    public unsafe void Apply(float frame)
    {
        if(_track.IsMuted)
            return;

        var (value, defined) = SampleWorldObject(frame);
        if(defined == WorldObjectComponents.None)
            return;

        if(defined.HasFlag(WorldObjectComponents.Transform))
        {
            var transform = Entity.GetCapability<WorldObjectTransformCapability>();
            transform.Transform = new Transform { Position = value.Position, Rotation = value.Rotation, Scale = value.Scale };
        }


        if(defined.HasFlag(WorldObjectComponents.Color) && GameBgObject is StaticVfxObject vfx)
            vfx.VFX->Color = value.Color;
    }

    public void RemoveTrack(TimelineTrack track)
        => track.Keyframes.Clear();

    private unsafe WorldObjectKeyframe CaptureCurrent()
    {
        var transform = Entity.GetCapability<WorldObjectTransformCapability>().Transform;

        return new WorldObjectKeyframe
        {
            Position = transform.Position,
            Rotation = transform.Rotation,
            Scale = transform.Scale,
            Color = GameBgObject is StaticVfxObject vfx ? vfx.VFX->Color : Vector4.Zero
        };
    }

    private (WorldObjectKeyframe Value, WorldObjectComponents Defined) SampleWorldObject(float frame)
    {
        var result = CaptureCurrent();
        var defined = WorldObjectComponents.None;

        if(TrySampleGroup(frame, WorldObjectComponents.Transform, out var transform))
        {
            result.Position = transform.Position;
            result.Rotation = transform.Rotation;
            result.Scale = transform.Scale;
            defined |= WorldObjectComponents.Transform;
        }
        if(TrySampleGroup(frame, WorldObjectComponents.Color, out var color))
        {
            result.Color = color.Color;
            defined |= WorldObjectComponents.Color;
        }

        return (result, defined);
    }


    private bool TrySampleGroup(float frame, WorldObjectComponents group, out WorldObjectKeyframe value)
    {
        value = default;

        TrackKeyframe? previous = null;
        TrackKeyframe? next = null;

        foreach(var keyframe in _track.Keyframes)
        {
            if(!keyframe.WorldObject.HasValue || !keyframe.WorldObjectComponents.HasFlag(group))
                continue;

            if(keyframe.Frame >= frame)
            {
                next = keyframe;
                break;
            }

            previous = keyframe;
        }

        if(next is null)
        {
            if(previous is null)
                return false;

            value = previous.WorldObject!.Value;
            return true;
        }

        if(previous is null || next.Frame == frame)
        {
            value = next.WorldObject!.Value;
            return true;
        }

        if(next.InterpolationMode == InterpolationMode.Step)
        {
            value = previous.WorldObject!.Value;
            return true;
        }

        var span = next.Frame - previous.Frame;
        var t = span == 0 ? 0f : (frame - previous.Frame) / span;

        value = WorldObjectKeyframe.Lerp(previous.WorldObject!.Value, next.WorldObject!.Value, next.EvaluateEasing(t));
        return true;
    }

    public override void Dispose()
    {
        _timelineService.Unregister(this);
        base.Dispose();
    }
}
