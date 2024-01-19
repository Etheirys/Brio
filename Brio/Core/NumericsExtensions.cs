using System;
using System.Numerics;
namespace Brio.Core;

internal static class NumericsExtensions
{
    public static Quaternion ToQuaternion(this Vector3 euler)
    {
        euler *= MathHelpers.DegreesToRadians;
        Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(euler.X, euler.Y, euler.Z);
        return Quaternion.Normalize(quaternion);
    }

    public static Vector3 ToEuler(this Quaternion r)
    {
        float yaw = MathF.Atan2(2.0f * (r.Y * r.W + r.X * r.Z), 1.0f - 2.0f * (r.X * r.X + r.Y * r.Y));
        float pitch = MathF.Asin(2.0f * (r.X * r.W - r.Y * r.Z));
        float roll = MathF.Atan2(2.0f * (r.X * r.Y + r.Z * r.W), 1.0f - 2.0f * (r.X * r.X + r.Z * r.Z));

        return new Vector3(yaw, pitch, roll) * MathHelpers.RadiansToDegrees;
    }

    public static bool IsPointInPolygon(this ref Vector2 point, Vector2[] polygon)
    {
        int i, j = polygon.Length - 1;
        bool inside = false;
        for(i = 0; i < polygon.Length; i++)
        {
            if(polygon[i].Y > point.Y != polygon[j].Y > point.Y &&
                point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    public static Matrix4x4 ToMatrix(this Transform transform)
    {
        Matrix4x4 mat = Matrix4x4.Identity;

        mat *= Matrix4x4.CreateScale(transform.Scale);

        Quaternion normalizedRotation = Quaternion.Normalize(transform.Rotation);
        mat *= Matrix4x4.CreateFromQuaternion(normalizedRotation);

        mat.M41 = transform.Position.X;
        mat.M42 = transform.Position.Y;
        mat.M43 = transform.Position.Z;

        return mat;
    }

    public static Transform ToTransform(this Matrix4x4 matrix)
    {
        Vector3 position = matrix.Translation;

        Vector3 scale = new(
            new Vector3(matrix.M11, matrix.M12, matrix.M13).Length(),
            new Vector3(matrix.M21, matrix.M22, matrix.M23).Length(),
            new Vector3(matrix.M31, matrix.M32, matrix.M33).Length()
        );

        scale.X = Math.Abs(scale.X) < float.Epsilon ? 0.01f : scale.X;
        scale.Y = Math.Abs(scale.Y) < float.Epsilon ? 0.01f : scale.Y;
        scale.Z = Math.Abs(scale.Z) < float.Epsilon ? 0.01f : scale.Z;

        Matrix4x4 rotationMatrix = new Matrix4x4(
            matrix.M11 / scale.X, matrix.M12 / scale.X, matrix.M13 / scale.X, 0,
            matrix.M21 / scale.Y, matrix.M22 / scale.Y, matrix.M23 / scale.Y, 0,
            matrix.M31 / scale.Z, matrix.M32 / scale.Z, matrix.M33 / scale.Z, 0,
            0, 0, 0, 1
        );

        Quaternion rotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

        Transform decomposedTransform = new()
        {
            Position = position,
            Rotation = rotation,
            Scale = scale
        };

        return decomposedTransform;
    }

    public static bool IsApproximatelySame(this Vector3 me, Vector3 other, float tolerance = 0.000001f)
    {
        return Vector3.Distance(me, other) <= tolerance;
    }

    public static bool IsApproximatelySame(this Quaternion me, Quaternion other, float tolerance = 0.000001f)
    {
        return Quaternion.Dot(me, other) >= 1 - tolerance;
    }
}
