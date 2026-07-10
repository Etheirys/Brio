using Brio.Core;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Resources.Extra;

public class FurnitureDatabase
{
    private readonly Dictionary<(uint ModelKey, bool Indoors), FurnitureInfo> _keyLookup;
    private readonly Dictionary<string, FurnitureInfo> _pathLookup;
    private readonly MultiValueDictionary<uint, FurnitureInfo> _iconLookup;

    private readonly List<FurnitureInfo> _furnishings;
    private readonly List<string> _categories;

    private static readonly Dictionary<uint, string> PlacementTypes = new()
    {
        { 12, "Indoor Furnishings" },
        { 13, "Tables"             },
        { 14, "Tabletop"           },
        { 15, "Wall-mounted"       },
        { 16, "Rugs"               },
    };

    public FurnitureDatabase(IDataManager dataManager)
    {
        _keyLookup = [];
        _pathLookup = [];
        _furnishings = [];
        _iconLookup = new();

        var list = new List<FurnitureInfo>(2000);

        var indoorSheet = dataManager.GetExcelSheet<HousingFurniture>();
        var indoorCatList = dataManager.GetExcelSheet<FurnitureCatalogItemList>();
        var indoorCatMeta = dataManager.GetExcelSheet<FurnitureCatalogCategory>();

        if(indoorSheet is not null && indoorCatList is not null && indoorCatMeta is not null)
        {
            var indoorCatMap = new Dictionary<uint, string>();
            foreach(var catRow in indoorCatList)
            {
                if(!indoorCatMeta.TryGetRow(catRow.Category.RowId, out var meta)) continue;
                var catName = PlacementTypes.TryGetValue(meta.Unknown0, out var pt)
                    ? pt
                    : meta.Category.ToString();
                indoorCatMap[catRow.Item.RowId] = catName;
            }

            foreach(var row in indoorSheet)
            {
                var item = row.Item.ValueNullable;
                if(item is null) continue;
                var name = item.Value.Name.ToString();
                if(string.IsNullOrWhiteSpace(name)) continue;

                indoorCatMap.TryGetValue(item.Value.RowId, out var cat);
                list.Add(new FurnitureInfo(name, row.ModelKey, true, cat ?? "Uncategorised", item.Value.Icon));
            }
        }

        var outdoorSheet = dataManager.GetExcelSheet<HousingYardObject>();
        var outdoorCatList = dataManager.GetExcelSheet<YardCatalogItemList>();
        var outdoorCatMeta = dataManager.GetExcelSheet<YardCatalogCategory>();

        if(outdoorSheet is not null && outdoorCatList is not null && outdoorCatMeta is not null)
        {
            var outdoorCatMap = new Dictionary<uint, string>();
            foreach(var catRow in outdoorCatList)
            {
                if(!outdoorCatMeta.TryGetRow(catRow.Category.RowId, out var meta)) continue;
                outdoorCatMap[catRow.Item.RowId] = meta.Category.ToString();
            }

            foreach(var row in outdoorSheet)
            {
                var item = row.Item.ValueNullable;
                if(item is null) continue;
                var name = item.Value.Name.ToString();
                if(string.IsNullOrWhiteSpace(name)) continue;

                outdoorCatMap.TryGetValue(item.Value.RowId, out var cat);
                list.Add(new FurnitureInfo(name, row.ModelKey, false, cat ?? "Uncategorised", item.Value.Icon));
            }
        }

        _furnishings = [.. list
            .GroupBy(f => (f.ModelKey, f.Indoors))
            .Select(g => g.First())
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)];

        _categories = [.. _furnishings.Select(f => f.Category).Distinct().Order()];

        foreach(var f in _furnishings)
            AddFurnishing(f);
    }

    public FurnitureInfo? GetByModelKey(uint modelKey, bool indoors)
        => _keyLookup.TryGetValue((modelKey, indoors), out var v) ? v : null;
    public FurnitureInfo? GetByPath(string path)
        => _pathLookup.TryGetValue(path, out var v) ? v : null;
    public IEnumerable<FurnitureInfo> GetByIcon(uint iconId)
        => _iconLookup.TryGetValues(iconId, out var values) ? values : [];
    public IEnumerable<FurnitureInfo> GetByCategory(string category)
        => _furnishings.Where(f => f.Category == category);

    public IReadOnlyList<FurnitureInfo> GetAll() => _furnishings;
    public IReadOnlyList<string> GetCategories() => _categories;

    private void AddFurnishing(FurnitureInfo info)
    {
        _keyLookup[(info.ModelKey, info.Indoors)] = info;
        _pathLookup[info.GetPath()] = info;
        _iconLookup.Add(info.IconId, info);
    }

    public record class FurnitureInfo(string Name, uint ModelKey, bool Indoors, string Category, uint IconId)
    {
        public string GetPath()
        {
            var model = ModelKey.ToString("0000");
            var location = Indoors ? "indoor" : "outdoor";
            var funGar = Indoors ? "fun" : "gar";
            return $"bgcommon/hou/{location}/general/{model}/asset/{funGar}_b0_m{model}.sgb";
        }
    }
}
