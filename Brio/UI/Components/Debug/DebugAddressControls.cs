using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ImGuiNET;
using static Brio.Game.World.WeatherService;
using System;
using System.Runtime.InteropServices;

namespace Brio.UI.Components.Debug;
public static class DebugAddressControls
{
    public unsafe static void Draw()
    {
        if(ImGui.Button("Copy Layout World Address"))
        {
            var addr = (nint)LayoutWorld.Instance();
            ImGui.SetClipboardText(addr.ToString("X"));
        }

        if(ImGui.Button("Copy Layout Manager Address"))
        {
            var addr = (nint)LayoutWorld.Instance()->ActiveLayout;
            ImGui.SetClipboardText(addr.ToString("X"));
        }

        if(ImGui.Button("Copy Game Main Address"))
        {
            var addr = (nint)GameMain.Instance();
            ImGui.SetClipboardText(addr.ToString("X"));
        }

        if(ImGui.Button("Copy EnvManager Address"))
        {
            IntPtr rawWeather = Dalamud.SigScanner.GetStaticAddressFromSig("4C 8B 05 ?? ?? ?? ?? 41 8B 80 ?? ?? ?? ?? C1 E8 02");
            IntPtr weatherSystem = Marshal.ReadIntPtr(rawWeather);  
            ImGui.SetClipboardText(weatherSystem.ToString("X"));
        }
    }
}
