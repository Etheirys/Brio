using Brio.Core;
using Brio.Game.Posing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Services.Timeline;

public enum KeyframeShape
{
    Diamond,
    Circle,
    Square
}
public enum InterpolationMode
{
    Bezier,
    Step
}

[Flags]
public enum CameraComponents
{
    None = 0,
    Position = 1,
    Rotation = 2,
    Lens = 4,

    All = Position | Rotation | Lens,
}
[Flags]
public enum LightComponents
{
    None = 0,
    Position = 1,
    Rendering = 2,

    All = Position | Rendering,
}
[Flags]
public enum WorldObjectComponents
{
    None = 0,
    Transform = 1,
    Color = 2,

    All = Transform | Color,
}

public struct CameraKeyframe
{
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector2 Angle;
    public Vector2 Pan;
    public float Zoom;
    public float FoV;
    public float PivotRotation;

    public static CameraKeyframe Lerp(CameraKeyframe a, CameraKeyframe b, float t) => new()
    {
        Position = Vector3.Lerp(a.Position, b.Position, t),
        Rotation = Vector3.Lerp(a.Rotation, b.Rotation, t),

        Angle = Vector2.Lerp(a.Angle, b.Angle, t),
        Pan = Vector2.Lerp(a.Pan, b.Pan, t),

        Zoom = a.Zoom + (b.Zoom - a.Zoom) * t,
        FoV = a.FoV + (b.FoV - a.FoV) * t,
        PivotRotation = a.PivotRotation + (b.PivotRotation - a.PivotRotation) * t
    };
}
public struct LightKeyframe
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public Vector3 Color;
    public float Intensity;
    public float Range;
    public float FalloffFactor;
    public float SpotLightAngleDegrees;
    public float AngularFalloffDegrees;

    public static LightKeyframe Lerp(LightKeyframe a, LightKeyframe b, float t) => new()
    {
        Position = Vector3.Lerp(a.Position, b.Position, t),
        Rotation = Quaternion.Lerp(a.Rotation, b.Rotation, t),
        Scale = Vector3.Lerp(a.Scale, b.Scale, t),
        Color = Vector3.Lerp(a.Color, b.Color, t),

        Intensity = a.Intensity + (b.Intensity - a.Intensity) * t,
        Range = a.Range + (b.Range - a.Range) * t,
        FalloffFactor = a.FalloffFactor + (b.FalloffFactor - a.FalloffFactor) * t,
        SpotLightAngleDegrees = a.SpotLightAngleDegrees + (b.SpotLightAngleDegrees - a.SpotLightAngleDegrees) * t,
        AngularFalloffDegrees = a.AngularFalloffDegrees + (b.AngularFalloffDegrees - a.AngularFalloffDegrees) * t
    };
}
public struct WorldObjectKeyframe
{
    public Vector3 Position;
    public Quaternion Rotation;

    public Vector3 Scale;
    public Vector4 Color;

    public static WorldObjectKeyframe Lerp(WorldObjectKeyframe a, WorldObjectKeyframe b, float t) => new()
    {
        Position = Vector3.Lerp(a.Position, b.Position, t),
        Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t),
        Scale = Vector3.Lerp(a.Scale, b.Scale, t),

        Color = Vector4.Lerp(a.Color, b.Color, t)
    };
}

public class TrackKeyframe(int frame, Transform transform)
{
    public Guid Id { get; } = Guid.NewGuid();
    public int Frame { get; set; } = frame;
    public Transform Transform { get; set; } = transform;

    public Vector2 P1 { get; set; } = new(0.25f, 0.25f);
    public Vector2 P2 { get; set; } = new(0.75f, 0.75f);

    public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Bezier;
    public KeyframeShape Shape { get; set; } = KeyframeShape.Diamond;
    public uint? CustomColor { get; set; }

    public TransformComponents Components { get; set; } = TransformComponents.All;

    public CameraKeyframe? Camera { get; set; }
    public CameraComponents CameraComponents { get; set; } = CameraComponents.All;

    public LightKeyframe? Light { get; set; }
    public LightComponents LightComponents { get; set; } = LightComponents.All;


    public WorldObjectKeyframe? WorldObject { get; set; }
    public WorldObjectComponents WorldObjectComponents { get; set; } = WorldObjectComponents.All;

