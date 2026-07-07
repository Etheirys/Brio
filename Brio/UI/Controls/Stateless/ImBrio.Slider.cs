using Brio.Config;
using Brio.Core;
using Brio.Input;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    public static bool SliderFloat3(string label, ref Vector3 value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f)
    {
        bool changed = false;

        float x = value.X;
        (var xChanged, _) = SliderFloat($"###{label}_x", ref x, min, max, format, flags, step);
        changed |= xChanged;
        value.X = x;

        float y = value.Y;
        (var yChanged, _) = SliderFloat($"###{label}_y", ref y, min, max, format, flags, step);
        changed |= yChanged;
        value.Y = y;

        float z = value.Z;
        (var zChanged, _) = SliderFloat($"###{label}_z", ref z, min, max, format, flags, step);
        changed |= zChanged;
        value.Z = z;

        return changed;
    }

    public static (bool changed, bool active) SliderFloat(string label, ref float value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f, string toolTip = "")
    {
        return SliderBase(label, ref value, min, max, format, flags, step, false, toolTip);
    }

    public static (bool changed, bool active) SliderAngle(string label, ref float value, float min, float max, string format = "%.2f", ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f, string toolTip = "")
    {
        return SliderBase(label, ref value, min, max, format, flags, step, true, toolTip);
    }

    private static (bool changed, bool active) SliderBase(string label, ref float value, float min, float max, string format, ImGuiSliderFlags flags, float step, bool isAngle = false, string toolTip = "")
    {
        bool changed = false;
        bool active = false;
        float buttonWidth = ImGui.GetCursorPosX();

        if(max < min)
            step = -step;

        var smallIncrement = InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier);
        if(smallIncrement)
            step /= 10;

        bool largeIncrement = InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier);
        if(largeIncrement)
            step *= 10;


        ImGui.PushID(label);

        if(FontIconButton(FontAwesomeIcon.ChevronLeft, new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            value -= isAngle ? step * MathHelpers.DegreesToRadians : step;
            changed |= true;
            active = true;
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

        active |= ImGui.IsItemActive();

        if(ImGui.IsItemHovered())
        {
            if(string.IsNullOrEmpty(toolTip) == false)
                ImGui.SetTooltip(toolTip);

            if(!ConfigurationService.Instance.Configuration.InputManager.DisableScrollWheelOnInputs)
            {
                float mouseWheel = ImGui.GetIO().MouseWheel / 10;
                if(mouseWheel != 0)
                {
                    value += isAngle ? mouseWheel * step * MathHelpers.DegreesToRadians : mouseWheel * step;
                    changed |= true;
                    active = true;
                }
            }
        }

        ImGui.SameLine();
        if(FontIconButton(FontAwesomeIcon.ChevronRight, new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            value += isAngle ? step * MathHelpers.DegreesToRadians : step;
            changed |= true;
            active = true;
        }

        if(hasLabel)
        {
            ImGui.SameLine();
            ImGui.Text(label);
        }

        ImGui.PopID();

        return (changed, active);
    }
}
