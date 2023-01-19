using Brio.Game.GPose;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;

namespace Brio.UI.Components;

public static class DebugTabControls
{
    public static void Draw()
    {
        DrawGPoseMode();
    }

    private unsafe static void DrawGPoseMode()
    {
        if (ImGui.CollapsingHeader("GPose Mode"))
        {
            var isFakeGPose = GPoseService.Instance.FakeGPose;
            var originalIsFake = isFakeGPose;
            ImGui.Checkbox("Fake GPose?", ref isFakeGPose);
            if (isFakeGPose != originalIsFake) GPoseService.Instance.FakeGPose = isFakeGPose;

            ImGui.Spacing();

            if (ImGui.Button("Enter GPose"))
            {
                var uiModule = Framework.Instance()->GetUiModule();
                ((delegate* unmanaged<UIModule*, bool>)uiModule->vfunc[75])(uiModule);
            }

            ImGui.SameLine();

            if (ImGui.Button("Exit GPose"))
            {
                var uiModule = Framework.Instance()->GetUiModule();
                ((delegate* unmanaged<UIModule*, void>)uiModule->vfunc[76])(uiModule);
            }
        }
    }
}
