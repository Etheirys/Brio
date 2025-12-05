using Brio.Core;
using Brio.Input;
using Brio.UI.Controls.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    private static readonly HashSet<string> expanded = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat3(string label, ref Vector3 vectorValue, float step = 1.0f,
        FontAwesomeIcon icon = FontAwesomeIcon.None, string tooltip = "", bool enableExpanded = false)
    {
        bool isExpanded = expanded.Contains(label);

        if(icon == FontAwesomeIcon.None)
        {
            ImGui.Text(label);
            if(string.IsNullOrEmpty(tooltip) is false)
                AttachToolTip(tooltip);
        }
        else
        {
            if(enableExpanded)
            {
                using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
                {
                    if(Button($"{label}##Button", icon, new Vector2(25 * ImGuiHelpers.GlobalScale), tooltip: tooltip))
                    {
                        if(isExpanded)
                        {
                            expanded.Remove(label);
                        }
                        else
                        {
                            expanded.Add(label);
                        }
                    }
                }
            }
            else
            {
                Icon(icon);
                if(string.IsNullOrEmpty(tooltip) is false)
                    AttachToolTip(tooltip);
            }
        }

        ImGui.SameLine();

        Vector2 size = new(0, 0)
        {
            X = GetRemainingWidth() + ImGui.GetStyle().ItemSpacing.X
        };

        (bool changed, bool active) = DragFloat3Horizontal($"###{label}_drag3", ref vectorValue, step, size, toolTip: tooltip);

        if(isExpanded && enableExpanded)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, UIConstants.GizmoBlue);

            float x = vectorValue.X;
            (var pdidChange, var panyActive) = DragFloat($"###{label}_x", ref x, step, $"{tooltip} X");
            vectorValue.X = x;

            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.FrameBg, UIConstants.GizmoGreen);

            float y = vectorValue.Y;
            (var rdidChange, var ranyActive) = DragFloat($"###{label}_y", ref y, step, $"{tooltip} Y");
            vectorValue.Y = y;

            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.FrameBg, UIConstants.GizmoRed);

            float z = vectorValue.Z;
            (var sdidChange, var sanyActive) = DragFloat($"###{label}_z", ref z, step, $"{tooltip} Z");
            vectorValue.Z = z;

            changed |= pdidChange |= rdidChange |= sdidChange;
            active |= panyActive |= ranyActive |= sanyActive;

            ImGui.PopStyleColor();
        }

        return (active, changed);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat3Simple(string label, ref Vector3 value, float step)
    {
        Vector2 d3size = new(0, 0)
        {
            X = GetRemainingWidth() + ImGui.GetStyle().ItemSpacing.X
        };

        return DragFloat3Horizontal($"###{label}", ref value, step, d3size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat3Horizontal(string label, ref Vector3 value, float step, Vector2 size, string? toolTip = null)
    {
        bool changed = false;
        bool active = false;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier))
            step *= 10;

        if(size.X <= 0)
            size.X = GetRemainingWidth();

        float entryWidth = (size.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 3;
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_X", ref value.X, step / 10);
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($" X {toolTip ?? ""}");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value.X += mouseWheel * step;
                changed = true;
            }
        }
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_Y", ref value.Y, step / 10);
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($" Y {toolTip ?? ""}");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value.Y += mouseWheel * step;
                changed = true;
            }
        }
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_Z", ref value.Z, step / 10);
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($" Z {toolTip ?? ""}");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value.Z += mouseWheel * step;
                changed = true;
            }
        }
        active |= ImGui.IsItemActive();

        return (active, changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat(string label, ref float value, float step = 0.1f, string tooltip = "")
    {
        bool changed = false;
        bool active = false;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier))
            step *= 10;

        float buttonWidth = 32;
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.Button($"◀ ###{label}_decrease", new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            value -= step;
            changed = true;
        }

        AttachToolTip($"Decrease {tooltip}");

        ImGui.SameLine();

        bool hasLabel = !label.StartsWith("##");

        if(hasLabel)
        {
            ImGui.SetNextItemWidth((ImGui.GetWindowWidth() * 0.65f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X);
        }
        else
        {
            ImGui.SetNextItemWidth(GetRemainingWidth() - buttonWidth + ImGui.GetStyle().ItemSpacing.X);
        }

        changed |= ImGui.DragFloat($"##{label}_drag", ref value, step / 10.0f);
        active |= ImGui.IsItemActive();

        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{tooltip}");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value += mouseWheel * step;
                changed = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.Button($"▶ ###{label}_increase", new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            value += step;
            changed = true;
        }

        AttachToolTip($"Increase {tooltip}");

        if(hasLabel)
        {
            ImGui.SameLine();
            ImGui.Text(label);
        }

        return (active, changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat(string label, ref float value, float min, float max, float step = 0.1f, string tooltip = "", int width = 0)
    {
        bool changed = false;
        bool active = false;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier))
            step *= 10;

        float buttonWidth = 32 * ImGuiHelpers.GlobalScale;
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.Button($"◀ ###{label}_decrease", new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            if(value - step <= min)
            {
                value = min;
            }
            else
            {
                value -= step;
            }
            changed = true;
        }

        AttachToolTip($"Decrease {tooltip}");

        ImGui.SameLine();

        bool hasLabel = !label.StartsWith("##");

        if(hasLabel)
        {
            ImGui.SetNextItemWidth((ImGui.GetWindowWidth() * 0.65f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X);
        }
        else
        {
            ImGui.SetNextItemWidth(GetRemainingWidth() - buttonWidth + ImGui.GetStyle().ItemSpacing.X);
        }

        if(width > 0)
        {
            ImGui.SetNextItemWidth(width);
        }

        if(ImGui.DragFloat($"###{label}_drag", ref value, step / 10.0f, min, max))
        {
            if(value < min)
            {
                value = min;
            }
            else if(value > max)
            {
                value = max;
            }
            changed |= true;
        }
        active |= ImGui.IsItemActive();

        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{tooltip}");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                if(value + (mouseWheel * step) <= min)
                {
                    value = min;
                }
                else if(value + (mouseWheel * step) >= max)
                {
                    value = max;
                }
                else
                {
                    value += mouseWheel * step;
                }
                changed = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.Button($"▶ ###{label}_increase", new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            if(value + step >= max)
            {
                value = max;
            }
            else
            {
                value += step;
            }
            changed = true;
        }

        AttachToolTip($"Increase {tooltip}");

        if(hasLabel)
        {
            ImGui.SameLine();
            ImGui.Text(label);
        }

        return (active, changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool DragFloat2V3(string label, ref Vector3 value, float min, float max, string format, bool degrees = false, ImGuiSliderFlags flags = ImGuiSliderFlags.None, float step = 1.0f)
    {
        Vector2 vector2;
        bool changed = false;

        if(degrees)
        {
            // Convert to degrees
            vector2 = new Vector2(BrioUtilities.RadiansToDegrees(-value.X), BrioUtilities.RadiansToDegrees(-value.Y));
            changed = ImGui.DragFloat2(label, ref vector2, step, min, max, format, flags);
            if(changed)
            {
                value.X = BrioUtilities.DegreesToRadians(-vector2.X);
                value.Y = BrioUtilities.DegreesToRadians(-vector2.Y);
            }
        }
        else
        {
            vector2 = new Vector2(value.X, value.Y);
            changed = ImGui.DragFloat2(label, ref vector2, step, min, max, format, flags);
            if(changed)
            {
                value.X = vector2.X;
                value.Y = vector2.Y;
            }
        }
        return changed;
    }
}


