using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using ImGuiNET;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

internal class CompanionSelector(string id) : Selector<CompanionRowUnion>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;


    private bool _showCompanions = true;
    private bool _showMounts = true;
    private bool _showOrnaments = true;

    protected override void PopulateList()
    {
        foreach(var companion in GameDataProvider.Instance.Companions.Values)
            AddItem(companion);

        foreach(var mount in GameDataProvider.Instance.Mounts.Values)
            AddItem(mount);

        foreach(var ornament in GameDataProvider.Instance.Ornaments.Values)
            AddItem(ornament);

        AddItem(new None());

    }

    protected override void DrawItem(CompanionRowUnion companionType, bool isHovered)
    {
        ImBrio.BorderedGameIcon("icon", companionType, flags: ImGuiButtonFlags.None, size: IconSize);
    }

    protected override void DrawOptions()
    {
        if(ImGui.Checkbox("Minions", ref _showCompanions))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Mounts", ref _showMounts))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Ornaments", ref _showOrnaments))
            UpdateList();
    }

    protected override bool Filter(CompanionRowUnion item, string search)
    {
        // Always show none
        if(item.Value is None)
            return true;

        bool shouldFilter = item.Match(
            companion => !_showCompanions || companion.Model.RowId == 0,
            mount => !_showMounts || mount.ModelChara.RowId == 0,
            ornament => !_showOrnaments || ornament.Model == 0,
            none => false
        );

        if(shouldFilter)
            return false;

        var searchText = item.Match(
            companion => $"{companion.Singular} {companion.Plural} {companion.RowId} {companion.Model.RowId}",
            mount => $"{mount.Singular} {mount.Plural} {mount.RowId} {mount.ModelChara.RowId}",
            ornament => $"{ornament.Singular} {ornament.Plural} {ornament.RowId} {ornament.Model}",
            none => "none"
        );

        if(searchText.Contains(search, System.StringComparison.InvariantCultureIgnoreCase))
            return true;


        return false;
    }

    protected override int Compare(CompanionRowUnion itemA, CompanionRowUnion itemB)
    {
        // None to top
        if(itemA.Value is None && itemB.Value is not None)
            return -1;

        if(itemA.Value is not None && itemB.Value is None)
            return 1;

        // Get name
        var textA = itemA.Match(
            companion => companion.Singular,
            mount => mount.Singular,
            ornament => ornament.Singular,
            none => ""
        );

        var textB = itemB.Match(
            companion => companion.Singular,
            mount => mount.Singular,
            ornament => ornament.Singular,
            none => ""
        );

        // Blank string to bottom
        if(string.IsNullOrEmpty(textA) && !string.IsNullOrEmpty(textB))
            return 1;

        if(!string.IsNullOrEmpty(textA) && string.IsNullOrEmpty(textB))
            return -1;

        // Alphabetical
        return string.Compare(textA, textB, System.StringComparison.InvariantCultureIgnoreCase);
    }
}