    public void MergeTransform(Transform transform, TransformComponents components)
    {
        var merged = Transform;
        if(components.HasFlag(TransformComponents.Position))
            merged.Position = transform.Position;
        if(components.HasFlag(TransformComponents.Rotation))
            merged.Rotation = transform.Rotation;
        if(components.HasFlag(TransformComponents.Scale))
            merged.Scale = transform.Scale;

        Transform = merged;
        Components |= components;
    }
    public void MergeCamera(CameraKeyframe camera, CameraComponents components)
    {
        var merged = Camera ?? camera;
        if(components.HasFlag(CameraComponents.Position))
            merged.Position = camera.Position;
        if(components.HasFlag(CameraComponents.Rotation))
            merged.Rotation = camera.Rotation;
        if(components.HasFlag(CameraComponents.Lens))
        {
            merged.Angle = camera.Angle;
            merged.Pan = camera.Pan;
            merged.Zoom = camera.Zoom;
            merged.FoV = camera.FoV;
            merged.PivotRotation = camera.PivotRotation;
        }

        Camera = merged;
        CameraComponents |= components;
    }
    public void MergeLight(LightKeyframe light, LightComponents components)
    {
        var merged = Light ?? light;
        if(components.HasFlag(LightComponents.Position))
        {
            merged.Position = light.Position;
            merged.Rotation = light.Rotation;
            merged.Scale = light.Scale;
        }
        if(components.HasFlag(LightComponents.Rendering))
        {
            merged.Color = light.Color;
            merged.Intensity = light.Intensity;
            merged.Range = light.Range;
            merged.FalloffFactor = light.FalloffFactor;
            merged.SpotLightAngleDegrees = light.SpotLightAngleDegrees;
            merged.AngularFalloffDegrees = light.AngularFalloffDegrees;
        }

        Light = merged;
        LightComponents |= components;
    }
    public void MergeWorldObject(WorldObjectKeyframe worldObject, WorldObjectComponents components)
    {
        var merged = WorldObject ?? worldObject;
        if(components.HasFlag(WorldObjectComponents.Transform))
        {
            merged.Position = worldObject.Position;
            merged.Rotation = worldObject.Rotation;
            merged.Scale = worldObject.Scale;
        }
        if(components.HasFlag(WorldObjectComponents.Color))
            merged.Color = worldObject.Color;

        WorldObject = merged;
        WorldObjectComponents |= components;
    }

    //

    public float EvaluateEasing(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        if(t <= 0f)
            return 0f;
        if(t >= 1f)
            return 1f;

        var solved = SolveBezierX(t, P1.X, P2.X);
        return BezierCoordinate(solved, P1.Y, P2.Y);
    }
    private static float BezierCoordinate(float t, float p1, float p2)
    {
        var u = 1f - t;
        var tt = t * t;
        var uu = u * u;
        return (3f * uu * t * p1) + (3f * u * tt * p2) + (tt * t);
    }
    private static float SolveBezierX(float x, float p1x, float p2x)
    {
        var t = x;
        for(var i = 0; i < 5; i++)
        {
            var currentX = BezierCoordinate(t, p1x, p2x);
            var derivative = (3f * (1f - t) * (1f - t) * p1x) + (6f * (1f - t) * t * (p2x - p1x)) + (3f * t * t * (1f - p2x));
            if(Math.Abs(derivative) < 1e-6f)
                break;
            t -= (currentX - x) / derivative;
        }
        return Math.Clamp(t, 0f, 1f);
    }
}

public class TimelineTrack(BonePoseInfoId boneId, string displayName)
{
    public BonePoseInfoId BoneId { get; } = boneId;
    public string Name => BoneId.BoneName;

    public string DisplayName { get; } = displayName;

    public string ParentName { get; set; } = string.Empty;
    public bool HasChildren { get; set; }

    public int Depth { get; set; }
    public bool IsExpanded { get; set; } = true;
    public bool IsMuted { get; set; }

    public List<TrackKeyframe> Keyframes { get; set; } = [];

