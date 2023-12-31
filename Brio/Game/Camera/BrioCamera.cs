namespace Brio.Game.Camera;

using System.Numerics;
using System.Runtime.InteropServices;
using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

[StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
internal struct BrioCamera
{
    [FieldOffset(0x0)]
    public GameCamera Camera;

    [FieldOffset(0x12C)] public float FoV;

    [FieldOffset(0x130)] public Vector2 Angle;

    [FieldOffset(0x150)] public Vector2 Pan;

    [FieldOffset(0x160)] public float Rotation;
}
