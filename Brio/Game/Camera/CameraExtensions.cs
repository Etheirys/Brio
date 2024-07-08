using System.Numerics;

namespace Brio.Game.Camera;

internal static class CameraExtensions
{
    public unsafe static Matrix4x4 GetProjectionMatrix(this BrioCamera camera)
    {
        var cam = camera.Camera.CameraBase.SceneCamera.RenderCamera;
        var proj = cam->ProjectionMatrix;
        proj.M33 = -(cam->FarPlane + cam->NearPlane) / (cam->FarPlane - cam->NearPlane);
        proj.M43 = -(2f * cam->FarPlane * cam->NearPlane) / (cam->FarPlane - cam->NearPlane);

        return proj;
    }

    public unsafe static Matrix4x4 GetViewMatrix(this BrioCamera camera)
    {
        var viewMatrix = camera.Camera.CameraBase.SceneCamera.ViewMatrix;
        viewMatrix.M44 = 1; //hcsf
        return viewMatrix;
    }

    public unsafe static bool WorldToScreen(this BrioCamera camera, Vector3 world, out Vector2 screen)
    {
        if(camera.Camera.CameraBase.SceneCamera.WorldToScreen(world, out var ffScreen))
        {
            screen = ffScreen;
            return true;
        }
        screen = Vector2.Zero;
        return false;
    }

    public unsafe static bool ScreenToWorld(this BrioCamera camera, Vector2 screen, out Vector3 world)
    {
        if(camera.Camera.CameraBase.SceneCamera.ScreenToWorld(screen, out var ffScreen))
        {
            world = ffScreen;
            return true;
        }
        world = Vector3.Zero;
        return false;
    }

    public unsafe static Vector3 GetPosition(this BrioCamera camera)
    {
        var viewMatrix = camera.GetViewMatrix();

        if(Matrix4x4.Invert(viewMatrix, out var invertedViewMatrix))
            return invertedViewMatrix.Translation;

        return Vector3.Zero;
    }

    public unsafe static float GetFoV(this BrioCamera camera)
    {
        return camera.Camera.FoV;
    }
}
