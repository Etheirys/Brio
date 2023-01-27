using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using ImGuiNET;

namespace Brio.UI.Components.Debug;
public static class DebugSandboxControls
{
    public unsafe static void Draw()
    {
       if(ImGui.Button("Copy Layout Address"))
        {
            var addr = (nint) LayoutWorld.Instance();
            ImGui.SetClipboardText(addr.ToString("X"));
        }

        if(ImGui.Button("Copy Game Main Address"))
        {
            var addr = (nint)GameMain.Instance();
            ImGui.SetClipboardText(addr.ToString("X"));
        }

    }
}
