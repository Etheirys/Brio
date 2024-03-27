using FFXIVClientStructs.Havok;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

using StructsTransforms = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;

namespace Brio.Core;

[StructLayout(LayoutKind.Sequential)]
internal struct Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static Transform Identity => new()
    {
        Position = Vector3.Zero,
        Rotation = Quaternion.Identity,
        Scale = Vector3.Zero
    };

    public static implicit operator Transform(StructsTransforms transform)
    {
        return new Transform()
        {
            Position = transform.Position,
            Rotation = transform.Rotation,
            Scale = transform.Scale
        };
    }

    public unsafe static implicit operator Transform(StructsTransforms* transform)
    {
        return new Transform()
        {
            Position = transform->Position,
            Rotation = transform->Rotation,
            Scale = transform->Scale
        };
    }

    public static implicit operator StructsTransforms(Transform transform)
    {
        return new StructsTransforms()
        {
            Position = transform.Position,
            Rotation = transform.Rotation,
            Scale = transform.Scale
        };
    }

    public static implicit operator Transform(hkQsTransformf transform)
    {
        return new Transform()
        {
            Position = new Vector3(transform.Translation.X, transform.Translation.Y, transform.Translation.Z),
            Rotation = new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W),
            Scale = new Vector3(transform.Scale.X, transform.Scale.Y, transform.Scale.Z)
        };
    }

    public unsafe static implicit operator Transform(hkQsTransformf* transform)
    {
        return new Transform()
        {
            Position = new Vector3(transform->Translation.X, transform->Translation.Y, transform->Translation.Z),
            Rotation = new Quaternion(transform->Rotation.X, transform->Rotation.Y, transform->Rotation.Z, transform->Rotation.W),
            Scale = new Vector3(transform->Scale.X, transform->Scale.Y, transform->Scale.Z)
        };
    }

    public static Transform operator +(Transform a, Transform b)
    {
        return new()
        {
            Position = a.Position + b.Position,
            Rotation = Quaternion.Normalize(a.Rotation * b.Rotation),
            Scale = a.Scale + b.Scale
        };
    }

    public Transform CalculateDiff(Transform other)
    {
        return new Transform()
        {
            Position = Position - other.Position,
            Rotation = Quaternion.Normalize(Quaternion.Conjugate(other.Rotation) * Rotation),
            Scale = Scale - other.Scale
        };
    }

    public readonly bool IsApproximatelySame(Transform other, float tolerance = 0.000001f)
    {
        return Position.IsApproximatelySame(other.Position, tolerance) &&
               Rotation.IsApproximatelySame(other.Rotation, tolerance) &&
               Scale.IsApproximatelySame(other.Scale, tolerance);
    }

    public void Filter(TransformComponents keep)
    {
        if(!keep.HasFlag(TransformComponents.Position))
            Position = Vector3.Zero;

        if(!keep.HasFlag(TransformComponents.Rotation))
            Rotation = Quaternion.Identity;

        if(!keep.HasFlag(TransformComponents.Scale))
            Scale = Vector3.Zero;
    }

    public Transform Inverted() => new()
    {
        Position = -Position,
        Rotation = Quaternion.Conjugate(Rotation),
        Scale = -Scale
    };

    public readonly bool ContainsNaN()
    {
        if(float.IsNaN(Position.X) || float.IsNaN(Position.Y) || float.IsNaN(Position.Z) ||
            float.IsNaN(Rotation.X) || float.IsNaN(Rotation.Y) || float.IsNaN(Rotation.Z) ||
            float.IsNaN(Scale.X) || float.IsNaN(Scale.Y) || float.IsNaN(Scale.Z))
        {
            return true;
        }

        return false;
    }

}

[Flags]
internal enum TransformComponents
{
    None = 0,
    Position = 1,
    Rotation = 2,
    Scale = 4,

    All = Position | Rotation | Scale,
}
