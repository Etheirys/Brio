using Brio.Config;
using Brio.Core;
using Brio.Input;
using ImGuiNET;
using System;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    public static bool SliderFloat(string label, ref float value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f)
    {
        return SliderBase(label, ref value, min, max, format, flags, step, false);
    }

    public static bool SliderAngle(string label, ref float value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f)
    {
        return SliderBase(label, ref value, min, max, format, flags, step, true);
    }

    private static bool SliderBase(string label, ref float value, float min, float max, string format, ImGuiSliderFlags flags, float step, bool isAngle = false)
    {
        bool changed = false;

        if(max < min)
            step = -step;

        var smallIncrement = InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier);
        if(smallIncrement)
            step /= 10;

        bool largeIncrement = InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier);
        if(largeIncrement)
            step *= 10;

        float buttonWidth = ImGui.GetCursorPosX();
        if(ImGui.ArrowButton($"##{label}_decrease", ImGuiDir.Left))
        {
            value -= isAngle ? step * MathHelpers.DegreesToRadians : step;
            changed = true;
        }
        ImGui.SameLine();

        buttonWidth = ImGui.GetCursorPosX() - buttonWidth;

        ImGui.SetNextItemWidth((ImGui.GetWindowWidth() * 0.65f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X);

        if(isAngle)
        {
            changed |= ImGui.SliderAngle($"##{label}_slider", ref value, min, max, format, flags);
        }
        else
        {
            changed |= ImGui.SliderFloat($"##{label}_slider", ref value, min, max, format, flags);
        }

        ImGui.SameLine();
        if(ImGui.ArrowButton($"##{label}_increase", ImGuiDir.Right))
        {
            value += isAngle ? step * MathHelpers.DegreesToRadians : step;
            changed = true;
        }

        ImGui.SameLine();
        ImGui.Text(label);

        return changed;
    }
}
