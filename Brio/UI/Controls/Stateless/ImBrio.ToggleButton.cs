using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static bool ToggleButton(string label, ref bool selected, bool canDeselect = true)
    {
        return ToggleButton(label, new(0,0), ref selected, canDeselect);
    }

    public static bool ToggleButton(string label, Vector2 size, ref bool selected, bool canDeselect = true)
    {
        if(canDeselect && selected)
            ImGui.BeginDisabled();

        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(selected ? ImGuiCol.CheckMark : ImGuiCol.Button));

        bool clicked = false;
        if(ImGui.Button(label, size))
        {
            selected = !selected;
            clicked = true;
        }

        ImGui.PopStyleColor();

        if(canDeselect && selected)
            ImGui.EndDisabled();

        return clicked;
    }
}
