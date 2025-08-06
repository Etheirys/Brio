using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Theming;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;
public static partial class ImBrio
{
    public static void FontIcon(FontAwesomeIcon icon)
    {
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Text(icon.ToIconString());
        }
    }

    public static bool FontIconButton(FontAwesomeIcon icon)
    {
        return FontIconButton(icon, new());
    }

    public static bool FontIconButton(FontAwesomeIcon icon, Vector2 size)
    {
        bool clicked = false;

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            clicked = ImGui.Button(icon.ToIconString(), size);
        }

        return clicked;
    }

    public static bool FontIconButton(string id, FontAwesomeIcon icon, string? tooltip = null, bool enabled = true, bool bordered = true, uint? textColor = null)
    {
        bool wasClicked = false;

        if(!enabled)
            ImGui.BeginDisabled();

        if(!bordered)
            ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        if(textColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, textColor.Value);

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{icon.ToIconString()}###{id}", new Vector2(25)))
                wasClicked = true;
        }

        if(textColor.HasValue)
            ImGui.PopStyleColor();

        if(!bordered)
            ImGui.PopStyleColor();

        if(tooltip != null && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        if(!enabled)
            ImGui.EndDisabled();

        return wasClicked;
    }
    public static bool FontIconButtonRight(string id, FontAwesomeIcon icon, float position, string? tooltip = null, bool enabled = true, bool bordered = true, uint? textColor = null)
    {
        bool wasClicked = false;

        if(enabled is false)
            ImGui.BeginDisabled();

        var pixelPos = ImGui.GetWindowSize().X - ((ImGui.CalcTextSize("XXX").X + (ImGui.GetStyle().FramePadding.X * 2)) * position);

        ImGui.SetCursorPosX(pixelPos);

        if(bordered is false)
            ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        if(textColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, textColor.Value);

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{icon.ToIconString()}###{id}", new Vector2(25)))
                wasClicked = true;
        }

        if(textColor.HasValue)
            ImGui.PopStyleColor();

        if(bordered is false)
            ImGui.PopStyleColor();

        if(tooltip is not null && ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        if(enabled is false)
            ImGui.EndDisabled();

        return wasClicked;
    }

    public static bool Button(string label, FontAwesomeIcon icon)
    {
        return Button(label, icon, Vector2.Zero);
    }

    public static bool Button(string label, FontAwesomeIcon icon, Vector2 size, string hoverText = "", bool centerTest = false)
    {
        bool clicked;

        // for consistency, hard-code this
        float iconWidth = 40;
        float textWidth = ImGui.CalcTextSize(label).X;
        float innerWidth = iconWidth + ImGui.GetStyle().ItemInnerSpacing.X + textWidth;
        float neededWidth = innerWidth + (ImGui.GetStyle().FramePadding.X * 2);

        if(size.X == 0)
        {
            size.X = neededWidth;
        }
        else
        {
            innerWidth = size.X - (ImGui.GetStyle().FramePadding.X * 2);
        }

        float iconR = iconWidth + ImGui.GetStyle().ItemInnerSpacing.X;
        float textOffset = iconR / innerWidth;
        using(ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(textOffset, 0.5f), centerTest == false))
        {
            Vector2 startPos = ImGui.GetCursorPos();
            clicked = ImGui.Button(label, size);
            Vector2 endPos = ImGui.GetCursorPos();

            if(string.IsNullOrEmpty(hoverText) is false)
            {
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip(hoverText);
            }

            ImGui.SetCursorPos(startPos + ImGui.GetStyle().FramePadding);
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.Text(icon.ToIconString());
            }

            size.Y = 1;

            ImGui.SetCursorPos(startPos);
            ImGui.InvisibleButton("##dummy", size);
            ImGui.SetCursorPos(endPos);
        }

        return clicked;
    }

    public static bool ToggelButton(string lable, bool isToggled, uint toggledColor = 0, string hoverText = "")
    {
        if(toggledColor == 0) toggledColor = TheameManager.CurrentTheame.Accent.AccentColor;

        return ToggelButton(lable, Vector2.Zero, isToggled, toggledColor, hoverText);
    }

    public static bool ToggelButton(string lable, Vector2 size, bool isToggled, uint toggledColor = 0, string hoverText = "")
    {
        if(toggledColor == 0) toggledColor = TheameManager.CurrentTheame.Accent.AccentColor;

        if(isToggled)
            ImGui.PushStyleColor(ImGuiCol.Button, toggledColor);

        bool clicked = ImGui.Button(lable, size);

        if(isToggled)
            ImGui.PopStyleColor();

        if(string.IsNullOrEmpty(hoverText) == false)
        {
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(hoverText);
        }

        return clicked;
    }

    public static bool ToggelFontIconButton(string id, FontAwesomeIcon icon, Vector2 size, bool isToggled, uint toggledColor = 0, string hoverText = "")
    {
        var clicked = false;

        if(toggledColor == 0) toggledColor = TheameManager.CurrentTheame.Accent.AccentColor;

        if(isToggled)
            ImGui.PushStyleColor(ImGuiCol.Button, toggledColor);

        if(size.X >= 0 || size.Y >= 0)
        {
            size += new Vector2(25);
        }

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{icon.ToIconString()}###{id}", size))
                clicked = true;
        }

        if(isToggled)
            ImGui.PopStyleColor();

        if(string.IsNullOrEmpty(hoverText) == false)
        {
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(hoverText);
        }

        return clicked;
    }

    public static bool IsItemConfirmed() => ImGui.IsItemDeactivated() && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter));

    public static bool BorderedGameIcon(string id, uint iconId, string fallback, string? description = null, ImGuiButtonFlags flags = ImGuiButtonFlags.MouseButtonLeft, Vector2? size = null)
    {
        IDalamudTextureWrap? iconTex = null;
        try
        {
            if(iconId != 0)
                iconTex = UIManager.Instance.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
        }
        catch
        {
            // ignored
        }

        iconTex ??= ResourceProvider.Instance.GetResourceImage(fallback);

        return BorderedGameIcon(id, iconTex, description, flags, size);
    }

    public static bool BorderedGameIcon(string id, IDalamudTextureWrap texture, string? description = null, ImGuiButtonFlags flags = ImGuiButtonFlags.MouseButtonLeft, Vector2? size = null)
    {
        using(ImRaii.PushId(id))
        {
            bool result = false;

            var border = ResourceProvider.Instance.GetResourceImage("Images.IconBorder.png");

            var startPos = ImGui.GetCursorPos();

            var containedSize = size ?? new Vector2(ImGui.GetTextLineHeight() * 4f);

            var offsetTopLeft = new Vector2(6, 3);
            var offsetBottomRight = new Vector2(1, 9);

            var realSize = border.Size - offsetTopLeft - offsetBottomRight;

            var scale = containedSize / realSize;

            var scaleOffsetTopLeft = offsetTopLeft * scale;

            var totalSize = border.Size * scale + scaleOffsetTopLeft;

            ImGui.SetCursorPos(startPos + scaleOffsetTopLeft);
            ImGui.Image(texture.Handle, containedSize);

            if(flags.HasFlag(ImGuiButtonFlags.MouseButtonLeft) || flags.HasFlag(ImGuiButtonFlags.MouseButtonRight) || flags.HasFlag(ImGuiButtonFlags.MouseButtonMiddle))
            {
                ImGui.SetCursorPos(startPos + scaleOffsetTopLeft);
                if(ImGui.InvisibleButton($"button", containedSize, flags))
                {
                    result = true;
                }
                if(ImGui.IsItemHovered())
                {
                    Vector2 topPos = ImGui.GetItemRectMin();
                    ImGui.GetWindowDrawList().AddRectFilled(topPos, topPos + containedSize, ImGui.GetColorU32(new Vector4(1, 1, 1, 0.2f)));
                }
            }

            ImGui.SetCursorPos(startPos);
            ImGui.Image(border.Handle, totalSize);

            if(!string.IsNullOrEmpty(description))
            {
                ImGui.SameLine();
                ImGui.Text(description);
            }

            return result;
        }
    }
}
