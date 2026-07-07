using Brio.Capabilities.Core;
using Brio.Core;
using Brio.Entities.Camera;
using Brio.Game.Posing;
using Brio.Services.Timeline;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Capabilities.Timeline;

public class CameraTimelineCapability : Capability, ITimelineHost
{
    public List<TimelineTrack> Tracks { get; } = [];

    public string Name => CameraEntity.FriendlyName;
    public string CaptureHint => "Pose the camera, then capture.";

    public IReadOnlyList<TimelineCaptureChannel> CaptureChannels { get; }

    private CameraEntity CameraEntity => (CameraEntity)Entity;
    private readonly TimelineService _timelineService;
    private readonly TimelineTrack _track;

    private const float AutoDetectTolerance = 0.0001f;

    public CameraTimelineCapability(CameraEntity parent, TimelineService timelineService) : base(parent)
    {
        _timelineService = timelineService;

        _track = new TimelineTrack(new BonePoseInfoId("Camera", 0, PoseInfoSlot.Unknown), "Camera");

        Tracks.Add(_track);

        CaptureChannels =
        [
            new("Position", "Capture position", f => CaptureKeyframe(f, CameraComponents.Position)),
            new("Rotation", "Capture rotation", f => CaptureKeyframe(f, CameraComponents.Rotation)),
            new("Lens", "Capture zoom, FoV, angle, pan and pivot", f => CaptureKeyframe(f, CameraComponents.Lens)),
            new("All", "Capture position, rotation and lens", f => CaptureKeyframe(f, CameraComponents.All)),
            new("Auto", "Capture only the components that changed", CaptureAuto)
        ];

        _timelineService.Register(this);
    }

    public void CaptureKeyframe(int frame, CameraComponents groups)
    {
        var camera = CameraEntity.VirtualCamera;
        var keyframe = new CameraKeyframe
        {
            Position = camera.Position,
            Rotation = camera.Rotation,
            Angle = camera.Angle,
            Pan = camera.Pan,
            Zoom = camera.Zoom,
            FoV = camera.FoV,
            PivotRotation = camera.PivotRotation
        };

        _track.AddCameraKeyframe(frame, keyframe, groups, _timelineService.NewKeyframeMode);
    }
    public void CaptureAuto(int frame)
    {
        var camera = CameraEntity.VirtualCamera;

        CameraComponents groups;
        if(_track.Keyframes.Count == 0)
        {
            groups = CameraComponents.All;
        }
        else
        {
            var (sampled, _) = SampleCamera(frame);
            groups = CameraComponents.None;

            if(!camera.Position.IsApproximatelySame(sampled.Position))
                groups |= CameraComponents.Position;
            if(!camera.Rotation.IsApproximatelySame(sampled.Rotation))
                groups |= CameraComponents.Rotation;
            if(Vector2.Distance(camera.Angle, sampled.Angle) > AutoDetectTolerance
                || Vector2.Distance(camera.Pan, sampled.Pan) > AutoDetectTolerance
                || Math.Abs(camera.Zoom - sampled.Zoom) > AutoDetectTolerance
                || Math.Abs(camera.FoV - sampled.FoV) > AutoDetectTolerance
                || Math.Abs(camera.PivotRotation - sampled.PivotRotation) > AutoDetectTolerance)
                groups |= CameraComponents.Lens;
        }

        if(groups == CameraComponents.None)
            return;

        CaptureKeyframe(frame, groups);
    }

    public void Apply(float frame)
    {
        if(_track.IsMuted)
            return;

        var (value, defined) = SampleCamera(frame);
        if(defined == CameraComponents.None)
            return;

        var camera = CameraEntity.VirtualCamera;

        if(defined.HasFlag(CameraComponents.Position))
            camera.Position = value.Position;
        if(defined.HasFlag(CameraComponents.Rotation))
            camera.Rotation = value.Rotation;
        if(defined.HasFlag(CameraComponents.Lens))
        {
            camera.Angle = value.Angle;
            camera.Pan = value.Pan;
            camera.Zoom = value.Zoom;
            camera.FoV = value.FoV;
            camera.PivotRotation = value.PivotRotation;
        }
    }

    public void RemoveTrack(TimelineTrack track) => track.Keyframes.Clear();

    private (CameraKeyframe Value, CameraComponents Defined) SampleCamera(float frame)
    {
        var camera = CameraEntity.VirtualCamera;
        var result = new CameraKeyframe
        {
            Position = camera.Position,
            Rotation = camera.Rotation,
            Angle = camera.Angle,
            Pan = camera.Pan,
            Zoom = camera.Zoom,
            FoV = camera.FoV,
            PivotRotation = camera.PivotRotation
        };

        var defined = CameraComponents.None;

        if(TrySampleGroup(frame, CameraComponents.Position, out var pos))
        {
            result.Position = pos.Position;
            defined |= CameraComponents.Position;
        }
        if(TrySampleGroup(frame, CameraComponents.Rotation, out var rot))
        {
            result.Rotation = rot.Rotation;
            defined |= CameraComponents.Rotation;
        }
        if(TrySampleGroup(frame, CameraComponents.Lens, out var lens))
        {
            result.Angle = lens.Angle;
            result.Pan = lens.Pan;
            result.Zoom = lens.Zoom;
            result.FoV = lens.FoV;
            result.PivotRotation = lens.PivotRotation;
            defined |= CameraComponents.Lens;
        }

        return (result, defined);
    }

    private bool TrySampleGroup(float frame, CameraComponents group, out CameraKeyframe value)
    {
        value = default;

        TrackKeyframe? previous = null;
        TrackKeyframe? next = null;

        foreach(var keyframe in _track.Keyframes)
        {
            if(!keyframe.Camera.HasValue || !keyframe.CameraComponents.HasFlag(group))
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

            value = previous.Camera!.Value;
            return true;
        }

        if(previous is null || next.Frame == frame)
        {
            value = next.Camera!.Value;
            return true;
        }

        if(next.InterpolationMode == InterpolationMode.Step)
        {
            value = previous.Camera!.Value;
            return true;
        }

        var span = next.Frame - previous.Frame;
        var t = span == 0 ? 0f : (frame - previous.Frame) / span;

        value = CameraKeyframe.Lerp(previous.Camera!.Value, next.Camera!.Value, next.EvaluateEasing(t));
        return true;
    }

    public override void Dispose()
    {
        _timelineService.Unregister(this);
        base.Dispose();
    }

    public static CameraTimelineCapability? CreateIfEligible(IServiceProvider provider, CameraEntity entity)
    {
        if(entity.CameraType == CameraType.Free)
            return ActivatorUtilities.CreateInstance<CameraTimelineCapability>(provider, entity);

        return null;
    }
}
