using Brio.Core;
using Brio.Input;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;
public static partial class ImBrio
{
    public static bool SliderFloat3(string label, ref Vector3 value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f)
    {
        bool changed = false;

        float x = value.X;
        changed |= SliderFloat($"###{label}_x", ref x, min, max, format, flags, step);
        value.X = x;

        float y = value.Y;
        changed |= SliderFloat($"###{label}_y", ref y, min, max, format, flags, step);
        value.Y = y;

        float z = value.Z;
        changed |= SliderFloat($"###{label}_z", ref z, min, max, format, flags, step);
        value.Z = z;

        return changed;
    }

    public static bool SliderFloat(string label, ref float value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f, string toolTip = "")
    {
        return SliderBase(label, ref value, min, max, format, flags, step, false, toolTip);
    }

    public static bool SliderAngle(string label, ref float value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f, string toolTip = "")
    {
        return SliderBase(label, ref value, min, max, format, flags, step, true, toolTip);
    }

    private static bool SliderBase(string label, ref float value, float min, float max, string format, ImGuiSliderFlags flags, float step, bool isAngle = false, string toolTip = "")
    {
        bool changed = false;
        float buttonWidth = ImGui.GetCursorPosX();

        if(max < min)
            step = -step;

        var smallIncrement = InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier);
        if(smallIncrement)
            step /= 10;

        bool largeIncrement = InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier);
        if(largeIncrement)
            step *= 10;


        if(ImGui.ArrowButton($"##{label}_decrease", ImGuiDir.Left))
        {
            value -= isAngle ? step * MathHelpers.DegreesToRadians : step;
            changed = true;
        }
        ImGui.SameLine();

        buttonWidth = ImGui.GetCursorPosX() - buttonWidth;

        bool hasLabel = !label.StartsWith("##");

        if(hasLabel)
        {
            ImGui.SetNextItemWidth((GetRemainingWidth() * 0.75f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X);
        }
        else
        {
            ImGui.SetNextItemWidth(GetRemainingWidth() - ((buttonWidth * 2) + ImGui.GetStyle().CellPadding.X) - (ImGui.GetStyle().WindowPadding.X * 2));
        }

        if(isAngle)
            changed |= ImGui.SliderAngle($"##{label}_slider", ref value, min, max, format, flags);
        else
            changed |= ImGui.SliderFloat($"##{label}_slider", ref value, min, max, format, flags);

        if(ImGui.IsItemHovered())
        {
            if(string.IsNullOrEmpty(toolTip) == false)
                ImGui.SetTooltip(toolTip);

            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value += isAngle ? mouseWheel * step * MathHelpers.DegreesToRadians : mouseWheel * step;
                changed = true;
            }
        }

        ImGui.SameLine();
        if(ImGui.ArrowButton($"##{label}_increase", ImGuiDir.Right))
        {
            value += isAngle ? step * MathHelpers.DegreesToRadians : step;
            changed = true;
        }

        if(hasLabel)
        {
            ImGui.SameLine();
            ImGui.Text(label);
        }

        return changed;
    }
}
