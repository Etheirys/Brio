using Brio.Resources;
using Brio.UI.Widgets.Actor;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

public class StatusEffectSelectorHolder
{
    public Status Status { get; set; }
    public bool _VFXLockEnabled { get; set; }
}

internal class StatusEffectSelector(string id) : Selector<StatusEffectSelectorHolder>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);
    public bool _VFXLockEnabled = false;

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3f;

    protected override SelectorFlags Flags => SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    protected override void PopulateList()
    {
        foreach(var item in GameDataProvider.Instance.Statuses.Values)
        {
            AddItem(new StatusEffectSelectorHolder { Status = item });
        }
    }

	protected override void DrawOptions()
	{
		base.DrawOptions();

		if(ImGui.Checkbox("###status_vfx_filter", ref this._VFXLockEnabled))
		    UpdateList();
        ImGui.SameLine();
        ImGui.Text("Filter out any Status whose VFX value is 0.");

	}

    protected override void DrawItem(StatusEffectSelectorHolder sesh, bool isHovered)
    {
        var item = sesh.Status;

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

    protected override int Compare(StatusEffectSelectorHolder sesh1, StatusEffectSelectorHolder sesh2)
    {
        var itemA = sesh1.Status;
        var itemB = sesh2.Status;

        if(itemA.RowId < itemB.RowId)
            return -1;

        if(itemA.RowId > itemB.RowId)
            return 1;

        return 0;
    }

    protected override bool Filter(StatusEffectSelectorHolder sesh, string search)
    {
        var item = sesh.Status;
		if(_VFXLockEnabled && item.VFX.RowId == 0) return false;

        if(item.StatusCategory == 0)
            return false;
        var searchTerm = $"{item.Name} {item.RowId} {item.VFX.RowId} {item.HitEffect.RowId}";

        if(searchTerm.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }
}
