using Brio.Core;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Brio.UI.Controls.Stateless;

struct HoldButtonState
{
    public double StartTime;
    public bool WasTriggered;
}

public static partial class ImBrio
{
    public const string TooltipSeparator = "--SEP--";

    private static readonly Dictionary<uint, HoldButtonState> _holdButtonStates = [];


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void FontIcon(FontAwesomeIcon icon)
    {
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Text(icon.ToIconString());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool FontIconButton(FontAwesomeIcon icon)
    {
        bool clicked = false;

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            clicked = ImGui.Button(icon.ToIconString(), new(25 * ImGuiHelpers.GlobalScale));
        }

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool FontIconButton(FontAwesomeIcon icon, Vector2 size)
    {
        bool clicked = false;

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            clicked = ImGui.Button(icon.ToIconString(), size * ImGuiHelpers.GlobalScale);
        }

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IconButtonWithText(FontAwesomeIcon icon, string text, Vector2 size)
    {
        bool clicked = ImGui.Button($"##{text}", size);
        var buttonMin = ImGui.GetItemRectMin();
        var buttonSize = ImGui.GetItemRectSize();
        var drawList = ImGui.GetWindowDrawList();
        var color = ImGui.GetColorU32(ImGuiCol.Text);
        var padding = 5 * ImGuiHelpers.GlobalScale;
        Vector2 iconSize;

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            var iconText = icon.ToIconString();
            iconSize = ImGui.CalcTextSize(iconText);
            drawList.AddText(buttonMin + new Vector2(padding, (buttonSize.Y - iconSize.Y) * 0.5f), color, iconText);
        }

        var textSize = ImGui.CalcTextSize(text);
        var textPosition = buttonMin + new Vector2(padding + iconSize.X + ImGui.GetStyle().ItemInnerSpacing.X, (buttonSize.Y - textSize.Y) * 0.5f);
        drawList.AddText(textPosition, color, text);

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool SliderAngle(string id, ref float angle, float min, float max)
    {
        bool clicked = false;

        float rad = angle * MathHelpers.Deg2Rad;
        if(ImGui.SliderAngle(id, ref rad, min, max))
        {
            angle = rad * MathHelpers.Rad2Deg;
            clicked = true;
        }

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
            if(ImGui.Button($"{icon.ToIconString()}###{id}", new Vector2(25 * ImGuiHelpers.GlobalScale)))
                wasClicked = true;
        }

        if(textColor.HasValue)
            ImGui.PopStyleColor();

        if(!bordered)
            ImGui.PopStyleColor();

        if(tooltip != null)
            AttachToolTip(tooltip);

        if(!enabled)
            ImGui.EndDisabled();

        return wasClicked;
    }

    public static Vector2 ScrollbarSize { get; } = ImGui.CalcTextSize("XXII");


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool FontIconButtonRight(string id, FontAwesomeIcon icon, float position, string? tooltip = null, bool enabled = true, bool bordered = true, uint? textColor = null, Vector2? size = null)
    {
        size ??= new Vector2(25);

        bool wasClicked = false;

        if(enabled is false)
            ImGui.BeginDisabled();

        var buttonSize = size.Value * ImGuiHelpers.GlobalScale;
        var style = ImGui.GetStyle();

        float totalStep = buttonSize.X + style.ItemSpacing.X;
        float offsetFromRight = (position - 1f) * totalStep;
        float cursorPosX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonSize.X - offsetFromRight;

        ImGui.SetCursorPosX(cursorPosX);

        if(bordered is false)
            ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        if(textColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, textColor.Value);

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{icon.ToIconString()}###{id}", size.Value * ImGuiHelpers.GlobalScale))
                wasClicked = true;
        }

        if(textColor.HasValue)
            ImGui.PopStyleColor();

        if(bordered is false)
            ImGui.PopStyleColor();

        if(tooltip is not null)
            AttachToolTip(tooltip);

        if(enabled is false)
            ImGui.EndDisabled();

