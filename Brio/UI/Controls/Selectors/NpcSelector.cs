using System;
using System.Numerics;
using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using static Brio.UI.Controls.Selectors.NpcSelector;

namespace Brio.UI.Controls.Selectors;

public class NpcSelector(string id) : Selector<NpcSelectorEntry>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags => SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    private bool showBNpcs = true;
    private bool showENpcs = true;
    private bool showMounts = true;
    private bool showCompanions = true;
    private bool showOrnaments = true;

    protected override void PopulateList()
    {
        var gameDataProvider = GameDataProvider.Instance;

        foreach(var row in gameDataProvider.FilteredBNpcBases)
        {
            var name = gameDataProvider.GetBNpcNameByBase(row.RowId);
            AddItem(new NpcSelectorEntry(name, 0, row));
        }

        foreach(var row in gameDataProvider.FilteredENpcBases)
        {
            var name = gameDataProvider.GetENpcName(row.RowId);
            AddItem(new NpcSelectorEntry(name, 0, row));
        }

        foreach(var row in gameDataProvider.FilteredMounts)
        {
            var name = gameDataProvider.GetMountName(row.RowId);
            AddItem(new NpcSelectorEntry(name, row.Icon, row));
        }

        foreach(var row in gameDataProvider.FilteredCompanions)
        {
            var name = gameDataProvider.GetCompanionName(row.RowId);
            AddItem(new NpcSelectorEntry(name, row.Icon, row));
        }

        foreach(var row in gameDataProvider.FilteredOrnaments)
        {
            var name = GameDataProvider.Instance.GetOrnamentName(row.RowId);
            AddItem(new NpcSelectorEntry(name, row.Icon, row));
        }
    }

    protected override void DrawOptions()
    {
        if(ImGui.Checkbox("Battle NPCs", ref showBNpcs))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Event NPCs", ref showENpcs))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Mounts", ref showMounts))
            UpdateList();

        if(ImGui.Checkbox("Companions", ref showCompanions))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Ornaments", ref showOrnaments))
            UpdateList();
    }

    protected override void DrawItem(NpcSelectorEntry item, bool isHovered)
    {
        var details = item.Appearance.Match(
            bnpc => $"Battle NPC: {bnpc.RowId}\nModel: {bnpc.ModelChara.RowId}",
            enpc => $"Event NPC: {enpc.RowId}\nModel: {enpc.ModelChara.RowId}",
            mount => $"Mount: {mount.RowId}\nModel: {mount.ModelChara.RowId}",
            companion => $"Companion: {companion.RowId}\nModel: {companion.Model.RowId}",
            ornament => $"Ornament: {ornament.RowId}\nModel: {ornament.Model}",
            none => ""
        );

        ImBrio.BorderedGameIcon("icon", item.Icon, "Images.UnknownIcon.png", flags: ImGuiButtonFlags.None, size: IconSize);
        ImGui.SameLine();
        ImGui.Text($"{item.Name}\n{details}");
    }

    protected override bool Filter(NpcSelectorEntry item, string search)
    {
        bool shouldHide = item.Appearance.Match(
            bnpc => !showBNpcs,
            enpc => !showENpcs,
            mount => !showMounts,
            companion => !showCompanions,
            ornament => !showOrnaments,
            none => true
        );

        if(shouldHide)
            return false;

        string searchTerm = item.Appearance.Match(
            bnpc => $"{item.Name} {bnpc.RowId} {bnpc.ModelChara.RowId}",
            enpc => $"{item.Name} {enpc.RowId} {enpc.ModelChara.RowId}",
            mount => $"{item.Name} {mount.RowId} {mount.ModelChara.RowId}",
            companion => $"{item.Name} {companion.RowId} {companion.Model.RowId}",
            ornament => $"{item.Name} {ornament.RowId} {ornament.Model}",
            none => ""
        );

        return searchTerm.Contains(search, StringComparison.InvariantCultureIgnoreCase);
    }

    protected override int Compare(NpcSelectorEntry itemA, NpcSelectorEntry itemB)
    {
        // Mounts
        if(itemA?.Appearance?.Value is Mount && itemB?.Appearance?.Value is not Mount)
            return -1;
        if(itemA?.Appearance?.Value is not Mount && itemB?.Appearance?.Value is Mount)
            return 1;

        // Companions
        if(itemA?.Appearance?.Value is Companion && itemB?.Appearance?.Value is not Companion)
            return -1;
        if(itemA?.Appearance?.Value is not Companion && itemB?.Appearance?.Value is Companion)
            return 1;

        // Event NPCs
        if(itemA?.Appearance?.Value is ENpcBase && itemB?.Appearance?.Value is not ENpcBase)
            return -1;
        if(itemA?.Appearance?.Value is not ENpcBase && itemB?.Appearance?.Value is ENpcBase)
            return 1;

        // Battle NPCs
        if(itemA?.Appearance?.Value is BNpcBase && itemB?.Appearance?.Value is not BNpcBase)
            return -1;
        if(itemA?.Appearance?.Value is not BNpcBase && itemB?.Appearance?.Value is BNpcBase)
            return 1;

        // Ornaments
        if(itemA?.Appearance?.Value is Ornament && itemB?.Appearance?.Value is not Ornament)
            return -1;
        if(itemA?.Appearance?.Value is not Ornament && itemB?.Appearance?.Value is Ornament)
            return 1;

        string nameA = itemA?.Name ?? string.Empty;
        string nameB = itemB?.Name ?? string.Empty;

        try
        {
            // Move unknown names down
            string[] prefixes = ["B:", "E:", "N:"];
            for(int i = 0; i < prefixes.Length; i++)
            {
                var prefix = prefixes[i];
                bool aHas = nameA.StartsWith(prefix, StringComparison.Ordinal);
                bool bHas = nameB.StartsWith(prefix, StringComparison.Ordinal);
                if(aHas && !bHas)
                    return 1;
                if(!aHas && bHas)
                    return 1;
            }

            // Blank string to bottom
            if(string.IsNullOrWhiteSpace(nameA) && !string.IsNullOrWhiteSpace(nameB))
                return 1;

            if(!string.IsNullOrWhiteSpace(nameA) && string.IsNullOrWhiteSpace(nameB))
                return -1;

            // Alphabetical
            return string.Compare(nameA, nameB, StringComparison.InvariantCultureIgnoreCase);
        }
        catch
        {
            return 0; // Fallback to equality
        }
    }

    public record class NpcSelectorEntry(string Name, uint Icon, ActorAppearanceUnion Appearance);
}
