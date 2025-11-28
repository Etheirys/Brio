using System.Numerics;

namespace ImSequencer;

public class Extensions
{
    public static float ImLerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
    public static Vector2 Add(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
    
}
