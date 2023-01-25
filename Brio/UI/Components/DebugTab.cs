using Brio.UI.Components.Debug;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System;

namespace Brio.UI.Components;

public static class DebugTab
{
    private static int _input = 0;

    public unsafe static void Draw()
    {
        if(ImGui.CollapsingHeader("GPose Mode"))
        {
            DebugGPoseControls.Draw();
        }

        if(ImGui.CollapsingHeader("IPC"))
        {
            DebugIPCControls.Draw();
        }

        if(ImGui.CollapsingHeader("Events"))
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputInt("Input", ref _input);

            if(ImGui.Button("Apply"))
            {
                var addr2 = Dalamud.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 48 89 44 24 20 48 89 44 24 28 48 89 44 24 30");

                var addr = Dalamud.SigScanner.ScanText("48 89 5c 24 10 57 48 83 ec 20 48 8b d9 8b fa 44 8b da 45 8b d0 8b 54 24 50 41 8b c9");


                //var addr = Dalamud.SigScanner.ScanText("48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 48 83 ec 30 48 83 3d 84 cd c3 01 00");

                var addr3 = Dalamud.SigScanner.ScanText("48 89 5c 24 10 57 48 83 ec 20 48 8b 81 e8 38 00 00 48 8b f9 48 8b 18 48 3b d8 0f 84 f5 00 00 00");

                var gameMain = GameMain.Instance();

                var func = (delegate* unmanaged<IntPtr, uint, uint, uint, uint, void>)addr;

                var func2 = (delegate* unmanaged<IntPtr, void>)addr3;

                func((nint)gameMain, (uint)_input, 0, 0, 0);
                //func2((nint)EventFramework.Instance());


            }
        }

        if(ImGui.CollapsingHeader("Trash"))
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputInt("Input", ref _input);

            if(ImGui.Button("Apply"))
            {
                var addr2 = Dalamud.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 48 89 44 24 20 48 89 44 24 28 48 89 44 24 30");

                var addr = Dalamud.SigScanner.ScanText("48 89 5c 24 10 57 48 83 ec 20 48 8b d9 8b fa 44 8b da 45 8b d0 8b 54 24 50 41 8b c9");


                //var addr = Dalamud.SigScanner.ScanText("48 89 5c 24 08 48 89 6c 24 10 48 89 74 24 18 57 48 83 ec 30 48 83 3d 84 cd c3 01 00");

                var addr3 = Dalamud.SigScanner.ScanText("48 89 5c 24 10 57 48 83 ec 20 48 8b 81 e8 38 00 00 48 8b f9 48 8b 18 48 3b d8 0f 84 f5 00 00 00");

                var gameMain = GameMain.Instance();

                var func = (delegate* unmanaged<IntPtr, uint, uint, uint, uint, void>)addr;

                var func2 = (delegate* unmanaged<IntPtr, void>)addr3;

                func((nint)gameMain, (uint)_input, 0, 0, 0);
                //func2((nint)EventFramework.Instance());


            }
        }
    }
}
