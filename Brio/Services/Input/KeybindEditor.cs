using Brio.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;

namespace Brio.Input;

public static class KeybindEditor
{
    private static readonly string[] virtualKeyNames;
    private static readonly List<VirtualKey> virtualKeys;

    static KeybindEditor()
    {
        List<string> names = [];
        List<VirtualKey> keys = [];

        names.Add("None");
        keys.Add(VirtualKey.NO_KEY);

        foreach(var vk in InputManagerService.GetValidKeys())
        {
            if(vk is > VirtualKey.NO_KEY and <= VirtualKey.XBUTTON2)
                continue;

            if(vk is VirtualKey.KANA && vk <= VirtualKey.MODECHANGE)
                continue;

            if(vk is VirtualKey.LWIN && vk <= VirtualKey.SLEEP)
                continue;

            if(vk is VirtualKey.SCROLL)
                continue;


            if(vk is VirtualKey.HELP
                or VirtualKey.EXECUTE
                or VirtualKey.PRINT)
                continue;

            names.Add(vk.GetFancyName());
            keys.Add(vk);
        }

        virtualKeyNames = [.. names];
        virtualKeys = keys;
    }

    public static bool KeySelector(string label, InputAction evt, InputManagerConfiguration config)
    {
        if(!config.KeyBindings.ContainsKey(evt))
            config.KeyBindings.Add(evt, new KeyConfig(VirtualKey.NO_KEY));

        bool changed = false;
        KeyConfig keyBind = config.KeyBindings[evt];

        // Control
        using(ImRaii.Disabled(keyBind.Key == VirtualKey.CONTROL))
        {
            bool control = keyBind.RequireCtrl;
            if(ImGui.Checkbox($"##{label}_Control", ref control))
            {
                keyBind.RequireCtrl = control;
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
            bool alt = keyBind.RequireAlt;
            if(ImGui.Checkbox($"##{label}_Alt", ref alt))
            {
                keyBind.RequireAlt = alt;
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
            bool shift = keyBind.RequireShift;
            if(ImGui.Checkbox($"##{label}_Shift", ref shift))
            {
                keyBind.RequireShift = shift;
                changed = true;
            }

            if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Shift");
            }
        }

        // Reset to Default Button
        ImGui.SameLine();
        if(ImGui.Button($"Reset##{label}"))
        {
            keyBind = config.GetDefaultKey(evt);
            changed = true;
        }

        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Reset Key to Default");
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

        if(changed)
        {
            config.KeyBindings[evt] = keyBind;
        }

        return changed;
    }
}
