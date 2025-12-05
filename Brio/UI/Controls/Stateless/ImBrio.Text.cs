using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using System;
using System.Numerics;

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
        if(icon == FontAwesomeIcon.None) return;

        // Use a button here since we can control its width, unlike text.
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Button(icon.ToIconString(), new(25 * ImGuiHelpers.GlobalScale, 0));
        }
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
        ImGui.PopStyleColor();
    }
}

public class ImBrioText : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;

    public IFontHandle UidFont { get; init; }

    public ImBrioText(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;

        UidFont = _pluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
        {
            e.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new()
            {
                SizePx = 85
            }));
        });
    }

    public void BigText(string text, Vector4? color = null)
    {
        FontText(text, UidFont, color);
    }

    private static void FontText(string text, IFontHandle font, Vector4? color = null)
    {
        FontText(text, font, color == null ? ImGui.GetColorU32(ImGuiCol.Text) : ImGui.GetColorU32(color.Value));
    }

    private static void FontText(string text, IFontHandle font, uint color)
    {
        using var pushedFont = font.Push();
        ImGui.TextUnformatted(text);
    }

    public void Dispose()
    {
        UidFont.Dispose();

        GC.SuppressFinalize(this);
    }
}
