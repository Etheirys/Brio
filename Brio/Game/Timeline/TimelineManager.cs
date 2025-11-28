using System.Numerics;

namespace Brio.Game.Timeline;

public class TimelineManager
{
    public record CameraKeyframe(int Frame, Vector3 Position, Quaternion Rotation, float FoV);
    
}
