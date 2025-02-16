using Brio.Config;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Collections.Generic;

namespace Brio.Input;

public static class KeybindEditor
{
    private static string[] virtualKeyNames;
    private static List<VirtualKey> virtualKeys;

    static KeybindEditor()
    {
        List<string> names = new();
        List<VirtualKey> keys = new();

        names.Add("None");
        keys.Add(VirtualKey.NO_KEY);

        foreach(var vk in InputService.GetValidKeys())
        {
            if(vk > VirtualKey.NO_KEY && vk <= VirtualKey.XBUTTON2)
                continue;

            if(vk >= VirtualKey.KANA && vk <= VirtualKey.MODECHANGE)
                continue;

            if(vk >= VirtualKey.LWIN && vk <= VirtualKey.SLEEP)
                continue;

            if(vk >= VirtualKey.SCROLL)
                continue;


            if(vk == VirtualKey.HELP
                || vk == VirtualKey.EXECUTE
                || vk == VirtualKey.PRINT)
                continue;

            names.Add(vk.GetFancyName());
            keys.Add(vk);
        }

        virtualKeyNames = names.ToArray();
        virtualKeys = keys;
    }

    public static bool KeySelector(string label, KeyBindEvents evt, InputConfiguration config)
    {
        if(!config.Bindings.ContainsKey(evt))
            config.Bindings.Add(evt, new());

        KeyBind bind = config.Bindings[evt];
        return KeySelector(label, ref bind);
    }

    public static bool KeySelector(string label, ref KeyBind keyBind)
    {
        bool changed = false;

        // Control
        using(ImRaii.Disabled(keyBind.Key == VirtualKey.CONTROL))
        {
            bool control = keyBind.Control;
            if(ImGui.Checkbox($"##{label}_Control", ref control))
            {
                keyBind.Control = control;
                changed = true;
            }

            if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Control");
            }
        }

        // Alt
        ImGui.SameLine();

        using(ImRaii.Disabled(keyBind.Key == VirtualKey.MENU))
        {
            bool alt = keyBind.Alt;
            if(ImGui.Checkbox($"##{label}_Alt", ref alt))
            {
                keyBind.Alt = alt;
                changed = true;
            }

            if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Alt");
            }
        }

        // Shift
        ImGui.SameLine();
        using(ImRaii.Disabled(keyBind.Key == VirtualKey.SHIFT))
        {
            bool shift = keyBind.Shift;
            if(ImGui.Checkbox($"##{label}_Shift", ref shift))
            {
                keyBind.Shift = shift;
                changed = true;
            }

            if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Shift");
            }
        }

        // Key
        ImGui.SameLine();
        int currentIndex = virtualKeys.IndexOf(keyBind.Key);
        ImGui.SetNextItemWidth(100);
        if(ImGui.Combo(label, ref currentIndex, virtualKeyNames, virtualKeyNames.Length))
        {
            keyBind.Key = virtualKeys[currentIndex];
            changed = true;
        }

        return changed;
    }
}
