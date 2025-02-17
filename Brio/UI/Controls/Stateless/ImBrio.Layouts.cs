using ImGuiNET;

namespace Brio.UI.Controls.Stateless;
public static partial class ImBrio
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

    public static void RightAlign(float width, int numItems)
    {
        RightAlign((width * numItems) + (ImGui.GetStyle().ItemSpacing.X * (numItems - 1)));
    }

    public static void RightAlign(float width)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (GetRemainingWidth() - width));
    }
}
