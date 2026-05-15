using Brio.Capabilities.Core;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Widgets.Core;

public class WidgetHelpers
{
    public static void DrawBodies(IEnumerable<Capability> capabilities)
    {
        foreach(var w in capabilities)
        {
            if(!w.Entity.IsAttached)
                break;

            DrawBody(w.Widget);
        }
    }

    public static void DrawBody(IWidget? widget)
    {
        if(widget == null || !widget.Flags.HasFlag(WidgetFlags.DrawBody))
            return;

        using(ImRaii.PushId(widget.GetType().Name))
        {
            ImGuiTreeNodeFlags treeFlags = ImGuiTreeNodeFlags.None;

            if(widget.Flags.HasFlag(WidgetFlags.DefaultOpen))
                treeFlags |= ImGuiTreeNodeFlags.DefaultOpen;

            if(widget.Flags.HasFlag(WidgetFlags.HasAdvanced))
            {
                if(DrawHeaderWithAdvancedButton(widget, treeFlags))
                {
                    widget.DrawBody();
                }
            }
            else
            {
                if(DrawHeaderWithAdvancedButton(widget, treeFlags, false))
                {
                    widget.DrawBody();
                }
            }
        }
    }

    private static bool DrawHeaderWithAdvancedButton(IWidget widget, ImGuiTreeNodeFlags treeFlags, bool showAdvancedButton = true)
    {
        // This was made by Ny for Glamourer, by "stealing" the concept from Brio, so I am now "stealing" the better implementation back. Thanks Ny <3

        var style = ImGui.GetStyle();
        var buttonSize = new Vector2(23) * ImGuiHelpers.GlobalScale;
        var savedCursor = ImGui.GetCursorPos();
        var headerWidth = ImGui.GetContentRegionAvail().X;
        var drawList = ImGui.GetWindowDrawList();

        if(showAdvancedButton)
        {
            headerWidth -= buttonSize.X;

            ImGui.SetCursorPosX(savedCursor.X + headerWidth);
            bool buttonClicked = ImGui.InvisibleButton("###advanced_btn", new Vector2(buttonSize.X, buttonSize.Y - 1));
            bool buttonHovered = ImGui.IsItemHovered();

            if(buttonHovered)
            {
                ImBrio.AttachToolTip($"Advanced {widget.HeaderName}");
            }

            if(buttonClicked)
            {
                widget.ToggleAdvancedWindow();
            }

            var btnMin = ImGui.GetItemRectMin();
            var btnMax = ImGui.GetItemRectMax();

            uint btnBg = (buttonHovered, buttonHovered && ImGui.IsMouseDown(ImGuiMouseButton.Left)) switch
            {
                (true, true) => ImGui.GetColorU32(ImGuiCol.HeaderActive),
                (true, false) => ImGui.GetColorU32(ImGuiCol.HeaderHovered),
                (false, _) => ImGui.GetColorU32(ImGuiCol.Header),
            };

            drawList.AddRectFilled(btnMin, btnMax, btnBg, style.FrameRounding, ImDrawFlags.RoundCornersRight);

            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                var iconStr = FontAwesomeIcon.SquareArrowUpRight.ToIconString();
                var iconSize = ImGui.CalcTextSize(iconStr);

                drawList.AddText(btnMin + (buttonSize - iconSize) * 0.5f, ImGui.GetColorU32(ImGuiCol.Text), iconStr);
            }

            ImGui.SetCursorPos(savedCursor);
        }

        var upperLeft = ImGui.GetCursorScreenPos();
        var lowerRight = upperLeft + new Vector2(headerWidth, ImGui.GetFrameHeight());
        
        uint headerBg = (ImGui.IsMouseHoveringRect(upperLeft, lowerRight - new Vector2(0.001f, 0f)), ImGui.IsMouseDown(ImGuiMouseButton.Left)) switch
        {
            (true, true) => ImGui.GetColorU32(ImGuiCol.HeaderActive),
            (true, false) => ImGui.GetColorU32(ImGuiCol.HeaderHovered),
            (false, _) => ImGui.GetColorU32(ImGuiCol.Header),
        };

        var roundingFlags = showAdvancedButton ? ImDrawFlags.RoundCornersLeft : ImDrawFlags.RoundCornersAll;
        drawList.AddRectFilled(upperLeft, lowerRight, headerBg, style.FrameRounding, roundingFlags);

        // Turns out you can do this, The things I learn
        using var _ = ImRaii.PushColor(ImGuiCol.Header, UIConstants.Transparent)
                            .Push(ImGuiCol.HeaderHovered, UIConstants.Transparent)
                            .Push(ImGuiCol.HeaderActive, UIConstants.Transparent);

        return ImGui.CollapsingHeader(widget.HeaderName, treeFlags);
    }

    public static void DrawQuickIcons(IEnumerable<Capability> capabilities)
    {
        bool drewAny = false;
        foreach(var w in capabilities)
        {
            if(!w.Entity.IsAttached)
                break;

            drewAny = true;
            DrawQuickIconSection(w);
            ImGui.SameLine();
        }

        if(drewAny)
            ImGui.NewLine();
    }

    public static void DrawQuickIconSection(Capability capability) => DrawQuickIconSection(capability.Widget);

    public static void DrawQuickIconSection(IWidget? widget)
    {
        if(widget == null || !widget.Flags.HasFlag(WidgetFlags.DrawQuickIcons))
            return;

        using(ImRaii.PushId(widget.GetType().Name))
        {
            widget.DrawQuickIcons();
        }
    }
}
