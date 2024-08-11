using System.Numerics;

namespace Brio.Game.Camera;

public record class CameraState(Matrix4x4 ViewMatrix, float FoV);

public class VirtualCamera
{
    public bool IsActive { get; set; } = false;

    public CameraState State { get; set; } = new CameraState(Matrix4x4.Identity, 0.78f);
}