    public TrackKeyframe AddPoseKeyframe(int frame, Transform transform, TransformComponents components, InterpolationMode mode = InterpolationMode.Bezier)
    {
        var existing = Keyframes.FirstOrDefault(k => k.Frame == frame);
        if(existing is not null)
        {
            existing.MergeTransform(transform, components);
            return existing;
        }

        var keyframe = new TrackKeyframe(frame, transform) { Components = components, InterpolationMode = mode };
        Keyframes.Add(keyframe);
        Keyframes = [.. Keyframes.OrderBy(k => k.Frame)];
        return keyframe;
    }
    public TrackKeyframe AddCameraKeyframe(int frame, CameraKeyframe camera, CameraComponents components, InterpolationMode mode = InterpolationMode.Bezier)
    {
        var existing = Keyframes.FirstOrDefault(k => k.Frame == frame);
        if(existing is not null)
        {
            existing.MergeCamera(camera, components);
            return existing;
        }

        var keyframe = new TrackKeyframe(frame, Transform.Identity) { Camera = camera, CameraComponents = components, InterpolationMode = mode };
        Keyframes.Add(keyframe);
        Keyframes = [.. Keyframes.OrderBy(k => k.Frame)];
        return keyframe;
    }
    public TrackKeyframe AddLightKeyframe(int frame, LightKeyframe light, LightComponents components, InterpolationMode mode = InterpolationMode.Bezier)
    {
        var existing = Keyframes.FirstOrDefault(k => k.Frame == frame);
        if(existing is not null)
        {
            existing.MergeLight(light, components);
            return existing;
        }

        var keyframe = new TrackKeyframe(frame, Transform.Identity) { Light = light, LightComponents = components, InterpolationMode = mode };
        Keyframes.Add(keyframe);
        Keyframes = [.. Keyframes.OrderBy(k => k.Frame)];
        return keyframe;
    }
    public TrackKeyframe AddWorldObjectKeyframe(int frame, WorldObjectKeyframe worldObject, WorldObjectComponents components, InterpolationMode mode = InterpolationMode.Bezier)
    {
        var existing = Keyframes.FirstOrDefault(k => k.Frame == frame);
        if(existing is not null)
        {
            existing.MergeWorldObject(worldObject, components);
            return existing;
        }

        var keyframe = new TrackKeyframe(frame, Transform.Identity) { WorldObject = worldObject, WorldObjectComponents = components, InterpolationMode = mode };
        Keyframes.Add(keyframe);
        Keyframes = [.. Keyframes.OrderBy(k => k.Frame)];
        return keyframe;
    }

    public void DeleteKeyframe(Guid id) => Keyframes.RemoveAll(k => k.Id == id);

    public Transform? Sample(float frame, Transform? defaultTransform)
    {
        if(IsMuted || Keyframes.Count == 0)
            return null;

        var def = defaultTransform ?? Keyframes[0].Transform;

        return new Transform
        {
            Position = SampleComponent(frame, TransformComponents.Position, def.Position, k => k.Transform.Position, Vector3.Lerp),
            Rotation = SampleComponent(frame, TransformComponents.Rotation, def.Rotation, k => k.Transform.Rotation, Quaternion.Slerp),
            Scale = SampleComponent(frame, TransformComponents.Scale, def.Scale, k => k.Transform.Scale, Vector3.Lerp)
        };
    }

    private T SampleComponent<T>(float frame, TransformComponents component, T defaultValue, Func<TrackKeyframe, T> selector, Func<T, T, float, T> lerp)
    {
        TrackKeyframe? previous = null;
        TrackKeyframe? next = null;

        foreach(var keyframe in Keyframes)
        {
            if(!keyframe.Components.HasFlag(component))
                continue;

            if(keyframe.Frame >= frame)
            {
                next = keyframe;
                break;
            }

            previous = keyframe;
        }

        if(next is null)
            return previous is null ? defaultValue : selector(previous);

        if(previous is null)
        {
            if(frame < next.Frame)
            {
                if(next.InterpolationMode == InterpolationMode.Step)
                    return defaultValue;

                var introT = next.Frame == 0 ? 1f : frame / next.Frame;
                return lerp(defaultValue, selector(next), next.EvaluateEasing(introT));
            }
            return selector(next);
        }

        if(next.Frame == frame)
            return selector(next);

        if(next.InterpolationMode == InterpolationMode.Step)
            return selector(previous);

        var span = next.Frame - previous.Frame;
        var deltat = span == 0 ? 0f : (frame - previous.Frame) / span;
        return lerp(selector(previous), selector(next), next.EvaluateEasing(deltat));
    }
}
