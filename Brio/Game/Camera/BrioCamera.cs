
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

    // Converts Brio Camera Angle and Pan Vector2s to Vector3 for Free Camera Rotation
    // using Cubic Polynomials and Quadratic Equations
    private readonly Vector3 ConvertBrioCameraToXIVCamera()
    { 
        const float cubicACoefficient = 0.3083f;
        const float cubicBCoefficient = -3.5417f;
        const float cubicCCoefiicient = 14.5646f;
        const float cubicDCoefficient = -24.6688f;

        const float quadraticACoefficient = -67.7f;
        const float quadraticBCoefficient = 72.45f;

        const float CONSTANT_X_OFFSET = 57.3f;

        // Calculate offset based on Zoom level
        // Yes.. this affects every calculation below
        // This will be used as the C coefficient
        float panYOffset = (float)((cubicACoefficient * Math.Pow(2.5, 3)) +
                           (cubicBCoefficient * Math.Pow(2.5, 2)) +
                           (cubicCCoefiicient * 2.5) +
                           cubicDCoefficient);

        Brio.Log.Verbose($"Zoom: {Zoom}, PanYOffset: {panYOffset}");

        Vector3 rotation;
        rotation.X = 0f;

        float newXDegrees;
        if (Angle.X == Pan.X)
        {
            newXDegrees = 0f;
        } else
        {
            newXDegrees = CONSTANT_X_OFFSET * (Angle.X - Pan.X);
        }

        float newYDegrees;
        if((Angle.Y + Pan.Y < -2f) || (Angle.Y + Pan.Y > 2f))
        {
            // cap the angle and pan as beyond is position changes
            // cap A at 1 (multiplication), B at 2 (addition)
            if(Angle.Y + Pan.Y > 2f)
            {
                newYDegrees = (quadraticACoefficient * 1f) + (quadraticBCoefficient * 2f) + panYOffset; 
            }
            else
            {
                newYDegrees = (quadraticACoefficient * -1f) + (quadraticBCoefficient * -2f) + panYOffset;
            }
        }
        else
        {
            newYDegrees = (quadraticACoefficient * (Angle.Y * Pan.Y)) + (quadraticBCoefficient * (Angle.Y + Pan.Y)) + panYOffset;
        }
        newYDegrees = -newYDegrees; // Invert for XIV Camera

        // convert to radians
        float newXRadians = newXDegrees * MathHelpers.DegreesToRadians;
        float newYRadians = newYDegrees * MathHelpers.DegreesToRadians;

        rotation.X = newXRadians;
        rotation.Y = newYRadians;
        rotation.Z = Roll;

        return rotation;
    }

    public readonly Vector3 RotationAsVector3 => ConvertBrioCameraToXIVCamera(); //new(Angle.X - Pan.X, -Angle.Y - Pan.Y, Roll);

    public readonly Quaternion CalculateDirectionAsQuaternion()
        => (new Vector3(-(Angle.Y + Pan.Y), ((Angle.X + MathF.PI) % MathF.Tau) - Pan.X, 0.0f)
                * MathHelpers.Rad2Deg).ToEulerAngles();
}
