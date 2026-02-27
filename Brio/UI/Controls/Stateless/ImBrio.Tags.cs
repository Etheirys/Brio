using Brio.Library.Tags;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    public enum MouseAction
    {
        None,
        Left,
        Right
    }

    public static MouseAction DrawTag(Tag tag)
    {
        if(ImGui.Button($"{tag.DisplayName}###drawTag{tag.GetHashCode()}"))
            return MouseAction.Left;
        if(ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            return MouseAction.Right;
        return MouseAction.None;
    }

    public static (Tag?, MouseAction?) DrawTags(IEnumerable<Tag> tags)
    {
        Tag? clicked = null;
        MouseAction? action = null;
        float maxWidth = ImGui.GetContentRegionAvail().X;
        foreach(var tag in tags)
        {
            float itemWidth = ImGui.CalcTextSize(tag.DisplayName).X + 10;
            float nextX = ImGui.GetCursorPosX() + itemWidth;
            if(nextX > maxWidth)
            {
                ImGui.NewLine();
            }

            var currentAction = DrawTag(tag);
            if(currentAction != MouseAction.None)
            {
                clicked = tag;
                action = currentAction;
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
        return (clicked, action);
    }
}
