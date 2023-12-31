using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Brio.Capabilities.Core;
using System.Collections.Generic;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;

namespace Brio.UI.Widgets.Core;

internal class WidgetHelpers
{
    public static void DrawBodies(IEnumerable<Capability> capabilities)
    {
        foreach (var w in capabilities)
            DrawBody(w);
    }

    public static void DrawBody(Capability capability) => DrawBody(capability.Widget);

    public static void DrawBody(IWidget? widget)
    {
        if (widget == null || !widget.Flags.HasFlag(WidgetFlags.DrawBody))
            return;

        using (ImRaii.PushId(widget.GetType().Name))
        {
            ImGuiTreeNodeFlags treeFlags = ImGuiTreeNodeFlags.None;

            if (widget.Flags.HasFlag(WidgetFlags.DefaultOpen))
                treeFlags |= ImGuiTreeNodeFlags.DefaultOpen;

            if (widget.Flags.HasFlag(WidgetFlags.HasAdvanced))
            {
                var startPos = ImGui.GetCursorPos();
                string tool = $"Advanced {widget.HeaderName}";

                if (ImBrio.FontIconButtonRight("advanced", FontAwesomeIcon.SquareArrowUpRight, 1, tool, bordered: false))
                    widget.ActivateAdvanced();

                ImGui.SetCursorPos(startPos);
            }

            if (ImGui.CollapsingHeader(widget.HeaderName, treeFlags))
                widget.DrawBody();
        }
    }

    public static void DrawQuickIcons(IEnumerable<Capability> capabilities)
    {
        bool drewAny = false;
        foreach (var w in capabilities)
        {
            drewAny = true;
            DrawQuickIconSection(w);
            ImGui.SameLine();
        }

        if (drewAny)
            ImGui.NewLine();
    }

    public static void DrawQuickIconSection(Capability capability) => DrawQuickIconSection(capability.Widget);

    public static void DrawQuickIconSection(IWidget? widget)
    {
        if (widget == null || !widget.Flags.HasFlag(WidgetFlags.DrawQuickIcons))
            return;

        using (ImRaii.PushId(widget.GetType().Name))
        {
            widget.DrawQuickIcons();
        }
    }
}
