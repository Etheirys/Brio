using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    public static bool ToggleButton(string label, ref bool selected, bool canDeselect = true)
    {
        return ToggleButton(label, new(0, 0), ref selected, canDeselect);
    }

    public static bool ToggleButton(string label, Vector2 size, ref bool selected, bool canDeselect = true)
    {
        bool clicked = false;

        using(ImRaii.Disabled(canDeselect && selected))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(selected ? ImGuiCol.TabActive : ImGuiCol.Tab));

            if(ImGui.Button(label, size))
            {
                selected = !selected;
                clicked = true;
            }

            ImGui.PopStyleColor();
        }

        return clicked;
    }

    public static bool ToggleButtonStrip(string id, Vector2 size, ref int selected, string[] options)
    {
        bool changed = false;
        float buttonWidth = (size.X / options.Length);

        using(ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Tab)))
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding))
            {
                using(var child = ImRaii.Child(id, size))
                {
                    if(child.Success)
                    {
                        using(ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0)))
                        {
                            for(int i = 0; i < options.Length; i++)
                            {
                                if(i > 0)
                                    ImGui.SameLine();

                                bool val = i == selected;
                                ToggleButton($"{options[i]}##{id}", new(buttonWidth, size.Y), ref val, false);

                                if(val && i != selected)
                                {
                                    selected = i;
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        return changed;
    }
}
