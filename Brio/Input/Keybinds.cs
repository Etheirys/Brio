using Brio.Config;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Input;

internal static class Keybinds
{
    private static string[] virtualKeyNames;
    private static List<VirtualKey> virtualKeys;

    static Keybinds()
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

    public static bool KeySelector(string label, ref KeyBind keyBind)
    {
        bool changed = false;

        // Test
        ImGui.BeginDisabled();
        ImGui.RadioButton($"##{label}_Test", keyBind.IsDown());
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Test your keys");
        }
        ImGui.EndDisabled();

        // Control
        ImGui.SameLine();

        if(keyBind.Key == VirtualKey.CONTROL)
            ImGui.BeginDisabled();

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

        if(keyBind.Key == VirtualKey.CONTROL)
            ImGui.EndDisabled();

        // Alt
        ImGui.SameLine();

        if(keyBind.Key == VirtualKey.MENU)
            ImGui.BeginDisabled();

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

        if(keyBind.Key == VirtualKey.MENU)
            ImGui.EndDisabled();

        // Shift
        ImGui.SameLine();

        if(keyBind.Key == VirtualKey.SHIFT)
            ImGui.BeginDisabled();

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

        if(keyBind.Key == VirtualKey.SHIFT)
            ImGui.EndDisabled();

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

internal class KeyBind
{
    public VirtualKey Key { get; set; }
    public bool Control { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }

    public KeyBind(VirtualKey key, bool control = false, bool alt = false, bool shift = false)
    {
        Key = key;
        Control = control;
        Alt = alt;
        Shift = shift;
    }

    public bool IsDown()
    {
        if(Key == VirtualKey.NO_KEY)
            return false;

        bool down = InputService.IsKeyDown(Key);

        if(Key != VirtualKey.CONTROL)
            down &= InputService.IsKeyDown(VirtualKey.CONTROL) == Control;

        if(Key != VirtualKey.MENU)
            down &= InputService.IsKeyDown(VirtualKey.MENU) == Alt;

        if(Key != VirtualKey.SHIFT)
            down &= InputService.IsKeyDown(VirtualKey.SHIFT) == Shift;

        return down;
    }

    public override string ToString()
    {
        if(!Control && !Alt && !Shift)
            return Key.GetFancyName();

        string str = string.Empty;

        if(Control)
            str += "Ctrl ";

        if(Alt)
            str += "Alt ";

        if(Shift)
            str += "Shift ";

        str += $"+ {Key.GetFancyName()}";
        return str;
    }
}
