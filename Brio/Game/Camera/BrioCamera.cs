
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

    // Converts Brio Camera Angle and Pan Vector2s to a single Vector3 for Free Camera use
    private readonly Vector3 ConvertBrioCameraToXIVCamera()
    {
        // Used for calculating rotation Y offset based on zoom level
        // A lot of math calculation was done to get these values so don't
        // touch them unless you know what you're doing.
        const float RECIPROCAL_A_VALUE = 0.73f;
        const float RECIPROCAL_B_VALUE = 15.742f;

        // 1. Get approximate offset from zoom
        // Use reciprocal of zoom to get rotation offset for Y axis
        float currentCameraZoom;
        if(Distance == 0)
        {
            Brio.Log.Warning("Distance is zero. Camera might be borked. Defaulting to 2.5f.");
            currentCameraZoom = 2.5f; // Default for Brio camera
        }
        else
        {
            currentCameraZoom = Distance;
        }

        // 2. Setup constants for rotation changes on Pan and Angle axis'
        // Same as 1, don't touch unless you know what you're doing.
        float ROT_Y_CHANGE_FROM_PAN_Y = ((0.878f * currentCameraZoom) + 51.805f) * MathHelpers.DegreesToRadians;
        const float ROT_Y_CHANGE_FROM_ANGLE_Y = 51.1f * MathHelpers.DegreesToRadians;
        const float ROT_X_CHANGE_FROM_PAN_X = 57.3f * MathHelpers.DegreesToRadians;
        const float ROT_X_CHANGE_FROM_ANGLE_X = 57.33f * MathHelpers.DegreesToRadians;

        // 3. Offset Rotation Y based on zoom level after calculating pan and angle values
        float rotationYOffset = (RECIPROCAL_A_VALUE - (RECIPROCAL_B_VALUE / currentCameraZoom)) * MathHelpers.DegreesToRadians;
        rotationYOffset = -rotationYOffset; // Invert Y rotation to match rotation later on

        // 4. Calculate rotation angles (degrees)
        float rotX =
            (ROT_X_CHANGE_FROM_PAN_X * Pan.X)
            - (ROT_X_CHANGE_FROM_ANGLE_X * Angle.X);
        rotX = -rotX; // Invert X rotation to match Free Camera

        // 4.1 Setup exponent for Pan.Y to make it less sensitive at lower values
        const float PAN_Y_EXPONENT_INTERCEPT = 1.501f;
        const float PAN_Y_EXPONENT_SLOPE = -0.6f;
        float panY = Math.Clamp(Pan.Y, -1f, 1f);
        float effectivePanYExponent = PAN_Y_EXPONENT_INTERCEPT + (PAN_Y_EXPONENT_SLOPE * panY);

        // 4.2 Calculate rotY with exponent applied to Pan.Y
        float rotY =
            (ROT_Y_CHANGE_FROM_PAN_Y * MathF.Pow(Pan.Y, effectivePanYExponent))
            + (ROT_Y_CHANGE_FROM_ANGLE_Y * Angle.Y);
        rotY = -rotY; // Invert Y rotation to match Free Camera

        Brio.Log.Debug($"rotY: {rotY * MathHelpers.RadiansToDegrees}");

        if (rotY == 0f)
        {
            // apply offset for Y if no other Y rotation is applied
            Brio.Log.Debug("rotY returned 0, applying offset as rotation.Y");
            rotY = rotationYOffset;
        }

        // 5. Convert Rotation to Quaternion Euler
        Vector3 rotation;
        rotation = new Vector3(rotX, rotY, Roll);

        return rotation;
    }

    public readonly Vector3 RotationAsVector3 => ConvertBrioCameraToXIVCamera();

    public readonly Quaternion CalculateDirectionAsQuaternion()
        => (new Vector3(-(Angle.Y + Pan.Y), ((Angle.X + MathF.PI) % MathF.Tau) - Pan.X, 0.0f)
                * MathHelpers.Rad2Deg).ToEulerAngles();
}
