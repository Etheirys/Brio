using Brio.Core;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace Brio.Game.Camera;

[StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
public struct BrioCamera
{
    [FieldOffset(0x000)] public GameCamera Camera;
    [FieldOffset(0x060)] public Vector3 Position;
    [FieldOffset(0x13C)] public float FoV;
    [FieldOffset(0x13C)] public float Zoom;
    [FieldOffset(0x140)] public Vector2 Angle;
    [FieldOffset(0x160)] public Vector2 Pan;
    [FieldOffset(0x170)] public float Rotation;
    [FieldOffset(0x218)] public Vector2 Collide;

    public readonly Vector3 RotationAsVector3 => new(Angle.X - Pan.X, -Angle.Y - Pan.Y, Rotation);

    public readonly Quaternion CalculateDirectionAsQuaternion()
        => (new Vector3(-(Angle.Y + Pan.Y), ((Angle.X + MathF.PI) % MathF.Tau) - Pan.X, 0.0f)
                * MathHelpers.Rad2Deg).ToEulerAngles();
}
