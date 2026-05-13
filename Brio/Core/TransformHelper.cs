using Brio.Entities.Core;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Core;

public static class TransformHelper
{
    public static void ApplyDelta(ITransformable target, Transform delta)
    {
        if(target.IsTransformFrozen)
            return;

        target.Transform += delta;
    }

    public static void ApplyDeltaToMultiple(IReadOnlyList<(EntityId id, ITransformable target, Transform snapshot)> targets, Transform delta, Vector3 pivot, bool applyAroundPivot)
    {
        foreach(var (_, target, snapshot) in targets)
        {
            if(target.IsTransformFrozen)
                continue;

            Transform newTransform;
            if(applyAroundPivot && delta.Rotation != Quaternion.Identity)
            {
                newTransform = RotateAroundPivot(snapshot, pivot, delta.Rotation);

                if(delta.Position != Vector3.Zero)
                    newTransform.Position += delta.Position;

                if(delta.Scale != Vector3.Zero)
                    newTransform.Scale += delta.Scale;
            }
            else
            {
                newTransform = snapshot + delta;
            }

            target.Transform = newTransform;
        }
    }

    public static void SnapshotAll(IEnumerable<ITransformable> targets)
    {
        foreach(var target in targets)
        {
            target.Snapshot();
        }
    }

    public static Vector3 GetCentroidForGivenTransforms(IEnumerable<Transform> transforms)
    {
        var positions = transforms.Select(t => t.Position).ToList();
        if(positions.Count == 0)
            return Vector3.Zero;

        var sum = positions.Aggregate(Vector3.Zero, (acc, pos) => acc + pos);
        return sum / positions.Count;
    }

    public static Transform RotateAroundPivot(Transform original, Vector3 pivot, Quaternion rotation)
    {
        Vector3 relativePosition = original.Position - pivot;

        Vector3 rotatedPosition = Vector3.Transform(relativePosition, rotation);

        Vector3 newPosition = rotatedPosition + pivot;

        Quaternion newRotation = Quaternion.Normalize(rotation * original.Rotation);

        return new Transform
        {
            Position = newPosition,
            Rotation = newRotation,
            Scale = original.Scale
        };
    }
}
