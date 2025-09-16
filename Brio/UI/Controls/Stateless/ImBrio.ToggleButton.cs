using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    // Patent pending ToggleLock! (this is a 5AM joke, I need sleep)
    public static (bool, bool) ToggleLock(string label, float size, ref bool selected, ref bool locked, bool canSelect = true, bool disableOnLock = false)
    {
        bool clicked = false;
        bool lockClick = false;

        using(ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Tab)))
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding))
            {
                using var child = ImRaii.Child($"###{label}_child", new Vector2((size - 2.3f), 25 * ImGuiHelpers.GlobalScale), false, ImGuiWindowFlags.NoScrollbar);

                if(child.Success)
                {
                    using(ImRaii.Disabled(locked && disableOnLock))
                        if(ToggelButton($"{label}###toggleButton", new Vector2(53, 25), selected))
                        {
                            clicked = true;
                            selected = !selected;
                        }

                    ImGui.SameLine();

                    using(ImRaii.Disabled(!selected))
                        if(FontIconButton($"###{label}_lockButton", locked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock, locked ? "Unlock" : "Lock", bordered: false))
                        {
                            lockClick = true;
                            locked = !locked;
                        }
                }
            }
        }

        return (clicked, lockClick);
    }

    public static bool ToggleStripButton(string label, ref bool selected, bool canSelect = false)
    {
        return ToggleStripButton(label, new(0, 0), ref selected, canSelect);
    }

    public static bool ToggleStripButton(string label, Vector2 size, ref bool selected, bool canSelect = true)
    {
        bool clicked = false;

        using(ImRaii.Disabled(canSelect && selected))
        {
            using(ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(selected ? ImGuiCol.TabActive : ImGuiCol.Tab)))
            using(ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0)))
                if(ImGui.Button(label, size))
                {
                    selected = !selected;
                    clicked = true;
                }
        }

        return clicked;
    }

    public static bool ToggleSelecterStrip(string id, Vector2 size, ref bool[] selected, string[] options)
    {
        bool changed = false;
        float buttonWidth = size.X / options.Length  - (options.Length - 2) - 0.5f;

        using(ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Tab)))
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding))
            {
                using var child = ImRaii.Child(id, size, false, ImGuiWindowFlags.NoScrollbar);
                if(child.Success)
                {
                    using(ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(options.Length - 1)))
                    {
                        for(int i = 0; i < selected.Length; i++)
                        {
                            if(i > 0) ImGui.SameLine();

                            changed |= ToggleStripButton($"{options[i]}##{id}_{i}", new(buttonWidth, size.Y), ref selected[i], false);
                        }
                    }
                }
            }
        }
        return changed;
    }

    public static bool ButtonSelectorStrip(string id, Vector2 size, ref int selected, string[] options)
    {
        bool changed = false;
        float buttonWidth = size.X / options.Length;

        using(ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Tab)))
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding))
            {
                using var child = ImRaii.Child(id, size, false, ImGuiWindowFlags.NoScrollbar);
                if(child.Success)
                {
                    using(ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0)))
                    {
                        for(int i = 0; i < options.Length; i++)
                        {
                            if(i > 0)
                                ImGui.SameLine();

                            bool val = i == selected;
                            ToggleStripButton($"{options[i]}##{id}", new(buttonWidth, size.Y), ref val, false);

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

        return changed;
    }
}
