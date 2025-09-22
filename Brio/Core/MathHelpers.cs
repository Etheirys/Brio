using System;
using System.Numerics;

namespace Brio.Core;

public static class MathHelpers
{
    public const float DegreesToRadians = MathF.PI / 180.0f;
    public const float RadiansToDegrees = 180.0f / MathF.PI;
   
    public const float Deg2Rad = MathF.PI * 2.0f / 360.0f;
    public const float Rad2Deg = 1.0f / Deg2Rad;
}
