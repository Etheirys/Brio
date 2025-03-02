using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Brio.Game.World.FestivalService;

namespace Brio.UI.Controls.Selectors;

public class FestivalSelector(string id, IEnumerable<FestivalEntry> entries) : Selector<FestivalEntry>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight();

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;


    private bool _showUnsafe = false;
    private bool _showUnknown = false;

    private readonly IEnumerable<FestivalEntry> _entries = entries;

    protected override void PopulateList()
    {
        AddItems(_entries);
    }

    protected override void DrawItem(FestivalEntry item, bool isHovered)
    {
        ImGui.Text(item.ToString());
    }

    protected override void DrawOptions()
    {
        if(ImGui.Checkbox("Show Unknown", ref _showUnknown))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Show Unsafe", ref _showUnsafe))
            UpdateList();
    }

    protected override bool Filter(FestivalEntry item, string search)
    {
        if(item.Unknown && !_showUnknown)
            return false;

        if(item.Unsafe && !_showUnsafe)
            return false;

        string searchTerm = $"{item.Name} {item.Id}";

        if(searchTerm.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }
}
