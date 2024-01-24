using ImGuiNET;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    public static float GetRemainingWidth()
    {
        return (ImGui.GetWindowSize().X - ImGui.GetCursorPosX()) - ImGui.GetStyle().WindowPadding.X;
    }
}
