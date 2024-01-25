using ImGuiNET;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    public static float GetRemainingWidth()
    {
        return ImGui.GetContentRegionAvail().X;
    }

    public static float GetRemainingHeight()
    {
        return ImGui.GetContentRegionAvail().Y;
    }

    public static float GetLineHeight()
    {
        return ImGui.GetTextLineHeight() + (ImGui.GetStyle().FramePadding.Y * 2);
    }
}
