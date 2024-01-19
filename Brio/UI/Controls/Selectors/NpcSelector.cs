using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Brio.UI.Controls.Selectors.NpcSelector;

namespace Brio.UI.Controls.Selectors;

internal class NpcSelector(string id) : Selector<NpcSelectorEntry>(id)
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
        foreach(var (_, npc) in GameDataProvider.Instance.BNpcBases)
        {
            string name = $"B:{npc.RowId:D7}";
            name = ResolveName(name);
            AddItem(new NpcSelectorEntry(name, 0, npc));
        }

        foreach(var (_, npc) in GameDataProvider.Instance.ENpcBases)
        {
            string name = $"E:{npc.RowId:D7}";

            var resident = GameDataProvider.Instance.ENpcResidents[npc.RowId];
            if(resident != null)
            {
                if(!string.IsNullOrEmpty(resident.Singular))
                    name = resident.Singular;
            }

            name = ResolveName(name);
            AddItem(new NpcSelectorEntry(name, 0, npc));
        }

        foreach(var (_, mount) in GameDataProvider.Instance.Mounts)
        {
            AddItem(new NpcSelectorEntry(mount.Singular ?? $"Mount {mount.RowId}", mount.Icon, mount));
        }

        foreach(var (_, companion) in GameDataProvider.Instance.Companions)
        {
            AddItem(new NpcSelectorEntry(companion.Singular ?? $"Companion {companion.RowId}", companion.Icon, companion));
        }

        foreach(var (_, ornament) in GameDataProvider.Instance.Ornaments)
        {
            AddItem(new NpcSelectorEntry(ornament.Singular ?? $"Ornament {ornament.RowId}", ornament.Icon, ornament));
        }
    }

    private static string ResolveName(string name)
    {
        var names = ResourceProvider.Instance.GetResourceDocument<IReadOnlyDictionary<string, string>>("Data.NpcNames.json");

        if(names.TryGetValue(name, out var nameOverride))
            name = nameOverride;

        if(name.StartsWith("N:"))
        {
            var nameId = uint.Parse(name.Substring(2));
            if(GameDataProvider.Instance.BNpcNames.TryGetValue(nameId, out var nameRef))
                if(nameRef != null && !string.IsNullOrEmpty(nameRef.Singular))
                    name = nameRef.Singular;
        }

        return name;
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
            bnpc => $"Battle NPC: {bnpc.RowId}\nModel: {bnpc.ModelChara.Row}",
            enpc => $"Event NPC: {enpc.RowId}\nModel: {enpc.ModelChara.Row}",
            mount => $"Mount: {mount.RowId}\nModel: {mount.ModelChara.Row}",
            companion => $"Companion: {companion.RowId}\nModel: {companion.Model.Row}",
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
            bnpc => !showBNpcs || bnpc.RowId == 0,
            enpc => !showENpcs || enpc.RowId == 0,
            mount => !showMounts || mount.ModelChara.Row == 0,
            companion => !showCompanions || companion.Model.Row == 0,
            ornament => !showOrnaments || ornament.Model == 0,
            none => true
        );

        if(shouldHide)
            return false;

        string searchTerm = item.Appearance.Match(
            bnpc => $"{item.Name} {bnpc.RowId} {bnpc.ModelChara.Row}",
            enpc => $"{item.Name} {enpc.RowId} {enpc.ModelChara.Row}",
            mount => $"{item.Name} {mount.RowId} {mount.ModelChara.Row}",
            companion => $"{item.Name} {companion.RowId} {companion.Model.Row}",
            ornament => $"{item.Name} {ornament.RowId} {ornament.Model}",
            none => ""
        );

        if(searchTerm.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }

    protected override int Compare(NpcSelectorEntry itemA, NpcSelectorEntry itemB)
    {
        // Mounts
        if(itemA.Appearance.Value is Mount && itemB.Appearance.Value is not Mount)
            return -1;

        if(itemA.Appearance.Value is not Mount && itemB.Appearance.Value is Mount)
            return 1;

        // Companions
        if(itemA.Appearance.Value is Companion && itemB.Appearance.Value is not Companion)
            return -1;

        if(itemA.Appearance.Value is not Companion && itemB.Appearance.Value is Companion)
            return 1;

        // Event NPCs
        if(itemA.Appearance.Value is ENpcBase && itemB.Appearance.Value is not ENpcBase)
            return -1;

        if(itemA.Appearance.Value is not ENpcBase && itemB.Appearance.Value is ENpcBase)
            return 1;

        // Then Battle NPCs
        if(itemA.Appearance.Value is BNpcBase && itemB.Appearance.Value is not BNpcBase)
            return -1;

        if(itemA.Appearance.Value is not BNpcBase && itemB.Appearance.Value is BNpcBase)
            return 1;

        // Ornaments
        if(itemA.Appearance.Value is Ornament && itemB.Appearance.Value is not Ornament)
            return -1;

        if(itemA.Appearance.Value is not Ornament && itemB.Appearance.Value is Ornament)
            return 1;

        // Move unknown names down
        if(itemA.Name.StartsWith("B:") && !itemB.Name.StartsWith("B:"))
            return 1;

        if(!itemA.Name.StartsWith("B:") && itemB.Name.StartsWith("B:"))
            return 1;

        if(itemA.Name.StartsWith("E:") && !itemB.Name.StartsWith("E:"))
            return 1;

        if(!itemA.Name.StartsWith("E:") && itemB.Name.StartsWith("E:"))
            return 1;

        if(itemA.Name.StartsWith("N:") && !itemB.Name.StartsWith("N:"))
            return 1;

        if(!itemA.Name.StartsWith("N:") && itemB.Name.StartsWith("N:"))
            return 1;

        // Blank string to bottom
        if(string.IsNullOrWhiteSpace(itemA.Name) && !string.IsNullOrWhiteSpace(itemB.Name))
            return 1;

        if(!string.IsNullOrWhiteSpace(itemA.Name) && string.IsNullOrWhiteSpace(itemB.Name))
            return -1;

        // Alphabetical
        return string.Compare(itemA.Name, itemB.Name, StringComparison.InvariantCultureIgnoreCase);
    }

    public record class NpcSelectorEntry(string Name, uint Icon, ActorAppearanceUnion Appearance);
}
