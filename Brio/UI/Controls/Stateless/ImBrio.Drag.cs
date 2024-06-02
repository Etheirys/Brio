using Brio.Input;
using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;
internal static partial class ImBrio
{
    public static (bool anyActive, bool didChange) DragFloat3(string label, ref Vector3 vectorValue, float step = 1.0f, FontAwesomeIcon icon = FontAwesomeIcon.None, string tooltip = "")
    {
        if(icon == FontAwesomeIcon.None)
        {
            ImGui.Text(label);
        }
        else
        {
            ImBrio.Icon(icon);
        }

        ImGui.SameLine();

        uint id = ImGui.GetID(label);

        Vector2 size = new(0, 0)
        {
            X = GetRemainingWidth() - 32 + ImGui.GetStyle().ItemSpacing.X
        };

        (bool changed, bool active) = DragFloat3Horizontal($"###{id}_drag3", ref vectorValue, step, size);

        return (active, changed);
    }

    public static (bool anyActive, bool didChange) DragFloat3Horizontal(string label, ref Vector3 value, float step, Vector2 size)
    {
        bool changed = false;
        bool active = false;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier))
            step *= 10;

        if(size.X <= 0)
            size.X = ImBrio.GetRemainingWidth();

        float entryWidth = (size.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 3;
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_X", ref value.X, step / 10);
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("X");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value.X += mouseWheel * step;
                changed = true;
            }
        }
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_Y", ref value.Y, step / 10);
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Y");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value.Y += mouseWheel * step;
                changed = true;
            }
        }
        active |= ImGui.IsItemActive();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(entryWidth);

        changed |= ImGui.DragFloat($"##{label}_Z", ref value.Z, step / 10);
        if(ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Z");
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value.Z += mouseWheel * step;
                changed = true;
            }
        }
        active |= ImGui.IsItemActive();

        return (active, changed);
    }

    public static (bool anyActive, bool didChange) DragFloat(string label, ref float value, float step = 0.1f, string tooltip = "")
    {
        bool changed = false;
        bool active = false;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier))
            step /= 10;

        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier))
            step *= 10;

        float buttonWidth = 32;
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.ArrowButton($"##{label}_decrease", ImGuiDir.Left))
        {
            value -= step;
            changed = true;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"Decrease {tooltip}");

        ImGui.SameLine();

        bool hasLabel = !label.StartsWith("##");

        if(hasLabel)
        {
            ImGui.SetNextItemWidth((ImGui.GetWindowWidth() * 0.65f) - (buttonWidth * 2) - ImGui.GetStyle().CellPadding.X);
        }
        else
        {
            ImGui.SetNextItemWidth((ImBrio.GetRemainingWidth() - buttonWidth) + ImGui.GetStyle().ItemSpacing.X);
        }


        changed |= ImGui.DragFloat($"##{label}_drag", ref value, step / 10.0f);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"{tooltip}");
        active |= ImGui.IsItemActive();

        if(ImGui.IsItemHovered())
        {
            float mouseWheel = ImGui.GetIO().MouseWheel / 10;
            if(mouseWheel != 0)
            {
                value += mouseWheel * step;
                changed = true;
            }
        }


        ImGui.SameLine();
        ImGui.SetNextItemWidth(buttonWidth);
        if(ImGui.ArrowButton($"##{label}_increase", ImGuiDir.Right))
        {
            value += step;
            changed = true;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"Increase {tooltip}");

        if(hasLabel)
        {
            ImGui.SameLine();
            ImGui.Text(label);
        }

        return (active, changed);
    }
}


