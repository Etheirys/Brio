using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Controls.Stateless;
public static partial class ImBrio
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

    public static void Text(string text, uint color = 0xFFFFFF)
    {
        ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(color), text);
    }

    public static void Icon(FontAwesomeIcon icon)
    {
        // Use a button here since we can control its width, unlike text.
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Button(icon.ToIconString(), new(25, 0));
        }
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
    }
}
