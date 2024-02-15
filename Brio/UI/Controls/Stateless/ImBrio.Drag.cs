using Brio.Config;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    static HashSet<uint> expanded = new();

    public static bool DragFloat3(string label, ref Vector3 value, float step = 1.0f, string tooltip = "")
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
       
        ImGui.SetNextItemWidth((ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X) - 32 - labelWidth);
        changed |= ImGui.DragFloat3($"##{label}_drag3", ref value, step / 10.0f);

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        uint id = ImGui.GetID(label);
        bool isExpanded = expanded.Contains(id);

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
            float x = value.X;
            changed |= ImBrio.DragFloat($"###{label}_x", ref x, step, $"{tooltip} X");
            value.X = x;

            float y = value.Y;
            changed |= ImBrio.DragFloat($"###{label}_y", ref y, step, $"{tooltip} Y");
            value.Y = y;

            float z = value.Z;
            changed |= ImBrio.DragFloat($"###{label}_z", ref z, step, $"{tooltip} Z");
            value.Z = z;
        }

        return changed;
    }

    public static bool DragFloat(string label, ref float value, float step = 0.1f, string tooltip = "")
    {
        bool changed = false;

        bool smallIncrement = ImGui.IsKeyDown(ConfigurationService.Instance.Configuration.Interface.IncrementSmall);
        if(smallIncrement)
            step /= 10;

        bool largeIncrement = ImGui.IsKeyDown(ConfigurationService.Instance.Configuration.Interface.IncrementLarge);
        if(largeIncrement)
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


