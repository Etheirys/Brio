using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System;
using System.Numerics;

namespace Brio.UI;

public static class BrioStyle
{
    public static bool EnableStyle { get; set; }
    public static bool EnableColor => Config.ConfigurationService.Instance.Configuration.Appearance.EnableBrioColor;

    static bool _hasPushed = false;
    static bool _hasPushedColor = false;

    public static void PushStyle()
    {
        if(EnableStyle == false)
        {
            return;
        }
        _hasPushed = true;

        if(EnableColor)
        {
            _hasPushedColor = true;
            var theme = ThemeManager.CurrentTheme;

            ImGui.PushStyleColor(ImGuiCol.Text, theme.Text.Text);
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, theme.Text.TextDisabled);
      
            var opacity = Config.ConfigurationService.Instance.Configuration.Appearance.WindowOpacity;
            if(Config.ConfigurationService.Instance.Configuration.Appearance.EnableBlur is false)
                opacity += 0.140f;

            ImGui.PushStyleColor(ImGuiCol.WindowBg, ApplyOpacity(theme.Window.WindowBg, opacity));          
            ImGui.PushStyleColor(ImGuiCol.ChildBg, theme.Window.ChildBg);
            ImGui.PushStyleColor(ImGuiCol.PopupBg, theme.Window.PopupBg);

            ImGui.PushStyleColor(ImGuiCol.Border, theme.Window.Border);
            ImGui.PushStyleColor(ImGuiCol.BorderShadow, theme.Window.BorderShadow);

            ImGui.PushStyleColor(ImGuiCol.FrameBg, theme.Frame.FrameBg);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, theme.Frame.FrameBgHovered);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, theme.Frame.FrameBgActive);

            ImGui.PushStyleColor(ImGuiCol.TitleBg, theme.Window.TitleBg);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, theme.Window.TitleBgActive);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, theme.Window.TitleBgCollapsed);

            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, theme.Window.MenuBarBg);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, theme.Scrollbar.ScrollbarBg);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, theme.Scrollbar.ScrollbarGrab);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, theme.Scrollbar.ScrollbarGrabHovered);
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, theme.Scrollbar.ScrollbarGrabActive);

            ImGui.PushStyleColor(ImGuiCol.CheckMark, theme.Accent.AccentCheckMark);

            ImGui.PushStyleColor(ImGuiCol.SliderGrab, theme.Slider.SliderGrab);
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, theme.Slider.SliderGrabActive);

            ImGui.PushStyleColor(ImGuiCol.Button, theme.Button.Button);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, theme.Button.ButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, theme.Button.ButtonActive);

            ImGui.PushStyleColor(ImGuiCol.Header, theme.Header.Header);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, theme.Header.HeaderHovered);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, theme.Header.HeaderActive);

            ImGui.PushStyleColor(ImGuiCol.Separator, theme.Separator.Separator);
            ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, theme.Separator.SeparatorHovered);
            ImGui.PushStyleColor(ImGuiCol.SeparatorActive, theme.Separator.SeparatorActive);

            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, theme.Misc.ResizeGrip);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, theme.Misc.ResizeGripHovered);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, theme.Misc.ResizeGripActive);

            ImGui.PushStyleColor(ImGuiCol.Tab, theme.Tab.Tab);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, theme.Tab.TabHovered);
            ImGui.PushStyleColor(ImGuiCol.TabActive, theme.Tab.TabActive);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, theme.Tab.TabUnfocused);
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, theme.Tab.TabUnfocusedActive);

            ImGui.PushStyleColor(ImGuiCol.DockingPreview, theme.Docking.DockingPreview);
            ImGui.PushStyleColor(ImGuiCol.DockingEmptyBg, theme.Docking.DockingEmptyBg);

            ImGui.PushStyleColor(ImGuiCol.PlotLines, theme.Misc.PlotLines);

            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, theme.Table.TableHeaderBg);
            ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, theme.Table.TableBorderStrong);
            ImGui.PushStyleColor(ImGuiCol.TableBorderLight, theme.Table.TableBorderLight);
            ImGui.PushStyleColor(ImGuiCol.TableRowBg, theme.Table.TableRowBg);
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, theme.Table.TableRowBgAlt);

            ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, theme.Text.TextSelectedBg);
            ImGui.PushStyleColor(ImGuiCol.DragDropTarget, theme.Misc.DragDropTarget);

            ImGui.PushStyleColor(ImGuiCol.NavHighlight, theme.Misc.NavHighlight);
            ImGui.PushStyleColor(ImGuiCol.NavWindowingDimBg, theme.Misc.NavWindowingDimBg);
            ImGui.PushStyleColor(ImGuiCol.NavWindowingHighlight, theme.Misc.NavWindowingHighlight);
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4));

        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 21.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 9.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabMinSize, 20.0f);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 7f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f * ImGuiHelpers.GlobalScale);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 4f * ImGuiHelpers.GlobalScale);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4f);
    }

    private static uint ApplyOpacity(uint color, float opacity)
    {
        var alpha = (uint)Math.Clamp(opacity * 255f, 0f, 255f);
        return (color & 0x00FFFFFF) | (alpha << 24);
    }

    public static void PopStyle()
    {
        if(_hasPushed)
        {
            _hasPushed = false;

            ImGui.PopStyleVar(19);

            if(_hasPushedColor)
            {
                _hasPushedColor = false;
                ImGui.PopStyleColor(51);
            }
        }
    }
}
