using Brio.Resources;
using System.Collections.Generic;

namespace Brio.Game.Posing;

internal class BoneCategories
{
    public IReadOnlyList<BoneCategory> Categories => _categories;

    private readonly List<BoneCategory> _categories = [];

    public BoneCategories()
    {
        var boneCategoryFile = ResourceProvider.Instance.GetResourceDocument<BoneCategoryFile>("Data.BoneCategories.json");

        foreach(var (id, entry) in boneCategoryFile.Categories)
        {
            var name = Localize.Get($"bone_categories.{id}", id);
            var category = new BoneCategory(id, name, entry.Type, entry.Bones);
            _categories.Add(category);
        }
    }

    internal record class BoneCategory(string Id, string Name, BoneCategoryTypes Type, List<string> Bones);

    private class BoneCategoryFile
    {
        public Dictionary<string, BoneCategoryFileEntry> Categories { get; set; } = [];

        internal record class BoneCategoryFileEntry(BoneCategoryTypes Type, List<string> Bones);
    }

    internal enum BoneCategoryTypes
    {
        Filter
    }
}
