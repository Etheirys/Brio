using Brio.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

// I don't really like this I will fix it later TODO (Ken)
public static partial class ImBrio
{
    private static readonly Vector4 FavoriteColor = new(1f, 0.82f, 0.2f, 1f);
    private static readonly Vector4 FavoriteMutedColor = new(0.5f, 0.5f, 0.5f, 0.55f);

    public static bool FavoriteStar(string id, bool isFavorite, Vector2? size = null)
    {
        var buttonSize = size ?? new Vector2(ImGui.GetFrameHeight());

        bool clicked;
        using(ImRaii.PushColor(ImGuiCol.Button, 0u)
            .Push(ImGuiCol.ButtonHovered, 0u)
            .Push(ImGuiCol.ButtonActive, 0u)
            .Push(ImGuiCol.Text, isFavorite ? FavoriteColor : FavoriteMutedColor))
        using(ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero))
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            clicked = ImGui.Button($"{FontAwesomeIcon.Star.ToIconString()}###favorite_{id}", buttonSize);
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(isFavorite ? "Remove Favorite" : "Add Favorite");

        return clicked;
    }

    public static QuickAccessEntry? DrawRecentsStrip(string label, IReadOnlyList<QuickAccessEntry> entries, float iconSize)
    {
        if(entries.Count == 0)
            return null;

        QuickAccessEntry? clicked = null;

        if(ImGui.CollapsingHeader($"{label} ({entries.Count})###entry_strip_{label}"))
        {
            float height = iconSize + ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ScrollbarSize;
            using var child = ImRaii.Child($"###entry_child_{label}", new Vector2(0, height), false, ImGuiWindowFlags.HorizontalScrollbar);
            if(child.Success)
            {
                for(int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if(i > 0) ImGui.SameLine();

                    if(BorderedGameIcon($"entry_{label}_{i}", entry.IconId, "Images.UnknownIcon.png", size: new Vector2(iconSize)))
                        clicked = entry;

                    if(ImGui.IsItemHovered())
                        AttachToolTip($"{entry.DisplayName}{TooltipSeparator}{entry.Payload}");
                }
            }
        }

        return clicked;
    }
}
