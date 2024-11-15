using Brio.Resources;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

internal class StatusEffectSelector(string id) : Selector<Status>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3f;

    protected override SelectorFlags Flags => SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    protected override void PopulateList()
    {
        AddItems(GameDataProvider.Instance.Statuses.Values);
    }

    protected override void DrawItem(Status item, bool isHovered)
    {
        IDalamudTextureWrap? tex = null;
        if(item.Icon != 0)
            tex = UIManager.Instance.TextureProvider.GetFromGameIcon(item.Icon).GetWrapOrEmpty();
        
        tex ??= ResourceProvider.Instance.GetResourceImage("Images.StatusEffect.png");

        float ratio = tex.Size.X / tex.Size.Y;
        Vector2 iconSize = new(EntrySize * ratio, EntrySize);

        ImGui.Image(tex.ImGuiHandle, iconSize);
        ImGui.SameLine();
        ImGui.Text($"{item.Name}\n{item.RowId}\nVFX: {item.VFX.RowId} / Hit: {item.HitEffect.RowId}");
    }

    protected override int Compare(Status itemA, Status itemB)
    {
        if(itemA.RowId < itemB.RowId)
            return -1;

        if(itemA.RowId > itemB.RowId)
            return 1;

        return 0;
    }

    protected override bool Filter(Status item, string search)
    {
        if(item.StatusCategory == 0)
            return false;

        var searchTerm = $"{item.Name} {item.RowId} {item.VFX.RowId} {item.HitEffect.RowId}";

        if(searchTerm.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }
}
