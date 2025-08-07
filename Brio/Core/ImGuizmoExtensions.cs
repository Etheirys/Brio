using Brio.Input;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using System.Numerics;

namespace Brio.Core;

public static class ImGuizmoExtensions
{
    public static bool MouseWheelManipulate(ref Matrix4x4 matrix)
    {
        if(ImGui.IsAnyMouseDown())
            return false;

        float mouseWheel = ImGui.GetIO().MouseWheel / 100;

        if(mouseWheel != 0)
        {
            bool smallIncrement = InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier);
            if(smallIncrement)
                mouseWheel /= 10;

            bool largeIncrement = InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier);
            if(largeIncrement)
                mouseWheel *= 10;

            if(ImGuizmo.IsOver(ImGuizmoOperation.RotateX))
            {
                matrix = Matrix4x4.CreateRotationX(mouseWheel) * matrix;
                return true;
            }
            else if(ImGuizmo.IsOver(ImGuizmoOperation.RotateY))
            {
                matrix = Matrix4x4.CreateRotationY(mouseWheel) * matrix;
                return true;
            }
            else if(ImGuizmo.IsOver(ImGuizmoOperation.RotateZ))
            {
                matrix = Matrix4x4.CreateRotationZ(mouseWheel) * matrix;
                return true;
            }
        }

        return false;
    }
}
