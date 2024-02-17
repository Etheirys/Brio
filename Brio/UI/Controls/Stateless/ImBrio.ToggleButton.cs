using Brio.UI.Windows;
using ImGuiNET;
using System;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.ConfigModule;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static bool ToggleButton(string label, ref bool selected, bool canDeselect = true)
    {
        return ToggleButton(label, new(0, 0), ref selected, canDeselect);
    }

    public static bool ToggleButton(string label, Vector2 size, ref bool selected, bool canDeselect = true)
    {
        if(!canDeselect && selected)
            ImGui.BeginDisabled();

        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(selected ? ImGuiCol.TabActive : ImGuiCol.Button));

        bool clicked = false;
        if(ImGui.Button(label, size))
        {
            selected = !selected;
            clicked = true;
        }

        ImGui.PopStyleColor();

        if(!canDeselect && selected)
            ImGui.EndDisabled();

        return clicked;
    }

    public static bool ToggleButtonStrip(string id, Vector2 size, ref int selected, string[] options)
    {
        bool changed = false;
        float buttonWidth = (size.X / options.Length);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Tab));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);
        if(ImGui.BeginChild(id, size))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            for(int i = 0; i < options.Length; i++)
            {
                if(i > 0)
                    ImGui.SameLine();

                bool val = i == selected;
                bool clicked = ImBrio.ToggleButton($"{options[i]}##{id}", new(buttonWidth, size.Y), ref val, false);

                if(val && i != selected)
                {
                    selected = i;
                    changed = true;
                }
            }

            ImGui.PopStyleVar();
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
        return changed;
    }
}
