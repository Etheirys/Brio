using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Linq;

namespace Brio.UI.Controls.Selectors;

public class VfxSelector(string id) : GamePathSelector(id)
{
    private HashSet<string> _selectedExpansions = [];
    private HashSet<string> _selectedAssets = [];

    private List<string> _expansionOptions = [];
    private List<string> _assetOptions = [];

    protected override void PopulateList()
    {
        var paths = GameDataProvider.Instance.PathDatabase.Vfx.Paths;

        _expansionOptions = [.. paths.Select(p => p.Expansion).Distinct().Order()];
        _assetOptions = [.. paths.Select(p => p.AssetType).Distinct().Order()];

        AddItems(paths.Select(p => new GamePathEntry(p)));
    }

    protected override bool Filter(GamePathEntry item, string search)
    {
        if(_selectedExpansions.Count > 0 && !_selectedExpansions.Contains(item.Info.Expansion))
            return false;

        if(_selectedAssets.Count > 0 && !_selectedAssets.Contains(item.Info.AssetType))
            return false;

        return base.Filter(item, search);
    }

    protected override void DrawOptions()
    {
        DrawMetadataOnlyToggle();

        float half = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2f;

        if(ImBrio.MultiComboBox("###vfx_selector_exp", _expansionOptions, ref _selectedExpansions, half, "All Expansions"))
            UpdateList();

        ImGui.SameLine();

        if(ImBrio.MultiComboBox("###vfx_selector_asset", _assetOptions, ref _selectedAssets, half, "All Asset Types"))
            UpdateList();
    }
}
