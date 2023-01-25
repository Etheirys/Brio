using Brio.Game.GPose;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;

namespace Brio.UI.Components.Debug;
public static class DebugGPoseControls
{
    public unsafe static void Draw()
    {
        var isFakeGPose = GPoseService.Instance.FakeGPose;
        var originalIsFake = isFakeGPose;
        ImGui.Checkbox("Fake GPose?", ref isFakeGPose);
        if(isFakeGPose != originalIsFake) GPoseService.Instance.FakeGPose = isFakeGPose;

        ImGui.Spacing();

        if(ImGui.Button("Enter GPose"))
        {
            Framework.Instance()->GetUiModule()->EnterGPose();
        }

        ImGui.SameLine();

        if(ImGui.Button("Exit GPose"))
        {
            Framework.Instance()->GetUiModule()->ExitGPose();
        }
    }
}
