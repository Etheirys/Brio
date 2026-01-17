using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Core;

public class GroupedTransformHelper
{
    public static Vector3 CalculateCentroid(IEnumerable<Transform> transforms)
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

    public static Transform ApplyRotationDeltaAroundPivot(Transform original, Vector3 pivot, Quaternion rotationDelta)
    {
        return RotateAroundPivot(original, pivot, rotationDelta);
    }
}
