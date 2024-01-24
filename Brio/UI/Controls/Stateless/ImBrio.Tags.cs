using Brio.Library.Tags;
using ImGuiNET;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static bool DrawTag(Tag tag)
    {
        return ImGui.Button(tag.Name);
    }

    public static Tag? DrawTags(TagCollection tags, TagCollection? skip = null, string[]? query = null)
    {
        Tag? clicked = null;
        float maxWidth = ImGui.GetContentRegionAvail().X;
        foreach(var tag in tags)
        {
            if(skip?.Contains(tag) == true)
                continue;

            if(query == null || tag.Search(query))
            {
                float itemWidth = ImGui.CalcTextSize(tag.Name).X + 10;
                float nextX = ImGui.GetCursorPosX() + itemWidth;
                if(nextX > maxWidth)
                {
                    ImGui.NewLine();
                }

                if(ImBrio.DrawTag(tag))
                    clicked = tag;

                ImGui.SameLine();
            }
        }

        ImGui.NewLine();
        return clicked;
    }
}
