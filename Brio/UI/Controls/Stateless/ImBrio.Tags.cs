using Brio.Library.Tags;
using ImGuiNET;
using System.Collections.Generic;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    public static bool DrawTag(Tag tag)
    {
        return ImGui.Button($"{tag.DisplayName}###drawTag{tag.GetHashCode()}");
    }

    public static Tag? DrawTags(IEnumerable<Tag> tags)
    {
        Tag? clicked = null;
        float maxWidth = ImGui.GetContentRegionAvail().X;
        foreach(var tag in tags)
        {
            float itemWidth = ImGui.CalcTextSize(tag.DisplayName).X + 10;
            float nextX = ImGui.GetCursorPosX() + itemWidth;
            if(nextX > maxWidth)
            {
                ImGui.NewLine();
            }

            if(DrawTag(tag))
                clicked = tag;

            ImGui.SameLine();
        }

        ImGui.NewLine();
        return clicked;
    }
}
