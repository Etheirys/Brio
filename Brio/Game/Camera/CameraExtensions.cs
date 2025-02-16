using System.Numerics;

namespace Brio.Game.Camera;

public static class CameraExtensions
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

    public static Vector3 RotatePosition(this Quaternion left, Vector3 right)
    {
        float num = left.X * 2f;
        float num2 = left.Y * 2f;
        float num3 = left.Z * 2f;
        float num4 = left.X * num;
        float num5 = left.Y * num2;
        float num6 = left.Z * num3;
        float num7 = left.X * num2;
        float num8 = left.X * num3;
        float num9 = left.Y * num3;
        float num10 = left.W * num;
        float num11 = left.W * num2;
        float num12 = left.W * num3;
        float x = ((1f - (num5 + num6)) * right.X) + ((num7 - num12) * right.Y) + ((num8 + num11) * right.Z);
        float y = ((num7 + num12) * right.X) + ((1f - (num4 + num6)) * right.Y) + ((num9 - num10) * right.Z);
        float z = ((num8 - num11) * right.X) + ((num9 + num10) * right.Y) + ((1f - (num4 + num5)) * right.Z);
        return new Vector3(x, y, z);
    }

}
