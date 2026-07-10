using Brio.Config;
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
    private const float BarWidth = 3f;

    private static readonly HashSet<string> expanded = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat3(string label, ref Vector3 value, float step = 1.0f, FontAwesomeIcon icon = FontAwesomeIcon.None, string tooltip = "", bool enableExpanded = false)
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
                    if(Button($"{label}##Button", icon, new Vector2(25 * ImGuiHelpers.GlobalScale), tooltip: tooltip))
                    {
                        if(isExpanded)
                            expanded.Remove(label);
                        else
                            expanded.Add(label);
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

        (bool changed, bool active) = DragFloat3Implementation($"###{label}_drag3", ref value, step, size, toolTip: tooltip);

        if(isExpanded && enableExpanded)
        {
            ImGui.PushStyleColor(ImGuiCol.Border, UIConstants.GizmoRed);

            float x = value.X;
            (var pdidChange, var panyActive) = DragFloat($"###{label}_x", ref x, step, $"{tooltip} X", UIConstants.GizmoRed);
            value.X = x;

            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Border, UIConstants.GizmoGreen);

            float y = value.Y;
            (var rdidChange, var ranyActive) = DragFloat($"###{label}_y", ref y, step, $"{tooltip} Y", UIConstants.GizmoGreen);
            value.Y = y;

            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Border, UIConstants.GizmoBlue);

            float z = value.Z;
            (var sdidChange, var sanyActive) = DragFloat($"###{label}_z", ref z, step, $"{tooltip} Z", UIConstants.GizmoBlue);
            value.Z = z;

            changed |= pdidChange |= rdidChange |= sdidChange;
            active |= panyActive |= ranyActive |= sanyActive;

            ImGui.PopStyleColor();
        }

        return (active, changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat3Implementation(string label, ref Vector3 value, float step, Vector2 size = default, string? toolTip = null)
    {
        if(size == Vector2.Zero)
        {
            size = new Vector2(GetRemainingWidth() + ImGui.GetStyle().ItemSpacing.X, 0);
        }

        bool changed = false;
        bool active = false;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier))
            step *= 10;

        if(size.X <= 0)
            size.X = GetRemainingWidth();

        float entryWidth = ((size.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        using(ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
            changed |= Scrub($"##{label}_X", ref value.X, UIConstants.GizmoRed, entryWidth, step / 10);

        if(ImGui.IsItemHovered())
        {
            AttachToolTip($" X {toolTip ?? ""}");
            if(!ConfigurationService.Instance.Configuration.InputManager.DisableScrollWheelOnInputs)
            {
                float mouseWheel = ImGui.GetIO().MouseWheel / 10;
                if(mouseWheel != 0)
                {
                    value.X += mouseWheel * step;
                    changed = true;
                }
            }
        }
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        using(ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
            changed |= Scrub($"##{label}_Y", ref value.Y, UIConstants.GizmoGreen, entryWidth, step / 10, "0.00");

        if(ImGui.IsItemHovered())
        {
            AttachToolTip($" Y {toolTip ?? ""}");
            if(!ConfigurationService.Instance.Configuration.InputManager.DisableScrollWheelOnInputs)
            {
                float mouseWheel = ImGui.GetIO().MouseWheel / 10;
                if(mouseWheel != 0)
                {
                    value.Y += mouseWheel * step;
                    changed = true;
                }
            }
        }
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        using(ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
            changed |= Scrub($"##{label}_Z", ref value.Z, UIConstants.GizmoBlue, entryWidth, step / 10, "0.00");

        if(ImGui.IsItemHovered())
        {
            AttachToolTip($" Z {toolTip ?? ""}");
            if(!ConfigurationService.Instance.Configuration.InputManager.DisableScrollWheelOnInputs)
            {
                float mouseWheel = ImGui.GetIO().MouseWheel / 10;
                if(mouseWheel != 0)
                {
                    value.Z += mouseWheel * step;
                    changed = true;
                }
            }
        }
        active |= ImGui.IsItemActive();

        return (active, changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (bool anyActive, bool didChange) DragFloat(string label, ref float value, float step = 0.1f, string tooltip = "", uint color = UIConstants.SlightGrey)
    {
        bool changed = false;
        bool active = false;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier))
            step *= 10;

        float buttonWidth = 32;
        ImGui.SetNextItemWidth(buttonWidth);
        if(FontIconButton(FontAwesomeIcon.ChevronLeft, new Vector2(25 * ImGuiHelpers.GlobalScale)))
        {
            value -= step;
            changed |= true;
        }

        AttachToolTip($"Decrease {tooltip}");

        ImGui.SameLine();

        bool hasLabel = !label.StartsWith("##");

        float width;
        if(hasLabel)
            width = (ImGui.GetWindowWidth() * 0.65f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X;
        else
            width = GetRemainingWidth() - buttonWidth + ImGui.GetStyle().ItemSpacing.X;

        using(ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        {
            changed |= Scrub($"##{label}_drag", ref value, color, width, step / 10.0f);
            active |= ImGui.IsItemActive();
        }

        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"{tooltip}");
            if(!ConfigurationService.Instance.Configuration.InputManager.DisableScrollWheelOnInputs)
            {
                float mouseWheel = ImGui.GetIO().MouseWheel / 10;
                if(mouseWheel != 0)
                {
                    value += mouseWheel * step;
                    changed = true;
                }
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(buttonWidth);
        if(FontIconButton(FontAwesomeIcon.ChevronRight, new Vector2(25 * ImGuiHelpers.GlobalScale)))
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
    public static (bool anyActive, bool didChange) DragFloat2V3(string label, ref Vector3 value, float min, float max, bool degrees = false, float step = 1.0f, Vector2 size = default, string tooltip = "")
    {
        if(size == Vector2.Zero)
        {
            size = new Vector2(GetRemainingWidth() + ImGui.GetStyle().ItemSpacing.X, 0);
        }

        bool changed = false;
        bool active = false;

        Vector2 vectorBuffer = degrees
            ? new Vector2(BrioUtilities.RadiansToDegrees(-value.X), BrioUtilities.RadiansToDegrees(-value.Y))
            : new Vector2(value.X, value.Y);

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputManagerService.ActionKeysPressed(InputAction.Interface_IncrementLargeModifier))
            step *= 10;

        if(size.X <= 0)
            size.X = GetRemainingWidth();

        float entryWidth = (size.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 2;

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        using(ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
            changed |= Scrub($"##{label}_X", ref vectorBuffer.X, UIConstants.GizmoRed, entryWidth, step / 10);

        if(ImGui.IsItemHovered())
        {
            AttachToolTip($" X {tooltip}");
            if(!ConfigurationService.Instance.Configuration.InputManager.DisableScrollWheelOnInputs)
            {
                float mouseWheel = ImGui.GetIO().MouseWheel / 10;
                if(mouseWheel != 0)
                {
                    vectorBuffer.X += mouseWheel * step;
                    changed = true;
                }
            }
        }
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        using(ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
            changed |= Scrub($"##{label}_Y", ref vectorBuffer.Y, UIConstants.GizmoGreen, entryWidth, step / 10, "0.00");

        if(ImGui.IsItemHovered())
        {
            AttachToolTip($" Y {tooltip}");
            if(!ConfigurationService.Instance.Configuration.InputManager.DisableScrollWheelOnInputs)
            {
                float mouseWheel = ImGui.GetIO().MouseWheel / 10;
                if(mouseWheel != 0)
                {
                    vectorBuffer.Y += mouseWheel * step;
                    changed = true;
                }
            }
        }
        active |= ImGui.IsItemActive();

        if(changed)
        {
            if(degrees)
            {
                value.X = BrioUtilities.DegreesToRadians(-vectorBuffer.X);
                value.Y = BrioUtilities.DegreesToRadians(-vectorBuffer.Y);
            }
            else
            {
                value.X = vectorBuffer.X;
                value.Y = vectorBuffer.Y;
            }
        }

        return (active, changed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Scrub(string id, ref float value, uint color, float width, float speed, string format = "0.00", float min = 0f, float max = 0f)
    {
        Vector2 startPOS = ImGui.GetCursorScreenPos();
        float height = ImGui.GetFrameHeight();
        float rounding = ImGui.GetStyle().FrameRounding;

        ImGui.GetWindowDrawList().AddRectFilled(startPOS, startPOS + new Vector2(BarWidth + rounding, height), color, rounding, ImDrawFlags.RoundCornersLeft);
        ImGui.SetCursorScreenPos(startPOS + new Vector2(BarWidth, 0));
        ImGui.SetNextItemWidth(width - BarWidth);

        bool changed = ImGui.DragFloat(id, ref value, speed, min, max);

        return changed;
    }
}


