using ImGuiNET;

namespace Brio.UI.Components.Debug;

public static class DebugTab
{
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

        if(ImGui.CollapsingHeader("Addresses"))
        {
            DebugAddressControls.Draw();
        }

        if(ImGui.CollapsingHeader("Sandbox"))
        {
            DebugSandboxControls.Draw();
        }
    }
}
