using Brio.Game.GPose;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;

namespace Brio.UI.Components;

public static class DebugTab
{
    public static void Draw()
    {
        DrawGPoseMode();
    }

    private unsafe static void DrawGPoseMode()
    {
        if(ImGui.CollapsingHeader("GPose Mode"))
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
}
