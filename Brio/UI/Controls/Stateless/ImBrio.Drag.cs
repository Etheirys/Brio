using Brio.Input;
using Brio.UI.Controls.Core;
using Dalamud.Interface;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    private static readonly HashSet<uint> expanded = [];

    public static (bool anyActive, bool didChange) DragFloat3(string label, ref Vector3 vectorValue, float step = 1.0f, FontAwesomeIcon icon = FontAwesomeIcon.None, string tooltip = "")
    {
        bool changed = false;
        bool active = false;


        if(icon == FontAwesomeIcon.None)
        {
            ImGui.Text(label);
        }
        else
        {
            ImBrio.Icon(icon);
            ImGui.SameLine();
        }

        uint id = ImGui.GetID(label);
        bool isExpanded = expanded.Contains(id);

        if(isExpanded)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.BeginDisabled();
        }

        Vector2 size = new Vector2(0, 0);
        size.X = (ImBrio.GetRemainingWidth() - 32) + ImGui.GetStyle().ItemSpacing.X;

        (var changedf3, var activef3) = DragFloat3Horizontal($"###{id}_drag3", ref vectorValue, step, size);
        changed |= changedf3;
        active |= activef3;

        if(isExpanded)
        {
            ImGui.EndDisabled();
            ImGui.PopStyleColor();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(32);
        if(ImGui.ArrowButton($"###{label}_decrease", isExpanded ? ImGuiDir.Up : ImGuiDir.Down))
        {
            if(isExpanded)
            {
                expanded.Remove(id);
            }
            else
            {
                expanded.Add(id);
            }
        }

        if(isExpanded)
        {
            float height = (ImBrio.GetLineHeight() * 3) + (ImGui.GetStyle().ItemSpacing.Y * 2) + (ImGui.GetStyle().WindowPadding.Y * 2);
            if(ImGui.BeginChild($"###{label}_child", new Vector2(0, height), true))
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
                ImGui.EndChild();
            }
        }
        return (active, changed);
    }

    public static (bool anyActive, bool didChange) DragFloat3Horizontal(string label, ref Vector3 value, float step, Vector2 size)
    {
        bool changed = false;
        bool active = false;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier))
            step *= 10;

        if(size.X <= 0)
            size.X = ImBrio.GetRemainingWidth();

        float entryWidth = (size.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 3;
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_X", ref value.X, step / 10);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("X");
        active |= ImGui.IsItemActive();

        if (ImGui.IsItemHovered())
        {
            float mouseWheel = ImGui.GetIO().MouseWheel / 100;
            if (mouseWheel != 0) 
            {
                value.X += mouseWheel * step;
                changed = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_Y", ref value.Y, step / 10);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Y");
        active |= ImGui.IsItemActive();

        if (ImGui.IsItemHovered())
        {
            float mouseWheel = ImGui.GetIO().MouseWheel / 100;
            if (mouseWheel != 0) 
            {
                value.Y += mouseWheel * step;
                changed = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_Z", ref value.Z, step / 10);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Z");
        active |= ImGui.IsItemActive();

        if (ImGui.IsItemHovered())
        {
            float mouseWheel = ImGui.GetIO().MouseWheel / 100;
            if (mouseWheel != 0) 
            {
                value.Z += mouseWheel * step;
                changed = true;
            }
        }

        ImGui.SameLine();

        return (active, changed);
    }

    public static (bool anyActive, bool didChange) DragFloat(string label, ref float value, float step = 0.1f, string tooltip = "")
    {
        bool changed = false;
        bool active = false;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier))
            step *= 10;

        float buttonWidth = 32;
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.ArrowButton($"##{label}_decrease", ImGuiDir.Left))
        {
            value -= step;
            changed = true;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"Decrease {tooltip}");

        ImGui.SameLine();

        bool hasLabel = !label.StartsWith("##");

        if(hasLabel)
        {
            ImGui.SetNextItemWidth((ImGui.GetWindowWidth() * 0.65f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X);
        }
        else
        {
            ImGui.SetNextItemWidth((ImBrio.GetRemainingWidth() - buttonWidth) + ImGui.GetStyle().ItemSpacing.X);
        }


        changed |= ImGui.DragFloat($"##{label}_drag", ref value, step / 10.0f);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"{tooltip}");
        active |= ImGui.IsItemActive();

        if (ImGui.IsItemHovered())
        {
            float mouseWheel = ImGui.GetIO().MouseWheel / 100;
            if (mouseWheel != 0) 
            {
                value += mouseWheel * step;
                changed = true;
            }
        }


        ImGui.SameLine();
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.ArrowButton($"##{label}_increase", ImGuiDir.Right))
        {
            value += step;
            changed = true;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"Increase {tooltip}");

        if(hasLabel)
        {
            ImGui.SameLine();
            ImGui.Text(label);
        }

        return (active, changed);
    }
}


