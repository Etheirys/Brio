using Brio.Library.Tags;
using ImGuiNET;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static bool DrawTag(Tag tag)
    {
        return ImGui.Button(tag.Name);
    }

    public static void DrawTags(TagCollection tags)
    {
        float maxWidth = ImGui.GetContentRegionAvail().X;
        foreach(var tag in tags)
        {
            float itemWidth = ImGui.CalcTextSize(tag.Name).X + 10;
            float nextX = ImGui.GetCursorPosX() + itemWidth;
            if(nextX > maxWidth)
            {
                ImGui.NewLine();
            }

            ImBrio.DrawTag(tag);
            ImGui.SameLine();
        }

        ImGui.NewLine();
    }
}
