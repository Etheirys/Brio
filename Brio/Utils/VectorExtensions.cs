using System.Numerics;

namespace Brio.Utils;
public static class VectorExtensions
{
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
}
