
using Brio.Config;
using ImGuiNET;
using ImGuizmoNET;
using System.Numerics;

namespace Brio.Core;

internal static class ImGuizmoExtensions
{
    public static bool MouseWheelManipulate(ref Matrix4x4 matrix)
    {
        if(ImGui.IsAnyMouseDown())
            return false;

        float mouseWheel = ImGui.GetIO().MouseWheel / 100;

        if(mouseWheel != 0)
        {
            bool smallIncrement = ImGui.IsKeyDown(ConfigurationService.Instance.Configuration.Interface.IncrementSmall);
            if(smallIncrement)
                mouseWheel /= 10;

            bool largeIncrement = ImGui.IsKeyDown(ConfigurationService.Instance.Configuration.Interface.IncrementLarge);
            if(largeIncrement)
                mouseWheel *= 10;

            if(ImGuizmo.IsOver(OPERATION.ROTATE_X))
            {
                matrix = Matrix4x4.CreateRotationX(mouseWheel) * matrix;
                return true;
            }
            else if(ImGuizmo.IsOver(OPERATION.ROTATE_Y))
            {
                matrix = Matrix4x4.CreateRotationY(mouseWheel) * matrix;
                return true;
            }
            else if(ImGuizmo.IsOver(OPERATION.ROTATE_Z))
            {
                matrix = Matrix4x4.CreateRotationZ(mouseWheel) * matrix;
                return true;
            }
        }

        return false;
    }
}
