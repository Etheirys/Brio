using Brio.Capabilities.World;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Game.Posing;
using Brio.Services.Timeline;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Capabilities.Timeline;

public unsafe class LightTimelineCapability : LightCapability, ITimelineHost
{
    public List<TimelineTrack> Tracks { get; } = [];

    public string Name => Light.FriendlyName;
    public string CaptureHint => "Pose the light, then capture.";
    public EntityId OwnerId => Entity.Id;
    public IReadOnlyList<TimelineCaptureChannel> CaptureChannels { get; }

    private readonly TimelineService _timelineService;
    private readonly TimelineTrack _track;

    private const float AutoDetectTolerance = 0.0001f;

    public LightTimelineCapability(Entity parent, TimelineService timelineService) : base(parent)
    {
        _timelineService = timelineService;

        _track = new TimelineTrack(new BonePoseInfoId("Light", 0, PoseInfoSlot.Unknown), "Light");
        Tracks.Add(_track);

        CaptureChannels =
        [
            new("Position", "Capture position", f => CaptureKeyframe(f, LightComponents.Position)),
            new("Rendering", "Capture color, intensity, range and falloff", f => CaptureKeyframe(f, LightComponents.Rendering)),
            new("All", "Capture position and rendering", f => CaptureKeyframe(f, LightComponents.All)),
            new("Auto", "Capture only the components that changed", CaptureAuto)
        ];

        _timelineService.Register(this);
    }


    public void CaptureAuto(int frame)
    {
        var current = CaptureCurrent();

        LightComponents groups;
        if(_track.Keyframes.Count == 0)
        {
            groups = LightComponents.All;
        }
        else
        {
            var (sampled, _) = SampleLight(frame);
            groups = LightComponents.None;

            if(!current.Position.IsApproximatelySame(sampled.Position))
            {
                groups |= LightComponents.Position;
            }
            if(Vector3.Distance(current.Color, sampled.Color) > AutoDetectTolerance
                || Math.Abs(current.Intensity - sampled.Intensity) > AutoDetectTolerance
                || Math.Abs(current.Range - sampled.Range) > AutoDetectTolerance
                || Math.Abs(current.FalloffFactor - sampled.FalloffFactor) > AutoDetectTolerance
                || Math.Abs(current.SpotLightAngleDegrees - sampled.SpotLightAngleDegrees) > AutoDetectTolerance
                || Math.Abs(current.AngularFalloffDegrees - sampled.AngularFalloffDegrees) > AutoDetectTolerance)
                groups |= LightComponents.Rendering;
        }

        if(groups == LightComponents.None)
            return;

        CaptureKeyframe(frame, groups);
    }
    public void AutoCapture(int frame) => CaptureAuto(frame);

    public void Apply(float frame)
    {
        if(_track.IsMuted)
            return;

        if(!GameLight.IsValid)
            return;

        var (value, defined) = SampleLight(frame);
        if(defined == LightComponents.None)
            return;

        if(defined.HasFlag(LightComponents.Position))
        {
            var transform = Entity.GetCapability<LightTransformCapability>();
            transform.Transform = new Transform { Position = value.Position, Rotation = value.Rotation, Scale = value.Scale };
        }

        if(defined.HasFlag(LightComponents.Rendering))
        {
            var renderLight = GameLight.GameLight->RenderLight;
            renderLight->Color = value.Color;
            renderLight->Intensity = value.Intensity;
            renderLight->Range = value.Range;
            renderLight->FalloffFactor = value.FalloffFactor;
            renderLight->SpotLightAngleDegrees = value.SpotLightAngleDegrees;
            renderLight->AngularFalloffDegrees = value.AngularFalloffDegrees;
        }
    }

    public void RemoveTrack(TimelineTrack track)
        => track.Keyframes.Clear();
    public void CaptureKeyframe(int frame, LightComponents groups)
        => _track.AddLightKeyframe(frame, CaptureCurrent(), groups, _timelineService.NewKeyframeMode);

    private LightKeyframe CaptureCurrent()
    {
        var renderLight = GameLight.GameLight->RenderLight;
        var transform = Entity.GetCapability<LightTransformCapability>().Transform;
        return new LightKeyframe
        {
            Position = transform.Position,
            Rotation = transform.Rotation,
            Scale = transform.Scale,
            Color = renderLight->Color,
            Intensity = renderLight->Intensity,
            Range = renderLight->Range,
            FalloffFactor = renderLight->FalloffFactor,
            SpotLightAngleDegrees = renderLight->SpotLightAngleDegrees,
            AngularFalloffDegrees = renderLight->AngularFalloffDegrees
        };
    }

    private (LightKeyframe Value, LightComponents Defined) SampleLight(float frame)
    {
        var result = CaptureCurrent();
        var defined = LightComponents.None;

        if(TrySampleGroup(frame, LightComponents.Position, out var pos))
        {
            result.Position = pos.Position;
            result.Rotation = pos.Rotation;
            result.Scale = pos.Scale;
            defined |= LightComponents.Position;
        }
        if(TrySampleGroup(frame, LightComponents.Rendering, out var rendering))
        {
            result.Color = rendering.Color;
            result.Intensity = rendering.Intensity;
            result.Range = rendering.Range;
            result.FalloffFactor = rendering.FalloffFactor;
            result.SpotLightAngleDegrees = rendering.SpotLightAngleDegrees;
            result.AngularFalloffDegrees = rendering.AngularFalloffDegrees;
            defined |= LightComponents.Rendering;
        }

        return (result, defined);
    }

    private bool TrySampleGroup(float frame, LightComponents group, out LightKeyframe value)
    {
        value = default;

        TrackKeyframe? previous = null;
        TrackKeyframe? next = null;

        foreach(var keyframe in _track.Keyframes)
        {
            if(!keyframe.Light.HasValue || !keyframe.LightComponents.HasFlag(group))
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

            value = previous.Light!.Value;
            return true;
        }

        if(previous is null || next.Frame == frame)
        {
            value = next.Light!.Value;
            return true;
        }

        if(next.InterpolationMode == InterpolationMode.Step)
        {
            value = previous.Light!.Value;
            return true;
        }

        var span = next.Frame - previous.Frame;
        var t = span == 0 ? 0f : (frame - previous.Frame) / span;

        value = LightKeyframe.Lerp(previous.Light!.Value, next.Light!.Value, next.EvaluateEasing(t));
        return true;
    }

    public override void Dispose()
    {
        _timelineService.Unregister(this);
        base.Dispose();
    }

    public static LightTimelineCapability? CreateIfEligible(IServiceProvider provider, LightEntity entity)
    {
        if(entity.GameLight.IsWorldLight)
            return null;

        return ActivatorUtilities.CreateInstance<LightTimelineCapability>(provider, entity);
    }
}
