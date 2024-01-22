using ImGuiNET;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    public static void TextCentered(string text, float width)
    {
        float textWidth = ImGui.CalcTextSize(text).X;
        float indent = (width - textWidth) * 0.5f;

        if(indent <= 0)
            indent = 0;

        float x = ImGui.GetCursorPosX() + indent;
        ImGui.SetCursorPosX(x);
        ImGui.TextWrapped(text);
    }
}
