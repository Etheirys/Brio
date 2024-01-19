using ImGuiNET;
using ImGuizmoNET;
using System.Numerics;

namespace Brio.Core;

internal static class ImGuizmoExtensions
{
    public static bool MouseWheelManipulate(ref Matrix4x4 matrix)
    {
        float mouseWheel = ImGui.GetIO().MouseWheel / 100;

        if(ImGui.GetIO().KeyCtrl)
            mouseWheel /= 10;

        if(ImGui.GetIO().KeyShift)
            mouseWheel *= 10;

        if(mouseWheel != 0)
        {
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
