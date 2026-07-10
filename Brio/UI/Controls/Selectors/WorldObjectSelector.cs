using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Linq;

namespace Brio.UI.Controls.Selectors;

public class WorldObjectSelector(string id) : GamePathSelector(id)
{
    private HashSet<string> _selectedExpansions = [];
    private HashSet<string> _selectedSubtypes = [];
    private HashSet<string> _selectedAssets = [];

    private List<string> _expansionOptions = [];
    private List<string> _subtypeOptions = [];
    private List<string> _assetOptions = [];

    protected override void PopulateList()
    {
        var paths = GameDataProvider.Instance.PathDatabase.Models.Paths;

        _expansionOptions = [.. paths.Select(p => p.Expansion).Distinct().Order()];
        _subtypeOptions = [.. paths.Select(p => p.Subtype).Distinct().Order()];
        _assetOptions = [.. paths.Select(p => p.AssetType).Distinct().Order()];

        AddItems(paths.Select(p => new GamePathEntry(p)));
    }

    protected override bool Filter(GamePathEntry item, string search)
    {
        if(_selectedExpansions.Count > 0 && !_selectedExpansions.Contains(item.Info.Expansion))
            return false;

        if(_selectedSubtypes.Count > 0 && !_selectedSubtypes.Contains(item.Info.Subtype))
            return false;

        if(_selectedAssets.Count > 0 && !_selectedAssets.Contains(item.Info.AssetType))
            return false;

        return base.Filter(item, search);
    }

    protected override void DrawOptions()
    {
        DrawMetadataOnlyToggle();

        float third = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3f;

        if(ImBrio.MultiComboBox("###worldobject_exp", _expansionOptions, ref _selectedExpansions, third, "All Expansions"))
            UpdateList();

        ImGui.SameLine();

        if(ImBrio.MultiComboBox("###worldobject_sub", _subtypeOptions, ref _selectedSubtypes, third, "All Subtypes"))
            UpdateList();

        ImGui.SameLine();

        if(ImBrio.MultiComboBox("###worldobject_asset", _assetOptions, ref _selectedAssets, third, "All Asset Types"))
            UpdateList();
    }
}
