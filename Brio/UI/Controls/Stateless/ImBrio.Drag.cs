using Brio.Config;
using Brio.UI.Controls.Core;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Brio.Input;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    static HashSet<uint> expanded = new();

    public static bool DragFloat3(string label, ref Vector3 vectorValue, float step = 1.0f, string tooltip = "")
    {
        bool changed = false;

        float labelWidth = 0;
        if(!label.StartsWith("##"))
        {
            if(label.Length < 3)
            {
                labelWidth = 22;
                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
                using(ImRaii.PushFont(UiBuilder.IconFont))
                    ImGui.Button(label, new(labelWidth, 0));
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();
                ImGui.SameLine();
            }
            else
            {
                ImGui.Text(label);
            }
        }

        uint id = ImGui.GetID(label);
        bool isExpanded = expanded.Contains(id);

        if(isExpanded)
        {
            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.BeginDisabled();
        }

        ImGui.SetNextItemWidth((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - 32 - labelWidth);
        changed |= ImGui.DragFloat3($"##{label}_drag3", ref vectorValue, step / 10.0f);

        if(isExpanded)
        {
            ImGui.EndDisabled();
            ImGui.PopStyleColor();
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(32);
        if(ImGui.ArrowButton($"##{label}_decrease", isExpanded ? ImGuiDir.Up : ImGuiDir.Down))
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
            changed |= ImBrio.DragFloat($"###{label}_x", ref x, step, $"{tooltip} X");
            vectorValue.X = x;
           
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.FrameBg, UIConstants.GizmoGreen);

            float y = vectorValue.Y;
            changed |= ImBrio.DragFloat($"###{label}_y", ref y, step, $"{tooltip} Y");
            vectorValue.Y = y;
           
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.FrameBg, UIConstants.GizmoRed);

            float z = vectorValue.Z;
            changed |= ImBrio.DragFloat($"###{label}_z", ref z, step, $"{tooltip} Z");
            vectorValue.Z = z;

            ImGui.PopStyleColor();
         
            ImGui.Separator();
        }


        return changed;
    }

    public static bool DragFloat(string label, ref float value, float step = 0.1f, string tooltip = "")
    {
        bool changed = false;

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

        return changed;
    }
}


