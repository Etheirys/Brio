using Brio.Config;
using Brio.UI.Controls.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using Brio.Input;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    private static readonly HashSet<uint> expanded = [];

    public static (bool anyActive, bool didChange) DragFloat3(string label, ref Vector3 vectorValue, float step = 1.0f, string icon = "", string tooltip = "")
    {
        bool changed = false;
        bool active = false;


        if(string.IsNullOrEmpty(icon))
        {
            ImGui.Text(label);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
            using(ImRaii.PushFont(UiBuilder.IconFont))
                ImGui.Button(icon, new(20, 0));
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }

        uint id = ImGui.GetID(label);
        bool isExpanded = expanded.Contains(id);

        if(isExpanded)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.BeginDisabled();
        }

        //

        (var changedf3, var activef3) = DragFloat3Horizontal($"###{id}_drag3", ref vectorValue);
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

            ImGui.Separator();
        }
        return (active, changed);
    }

    public static (bool anyActive, bool didChange) DragFloat3Horizontal(string label, ref Vector3 value, float step = 0.1f)
    {
        bool changed = false;
        bool active = false;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier)) 
            step /= 10;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier))
            step *= 10;

        var itemSpacing = (ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - 32 - 22) / 3;
        ImGui.SetNextItemWidth(itemSpacing);

        changed |= ImGui.DragFloat($"##{label}_X", ref value.X, step / 10);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("X");
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(itemSpacing);

        changed |= ImGui.DragFloat($"##{label}_Y", ref value.Y, step / 10);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Y");
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(itemSpacing);

        changed |= ImGui.DragFloat($"##{label}_Z", ref value.Z, step / 10);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Z");
        active |= ImGui.IsItemActive();

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

        float buttonWidth = ImGui.GetCursorPosX();
        if(ImGui.ArrowButton($"##{label}_decrease", ImGuiDir.Left))
        {
            value -= step;
            changed = true;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"Decrease {tooltip}");

        ImGui.SameLine();

        buttonWidth = ImGui.GetCursorPosX() - buttonWidth;

        bool hasLabel = !label.StartsWith("##");

        if(hasLabel)
        {
            ImGui.SetNextItemWidth((ImGui.GetWindowWidth() * 0.65f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X);
        }
        else
        {
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - ((buttonWidth * 2) + ImGui.GetStyle().CellPadding.X) - (ImGui.GetStyle().WindowPadding.X * 2));
        }


        changed |= ImGui.DragFloat($"##{label}_drag", ref value, step / 10.0f);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"{tooltip}");
        active |= ImGui.IsItemActive();


        ImGui.SameLine();
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