        return wasClicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Button(string label, FontAwesomeIcon icon)
    {
        return Button(label, icon, Vector2.Zero);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool Button(string label, FontAwesomeIcon icon, Vector2 size, string tooltip = "", bool centerTest = false)
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

            if(string.IsNullOrEmpty(tooltip) is false)
            {
                AttachToolTip(tooltip);
            }

            if(icon != FontAwesomeIcon.None)
            {
                ImGui.SetCursorPos(startPos + ImGui.GetStyle().FramePadding);
                using(ImRaii.PushFont(UiBuilder.IconFont))
                {

                    ImGui.Text(icon.ToIconString());
                }
            }

            size.Y = 1;

            ImGui.SetCursorPos(startPos);
            ImGui.InvisibleButton("##dummy"u8, size);
            ImGui.SetCursorPos(endPos);
        }

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool ToggelButton(string lable, bool isToggled, uint toggledColor = 0, string hoverText = "")
    {
        if(toggledColor == 0) toggledColor = ThemeManager.CurrentTheme.Accent.AccentColor;

        return ToggelButton(lable, Vector2.Zero, isToggled, toggledColor, hoverText);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool ToggelButton(string lable, Vector2 size, bool isToggled, uint toggledColor = 0, string hoverText = "")
    {
        if(toggledColor == 0) toggledColor = ThemeManager.CurrentTheme.Accent.AccentColor;

        if(isToggled)
            ImGui.PushStyleColor(ImGuiCol.Button, toggledColor);

        bool clicked = ImGui.Button(lable, size * ImGuiHelpers.GlobalScale);

        if(isToggled)
            ImGui.PopStyleColor();

        if(string.IsNullOrEmpty(hoverText) == false)
        {
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(hoverText);
        }

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool ToggelFontIconButton(string id, FontAwesomeIcon icon, Vector2 size, bool isToggled, uint toggledColor = 0, string tooltip = "")
    {
        var clicked = false;

        if(toggledColor == 0) toggledColor = ThemeManager.CurrentTheme.Accent.AccentColor;

        if(isToggled)
            ImGui.PushStyleColor(ImGuiCol.Button, toggledColor);

        if(size.X >= 0 || size.Y >= 0)
        {
            size += new Vector2(25);
        }

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{icon.ToIconString()}###{id}", size * ImGuiHelpers.GlobalScale))
                clicked = true;
        }

        if(isToggled)
            ImGui.PopStyleColor();

        if(string.IsNullOrEmpty(tooltip) == false)
        {
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip(tooltip);
        }

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool ToggelFontIconButtonRight(string id, FontAwesomeIcon icon, float position, bool isToggled, uint toggledColor = 0,
        string? tooltip = null, bool enabled = true, bool bordered = true, uint? textColor = null, Vector2? size = null)
    {
        size ??= new Vector2(25);

        bool wasClicked = false;

        if(toggledColor == 0)
            toggledColor = ThemeManager.CurrentTheme.Accent.AccentColor;

        if(enabled is false)
            ImGui.BeginDisabled();

        var buttonSize = size.Value * ImGuiHelpers.GlobalScale;
        var style = ImGui.GetStyle();

        float totalStep = buttonSize.X + style.ItemSpacing.X;
        float offsetFromRight = (position - 1f) * totalStep;
        float cursorPosX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - buttonSize.X - offsetFromRight;

        ImGui.SetCursorPosX(cursorPosX);

        if(isToggled)
            ImGui.PushStyleColor(ImGuiCol.Button, toggledColor);

        if(bordered is false)
            ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        if(textColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, textColor.Value);

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{icon.ToIconString()}###{id}", size.Value * ImGuiHelpers.GlobalScale))
                wasClicked = true;
        }

        if(textColor.HasValue)
            ImGui.PopStyleColor();

        if(bordered is false)
            ImGui.PopStyleColor();

        if(isToggled)
            ImGui.PopStyleColor();

        if(tooltip is not null)
            AttachToolTip(tooltip);

        if(enabled is false)
            ImGui.EndDisabled();

        return wasClicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsItemConfirmed() => ImGui.IsItemDeactivated() && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter));


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool BorderedGameTex(string id, string texPath, string? fallback = null, string? description = null, ImGuiButtonFlags flags = ImGuiButtonFlags.MouseButtonLeft, Vector2? size = null)
    {
        IDalamudTextureWrap? iconTex = null;
        try
        {
            if(texPath.IsNullOrEmpty() is false)
                iconTex = UIManager.Instance.TextureProvider.GetFromGame(texPath).GetWrapOrEmpty();
        }
        catch
        {
            // ignored
        }

        if(fallback is not null)
            iconTex ??= ResourceProvider.Instance.GetResourceImage(fallback);

        if(iconTex is not null)
            return BorderedGameIcon(id, iconTex, description, flags, size);

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
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
                if(ImGui.InvisibleButton("button"u8, containedSize, flags))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void AttachToolTip(string text)
    {
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using(ImRaii.Disabled(false))
            {
                using(ImRaii.Tooltip())
                {
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                    if(text.Contains(TooltipSeparator, StringComparison.Ordinal))
                    {
                        var splitText = text.Split(TooltipSeparator, StringSplitOptions.RemoveEmptyEntries);
                        for(int i = 0; i < splitText.Length; i++)
                        {
                            ImGui.TextUnformatted(splitText[i]);
                            if(i != splitText.Length - 1) ImGui.Separator();
                        }
                    }
                    else
                    {
                        ImGui.TextUnformatted(text);
                    }
                    ImGui.PopTextWrapPos();
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool SeparatorTextButton(string label, FontAwesomeIcon icon, string? tooltip = null, bool enabled = true, bool toggled = false)
    {
        var style = ImGui.GetStyle();
        float availWidth = ImGui.GetContentRegionAvail().X;
        if(availWidth <= 0) return false;

        float buttonSize = 25 * ImGuiHelpers.GlobalScale;
        float lineThickness = ImGuiHelpers.GlobalScale;
        float innerSpacing = style.ItemInnerSpacing.X;

        float textWidth = ImGui.CalcTextSize(label).X;
        float lineHeight = ImGui.GetTextLineHeight();

        float rowHeight = MathF.Max(lineHeight, buttonSize);
        float lineY_offset = rowHeight * 0.5f;

        var cursorPos = ImGui.GetCursorPos();
        var screenPos = ImGui.GetCursorScreenPos();

        float lineY = screenPos.Y + lineY_offset;

        float leftArmEnd = screenPos.X + innerSpacing * 2f;
        float textStartX = leftArmEnd + innerSpacing;
        float textEndX = textStartX + textWidth;
        float rightArmStart = textEndX + innerSpacing;

        float buttonScreenX = screenPos.X + availWidth - buttonSize;
        float rightArmEnd = buttonScreenX - innerSpacing;

        uint lineColor = ImGui.GetColorU32(ImGuiCol.Separator);
        uint textColor = ImGui.GetColorU32(ImGuiCol.Text);

        ImGui.Dummy(new Vector2(availWidth, rowHeight));

        var drawList = ImGui.GetWindowDrawList();

        drawList.AddLine(new Vector2(screenPos.X, lineY), new Vector2(leftArmEnd, lineY), lineColor, lineThickness);

        if(rightArmStart < rightArmEnd)
            drawList.AddLine(new Vector2(rightArmStart, lineY), new Vector2(rightArmEnd, lineY), lineColor, lineThickness);

        float textY = screenPos.Y + (rowHeight - lineHeight) * 0.5f;
        drawList.AddText(new Vector2(textStartX, textY), textColor, label);

        float buttonY = cursorPos.Y + (rowHeight - buttonSize) * 0.5f;
        ImGui.SetCursorPos(new Vector2(cursorPos.X + availWidth - buttonSize, buttonY));

        bool wasClicked = ToggelFontIconButtonRight($"###togbutton_{label}", icon, 1f, toggled, tooltip: tooltip, enabled: enabled);

        ImGui.SetCursorPos(new Vector2(cursorPos.X, cursorPos.Y + rowHeight + style.ItemSpacing.Y));

        return wasClicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void SeparatorText(string label)
    {
        var style = ImGui.GetStyle();
        var drawList = ImGui.GetWindowDrawList();
        var screenPos = ImGui.GetCursorScreenPos();
        float availWidth = ImGui.GetContentRegionAvail().X;
        float lineHeight = ImGui.GetTextLineHeight();

        ImGui.Dummy(new Vector2(availWidth, lineHeight));

        if(!ImGui.IsItemVisible())
            return;

        float innerSpacing = style.ItemInnerSpacing.X;
        float lineY = (screenPos.Y + (lineHeight * 0.5f));

        float leftArmEnd = screenPos.X + (innerSpacing * 2f);
        float textStartX = leftArmEnd + innerSpacing;
        float textEndX = textStartX + ImGui.CalcTextSize(label).X;
        float rightArmStart = textEndX + innerSpacing;
        float rightArmEnd = screenPos.X + availWidth;

        uint lineColor = ImGui.GetColorU32(ImGuiCol.Separator);
        uint textColor = ImGui.GetColorU32(ImGuiCol.Text);

        float lineThickness = ImGuiHelpers.GlobalScale;

        drawList.AddLine(new Vector2(screenPos.X, lineY), new Vector2(leftArmEnd, lineY), lineColor, lineThickness);

        if(rightArmStart < rightArmEnd)
            drawList.AddLine(new Vector2(rightArmStart, lineY), new Vector2(rightArmEnd, lineY), lineColor, lineThickness);

        drawList.AddText(new Vector2(textStartX, screenPos.Y), textColor, label);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void PillDummyBox(ref ImDrawListPtr dl, float width, float height, uint color)
    {
        var pillPosZ = ImGui.GetCursorScreenPos();
        dl.AddRectFilled(pillPosZ, pillPosZ + new Vector2(width * ImGuiHelpers.GlobalScale, height * ImGuiHelpers.GlobalScale), color);
        ImGui.Dummy(new Vector2(width * ImGuiHelpers.GlobalScale, height * ImGuiHelpers.GlobalScale));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool BoneOverlayVisibilityButton(string id, bool isVisible, Vector2 size, string? tooltip = null, bool enabled = true)
    {
        var buttonSize = size;

        uint baseBoneColor = 0xFFFFBF7A;
        uint hiddenBoneColor = 0xFF444444;
        uint baseJointColor = 0xFFFFFFFF;
        uint hiddenJointColor = 0xFF3A3A3A;
        uint slashColor = 0xCC4444CC;

        uint boneColor = isVisible ? baseBoneColor : hiddenBoneColor;
        uint jointsColor = isVisible ? baseJointColor : hiddenJointColor;

        using var _ = ImRaii.Disabled(!enabled);

        bool clicked = ImGui.InvisibleButton($"##{id}", buttonSize);
        bool hovered = ImGui.IsItemHovered();

        if(tooltip is not null)
            AttachToolTip(tooltip);

        var drawList = ImGui.GetWindowDrawList();
        var rectMin = ImGui.GetItemRectMin();
        var rectMax = ImGui.GetItemRectMax();

        var rectSize = rectMax - rectMin;

        float iconSize = MathF.Min(rectSize.X, rectSize.Y);

        float centerX = rectMin.X + (rectSize.X * 0.5f);
        float centerY = rectMin.Y + (rectSize.Y * 0.5f) - (iconSize * 0.04f);

        float skeletonScale = iconSize * 0.36f;
        float lineThickness = MathF.Max(1f, iconSize * 0.038f);
        float jointRadius = MathF.Max(1.5f, iconSize * 0.055f);

        drawList.AddRectFilled(rectMin, rectMax,
            hovered ? ImGui.GetColorU32(ImGuiCol.ButtonHovered)
                    : ImGui.GetColorU32(ImGuiCol.Button), 4f);

        // bone & joint positions 
        var head = new Vector2(centerX, centerY - skeletonScale);
        var neck = new Vector2(centerX, centerY - (skeletonScale * 0.58f));
        var mid = new Vector2(centerX, centerY - (skeletonScale * 0.05f));

        var pelvis = new Vector2(centerX, centerY + (skeletonScale * 0.28f));

        var lShoulder = new Vector2(centerX - (skeletonScale * 0.42f), centerY - (skeletonScale * 0.32f));
        var rShoulder = new Vector2(centerX + (skeletonScale * 0.42f), centerY - (skeletonScale * 0.32f));

        var lElbow = new Vector2(centerX - (skeletonScale * 0.52f), centerY + (skeletonScale * 0.08f));
        var rElbow = new Vector2(centerX + (skeletonScale * 0.52f), centerY + (skeletonScale * 0.08f));

        var lHip = new Vector2(centerX - (skeletonScale * 0.24f), centerY + (skeletonScale * 0.28f));
        var rHip = new Vector2(centerX + (skeletonScale * 0.24f), centerY + (skeletonScale * 0.28f));

        var lKnee = new Vector2(centerX - (skeletonScale * 0.28f), centerY + (skeletonScale * 0.70f));
        var rKnee = new Vector2(centerX + (skeletonScale * 0.28f), centerY + (skeletonScale * 0.70f));

        ReadOnlySpan<(Vector2, Vector2)> bones =
        [
            (neck,      mid),
            (mid,       pelvis),
            (lShoulder, neck),      (rShoulder, neck),
            (lShoulder, lElbow),    (rShoulder, rElbow),
            (lHip,      pelvis),    (rHip,      pelvis),
            (lHip,      lKnee),     (rHip,      rKnee),
        ];

        foreach(var (x, y) in bones)
            drawList.AddLine(x, y, boneColor, lineThickness);

        // head
        drawList.AddCircle(head, iconSize * 0.09f, boneColor, 12, lineThickness);

        ReadOnlySpan<Vector2> jointPoints =
        [
            neck, mid, pelvis,
            lShoulder, rShoulder,
            lElbow,    rElbow,
            lHip,      rHip,
            lKnee,     rKnee,
        ];

        foreach(var joint in jointPoints)
            drawList.AddCircleFilled(joint, jointRadius, jointsColor);

        if(!isVisible)
        {
            float slashRadius = skeletonScale * 1.05f;
            float slashOffset = slashRadius / MathF.Sqrt(2f);
            drawList.AddLine(
                new Vector2(centerX - slashOffset, centerY - slashOffset),
                new Vector2(centerX + slashOffset, centerY + slashOffset),
                slashColor, lineThickness + 1.5f);
        }

        return clicked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool MultiComboBox<T>(string id, IReadOnlyList<T> options, ref HashSet<T> selected, float width, string allPreviewText = "All")
    {
        bool changed = false;
        string preview = selected.Count switch
        {
            0 => allPreviewText,
            1 => $"{selected.First()}",
            _ => $"{selected.Count} selected"
        };

        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo(id, preview, ImGuiComboFlags.HeightLarge);
        if(combo.Success)
        {
            if(selected.Count > 0 && ImGui.Selectable("Clear###clear_all", false, ImGuiSelectableFlags.DontClosePopups))
            {
                selected.Clear();
                changed = true;
            }

            foreach(var opt in options)
            {
                bool on = selected.Contains(opt);
                if(ImGui.Checkbox($"{opt}###{id}_{opt}", ref on))
                {
                    if(on) selected.Add(opt);
                    else selected.Remove(opt);
                    changed = true;
                }
            }
        }

        return changed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool TruncatedText(string text, float maxWidth)
    {
        if(maxWidth <= 0 || ImGui.CalcTextSize(text).X <= maxWidth)
        {
            ImGui.TextUnformatted(text);
            return false;
        }

        const string ellipsis = "..";
        var n = text.Length - 1;
        while(n > 0 && ImGui.CalcTextSize(text[..n] + ellipsis).X > maxWidth)
            n--;

        ImGui.TextUnformatted(text[..n] + ellipsis);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool HoldButton(ImU8String id, string label, FontAwesomeIcon icon, float holdDuration = 1.0f, Vector2? btnsize = null, string tooltip = "", bool centerTest = false, bool onlyIcon = false)
    {
        bool wasTriggered = false;

        double currentTime = Environment.TickCount64;
        var style = ImGui.GetStyle();

        Vector2 size = btnsize ?? new Vector2(25 * ImGuiHelpers.GlobalScale);

        float iconWidth = 40;
        float innerWidth = iconWidth + style.ItemInnerSpacing.X + ImGui.CalcTextSize(label).X;

        if(size.X == 0)
        {
            size.X = innerWidth + style.FramePadding.X * 2;
        }
        else
        {
            innerWidth = size.X - style.FramePadding.X * 2;
        }

        float textOffset = (iconWidth + style.ItemInnerSpacing.X) / innerWidth;

        var uid = ImGui.GetID(id); // wow. I need to use this in other places 
        if(!_holdButtonStates.TryGetValue(uid, out var state))
        {
            state = new HoldButtonState();
        }

        bool isActive;
        bool activated;
        Vector2 startPos;
        Vector2 buttonMin;
        Vector2 buttonMax;

        if(onlyIcon)
        {
            FontIconButton(icon);
            AttachToolTip(tooltip);

            isActive = ImGui.IsItemActive();
            activated = ImGui.IsItemActivated();
            buttonMin = ImGui.GetItemRectMin();
            buttonMax = ImGui.GetItemRectMax();
        }
        else
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(textOffset, 0.5f), !centerTest))
            {
                startPos = ImGui.GetCursorPos();
                ImGui.Button(label, size);
                isActive = ImGui.IsItemActive();
                activated = ImGui.IsItemActivated();
                buttonMin = ImGui.GetItemRectMin();
                buttonMax = ImGui.GetItemRectMax();

                Vector2 endPos = ImGui.GetCursorPos();

                if(!string.IsNullOrEmpty(tooltip))
                {
                    AttachToolTip(tooltip);
                }

                ImGui.SetCursorPos(startPos + style.FramePadding);

                using(ImRaii.PushFont(UiBuilder.IconFont))
                    ImGui.Text(icon.ToIconString());

                ImGui.SetCursorPos(endPos);
            }
        }

        if(activated && ImGui.GetIO().KeyCtrl)
        {
            _holdButtonStates.Remove(uid);
            return true;
        }

        if(isActive)
        {
            if(activated || state.StartTime == 0)
            {
                state.StartTime = currentTime;
                state.WasTriggered = false;
            }

            float progress = Math.Min((float)((currentTime - state.StartTime) / 1000.0 / holdDuration), 1.0f);

            uint accent = ThemeManager.CurrentTheme.Accent.AccentColor;
            uint progressColor = (accent & 0x00FFFFFF) | 0xA0000000;

            ImGui.GetWindowDrawList().AddRectFilled(buttonMin, new Vector2(buttonMin.X + (buttonMax.X - buttonMin.X) * progress, buttonMax.Y), progressColor);

            if(progress >= 1.0f && !state.WasTriggered)
            {
                state.WasTriggered = true;
                wasTriggered = true;
            }

            _holdButtonStates[uid] = state;
        }
        else
        {
            _holdButtonStates.Remove(uid);
        }

        return wasTriggered;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void VerticalSeparator(float height = 0f, float thickness = 1f, uint? color = null, float padding = 1f)
    {
        ImGui.SameLine();

        if(height <= 0f)
        {
            height = ImGui.GetFrameHeight();
        }

        height *= ImGuiHelpers.GlobalScale;

        float pad = padding * ImGuiHelpers.GlobalScale;
        float thick = thickness * ImGuiHelpers.GlobalScale;
        uint col = color ?? ImGui.GetColorU32(ImGuiCol.Separator);

        Vector2 cursorPos = ImGui.GetCursorScreenPos();
        float x = cursorPos.X + pad;
        float y = cursorPos.Y;

        ImGui.GetWindowDrawList().AddLine(new Vector2(x, y), new Vector2(x, y + height), col, thick);

        ImGui.Dummy(new Vector2((pad * 2) + thick, height));

        ImGui.SameLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void VerticalPadding(float leng)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + leng * ImGuiHelpers.GlobalScale);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void HorizontalPadding(float leng)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + leng * ImGuiHelpers.GlobalScale);
    }
}
