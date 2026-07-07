using Brio.Resources;
using Brio.Resources.Extra;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

public sealed record GamePathEntry
{
    public GamePathInfo Info { get; }
    public PathData? Metadata { get; }
    public string DisplayLabel { get; }

    public GamePathEntry(GamePathInfo info)
    {
        Info = info;
        Metadata = GameDataProvider.Instance.PathDatabase.GetPathDataByPath(info.Path);
        DisplayLabel = Metadata is not null ? $"{Metadata.Name} ({info.DisplayName})" : info.DisplayName;
    }
}

public abstract class GamePathSelector(string id) : Selector<GamePathEntry>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;

    protected override SelectorFlags Flags => SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    private bool _metadataOnly = false;

    protected override bool Filter(GamePathEntry item, string search)
    {
        if(_metadataOnly && item.Metadata is null)
            return false;

        if(string.IsNullOrWhiteSpace(search))
            return true;

        return MatchesSearch(item, search);
    }

    protected override int Compare(GamePathEntry itemA, GamePathEntry itemB)
    {
        int assetCompare = string.Compare(itemA.Info.AssetType, itemB.Info.AssetType, StringComparison.InvariantCultureIgnoreCase);
        if(assetCompare != 0)
            return assetCompare;

        return string.Compare(itemA.Info.DisplayName, itemB.Info.DisplayName, StringComparison.InvariantCultureIgnoreCase);
    }

    protected override void DrawItem(GamePathEntry item, bool isHovered)
    {
        ImGui.Text($"{item.DisplayLabel}\n{item.Info.Expansion} - {item.Info.AssetType}");
    }

    protected override void DrawTooltip(GamePathEntry item)
    {
        ImBrio.AttachToolTip(item.Metadata is not null ? $"{item.Metadata.Name}{ImBrio.TooltipSeparator}{item.Info.Path}" : $"{item.Info.DisplayName}{ImBrio.TooltipSeparator}{item.Info.Path}");
    }

    protected void DrawMetadataOnlyToggle()
    {
        if(ImGui.Checkbox("Only items with metadata###metadata_only", ref _metadataOnly))
            UpdateList();
    }

    private static bool MatchesSearch(GamePathEntry item, string search)
    {
        var info = item.Info;
        var metadata = item.Metadata;

        if(info.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
            || info.Path.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;

        if(metadata is null)
            return false;

        if(metadata.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach(var tag in metadata.Tags)
            if(tag.Contains(search, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
