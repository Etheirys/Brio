using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static bool DrawLabeledColor(string id, uint color, string colorText, string description, Vector2? size = null)
    {
        bool wasClicked = false;

        Vector2 minButtonSize = new(ImGui.CalcTextSize("XXX").X + ImGui.GetStyle().FramePadding.X * 2, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);
        Vector2 desiredButtonSize = new(ImGui.CalcTextSize(colorText).X + ImGui.GetStyle().FramePadding.X * 2, ImGui.CalcTextSize(colorText).Y + ImGui.GetStyle().FramePadding.Y * 2);
        Vector2 buttonSize = size.HasValue ? size.Value : Vector2.Max(minButtonSize, desiredButtonSize);

        Vector2 textSize = ImGui.CalcTextSize(colorText);
        Vector2 initialPos = ImGui.GetCursorScreenPos();
        Vector2 textPos = initialPos + (buttonSize - textSize) * 0.5f;

        wasClicked |= ImGui.ColorButton($"{description}###{id}", ImGui.ColorConvertU32ToFloat4(color), ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.NoAlpha, buttonSize);

        var textColor = CalculateContrastingTextColor(color);

        ImGui.GetWindowDrawList().AddText(textPos, textColor, colorText);

        return wasClicked;
    }

    public static bool DrawPopupColorSelector(string id, uint[] colors, ref int index, int columns = 8)
    {
        bool wasChanged = false;

        using(var popup = ImRaii.Popup(id))
        {
            if(popup.Success)
            {
                ImGui.SetNextItemWidth(-1);
                wasChanged |= ImGui.InputInt($"##{id}_index", ref index, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue);
                wasChanged |= DrawColorSelector(id, colors, ref index, columns);

                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    ImGui.CloseCurrentPopup();
            }
        }

        return wasChanged;
    }

    public static bool DrawColorSelector(string id, uint[] colors, ref int index, int columns = 8)
    {
        bool wasClicked = false;

        for(int i = 0; i < colors.Length; ++i)
        {
            var color = colors[i];
            if(color == 0)
                continue;

            var thisColorText = i.ToString();

            bool isSelected = i == index;

            if(isSelected)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 4.0f);
                ImGui.PushStyleColor(ImGuiCol.Border, 0xFFFFFFFF);
            }

            if(DrawLabeledColor($"{id}_{i}", color, thisColorText, thisColorText))
            {
                wasClicked = true;
                index = i;
            }

            if(isSelected)
            {
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();
            }

            if((i + 1) % columns != 0)
                ImGui.SameLine();
        }

        return wasClicked;
    }

    public static float CalculateLuminance(uint color)
    {
        float r = ((color & 0xFF0000) >> 16) / 255.0f;
        float g = ((color & 0x00FF00) >> 8) / 255.0f;
        float b = (color & 0x0000FF) / 255.0f;
        float luminance = 0.299f * r + 0.587f * g + 0.114f * b;
        return luminance;
    }

    public static uint CalculateContrastingTextColor(uint backgroundColor)
    {
        float luminance = CalculateLuminance(backgroundColor);
        uint textColor = (luminance > 0.5f) ? 0xFF000000 : 0xFFFFFFFF;
        return textColor;
    }

    public static uint ARGBToABGR(uint argbColor) => ((argbColor >> 24) & 0xFF) | ((argbColor & 0xFF) << 16) | ((argbColor & 0xFF00) & 0xFF00) | ((argbColor >> 16) & 0xFF);
}
