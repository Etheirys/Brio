
//
// Come of the offsets here are from:
// Lights, Camera, Action https://github.com/NeNeppie/LightsCameraAction by NeNeppie
//

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

    [FieldOffset(0x124)] public float Distance;     // default is 6
    [FieldOffset(0x128)] public float MinDistance;  // 1.5
    [FieldOffset(0x12C)] public float MaxDistance;  // 20

    [FieldOffset(0x130)] public float FoV;          // default is 0.78
    [FieldOffset(0x134)] public float MinFoV;       // 0.69
    [FieldOffset(0x138)] public float MaxFoV;       // 0.78
    [FieldOffset(0x13C)] public float Zoom;         // -0.5 to 0.5, default is 0

    [FieldOffset(0x140)] public Vector2 Angle;
    [FieldOffset(0x160)] public Vector2 Pan;        // Pan, Tilt
    [FieldOffset(0x170)] public float Roll;
    
    [FieldOffset(0x180)] public int Mode;           // 0 = 1st Person. 1 = 3rd Person. 2+ = Restrictive camera control 

    [FieldOffset(0x218)] public Vector2 Collide;

    public readonly Vector3 RotationAsVector3 => new(Angle.X - Pan.X, -Angle.Y - Pan.Y, Roll);

    public readonly Quaternion CalculateDirectionAsQuaternion()
        => (new Vector3(-(Angle.Y + Pan.Y), ((Angle.X + MathF.PI) % MathF.Tau) - Pan.X, 0.0f)
                * MathHelpers.Rad2Deg).ToEulerAngles();
}
