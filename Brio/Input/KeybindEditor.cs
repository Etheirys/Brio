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

        foreach(var vk in InputManagerService.GetValidKeys())
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

    public static bool KeySelector(string label, InputAction evt, InputManagerConfiguration config)
    {
        if(!config.KeyBindings.ContainsKey(evt))
            config.KeyBindings.Add(evt, new KeyConfig(VirtualKey.NO_KEY));

        return KeySelector(label, ref evt, config);
    }

    public static bool KeySelector(string label, ref InputAction evt, InputManagerConfiguration config)
    {
        bool changed = false;
        var bind = config.KeyBindings[evt];
        ref var keyBind = ref bind;

        // Control
        using(ImRaii.Disabled(keyBind.isCtrl))
        {
            bool control = keyBind.requireCtrl;
            if(ImGui.Checkbox($"##{label}_Control", ref control))
            {
                keyBind.requireCtrl = control;
                changed = true;
            }

            if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Control");
            }
        }

        // Alt
        ImGui.SameLine();

        using(ImRaii.Disabled(keyBind.isAlt))
        {
            bool alt = keyBind.requireAlt;
            if(ImGui.Checkbox($"##{label}_Alt", ref alt))
            {
                keyBind.requireAlt = alt;
                changed = true;
            }

            if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Alt");
            }
        }

        // Shift
        ImGui.SameLine();
        using(ImRaii.Disabled(keyBind.isShift))
        {
            bool shift = keyBind.requireShift;
            if(ImGui.Checkbox($"##{label}_Shift", ref shift))
            {
                keyBind.requireShift = shift;
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
            config.ResetKeyToDefault(evt);
        }
        
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Reset Key to Default");
        }

        // Key
        ImGui.SameLine();
        int currentIndex = virtualKeys.IndexOf(keyBind.key);
        ImGui.SetNextItemWidth(100);
        if(ImGui.Combo(label, ref currentIndex, virtualKeyNames, virtualKeyNames.Length))
        {
            keyBind.key = virtualKeys[currentIndex];
            changed = true;
        }

        return changed;
    }
}
