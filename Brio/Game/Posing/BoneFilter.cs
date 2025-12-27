using Brio.Game.Posing.Skeletons;
using System.Collections.Generic;
using static Brio.Game.Posing.BoneCategories;

namespace Brio.Game.Posing;

public class SubCategory(string[] Categories, bool Enabled)
{
    public string[] Categories { get; } = Categories;
    public bool Enabled { get; set; } = Enabled;
}

public class BoneFilter
{
    private readonly PosingService _posingService;

    private readonly Dictionary<string, SubCategory> _subCategories = [];

    private readonly HashSet<string> _allowedCategories = [];

    private readonly HashSet<string> _excludedPrefixes = [];

    private readonly Dictionary<(string boneName, PoseInfoSlot slot, bool considerHidden), bool> _cache = [];
    private readonly Dictionary<string, List<string>> _bonePrefixToCategories = [];

    private bool _weaponsAllowed;
    private bool _otherAllowed;
    private bool _ornamentsAllowed;
    private bool _propAllowed;

    public IReadOnlyList<BoneCategory> AllCategories => _posingService.BoneCategories.Categories;

    public BoneFilter(PosingService posingService)
    {
        _posingService = posingService;

        // Add "n_throw" to excluded bone by default
        _excludedPrefixes.Add("n_throw");

        foreach(var category in _posingService.BoneCategories.Categories)
        {
            switch(category.Type)
            {
                case BoneCategoryTypes.Category:
                    _subCategories.Add(category.Id, new SubCategory([.. category.Bones], true));
                    break;
                case BoneCategoryTypes.Filter:
                    _allowedCategories.Add(category.Id);
                    break;
            }
        }

        BuildBonePrefixMapping();
        UpdateCachedProperties();
    }

    private void BuildBonePrefixMapping()
    {
        _bonePrefixToCategories.Clear();

        foreach(var category in AllCategories)
        {
            if(category.Type != BoneCategoryTypes.Filter)
                continue;

            foreach(var bonePrefix in category.Bones)
            {
                if(!_bonePrefixToCategories.TryGetValue(bonePrefix, out var categories))
                {
                    categories = [];
                    _bonePrefixToCategories[bonePrefix] = categories;
                }
                categories.Add(category.Id);
            }
        }
    }

    private void UpdateCachedProperties()
    {
        _weaponsAllowed = _allowedCategories.Contains("weapon");
        _otherAllowed = _allowedCategories.Contains("other");
        _ornamentsAllowed = _allowedCategories.Contains("ornament");
        _propAllowed = _allowedCategories.Contains("prop");
    }

    private void InvalidateCache()
    {
        _cache.Clear();

        UpdateCachedProperties();
    }

    public bool IsBoneValid(Bone bone, PoseInfoSlot slot, bool considerHidden = false)
    {
        var cacheKey = (bone.Name, slot, considerHidden);

        if(_cache.TryGetValue(cacheKey, out bool cachedResult))
        {
            return cachedResult;
        }

        bool result = IsBoneValidUncached(bone, slot, considerHidden);

        _cache[cacheKey] = result;
        return result;
    }

    private bool IsBoneValidUncached(Bone bone, PoseInfoSlot slot, bool considerHidden)
    {
        if(bone.IsHidden && !considerHidden)
            return false;

        // Look for excludes 
        foreach(var excluded in _excludedPrefixes)
        {
            if(bone.Name.StartsWith(excluded))
                return false;
        }

        if(slot is PoseInfoSlot.MainHand or PoseInfoSlot.OffHand)
            return _weaponsAllowed;

        if(slot is PoseInfoSlot.Ornament)
            return _ornamentsAllowed;

        if(slot is PoseInfoSlot.Prop)
            return _propAllowed;

        bool foundBone = false;

        foreach(var (bonePrefix, categoryIds) in _bonePrefixToCategories)
        {
            if(bone.Name.StartsWith(bonePrefix))
            {
                foundBone = true;

                // Check if any of the categories this bone belongs to is allowed
                foreach(var categoryId in categoryIds)
                {
                    if(_allowedCategories.Contains(categoryId))
                        return true;
                }
            }
        }

        // If we didn't find a bone, and the "other" category is visible, we should display it
        if(!foundBone && _otherAllowed)
            return true;

        return false;
    }

    public bool WeaponsAllowed => _weaponsAllowed;
    public bool OtherAllowed => _otherAllowed;
    public bool OrnamentsAllowed => _ornamentsAllowed;
    public bool PropAllowed => _propAllowed;

    public bool IsSubCategoryEnabled(string id) => _subCategories[id].Enabled;
    public bool IsCategoryEnabled(string id) => _allowedCategories.Contains(id);
    public bool IsCategoryEnabled(BoneCategory category) => _allowedCategories.Contains(category.Id);


    public void DisableSubCategory(string id)
    {
        var bones = _subCategories[id];
        foreach(var category in bones.Categories)
        {
            _allowedCategories.Remove(category);
        }
        bones.Enabled = false;

        InvalidateCache();
    }

    public void EnableSubCategory(string id)
    {
        var bones = _subCategories[id];
        foreach(var category in bones.Categories)
        {
            _allowedCategories.Add(category);
        }
        bones.Enabled = true;

        InvalidateCache();
    }

    public void DisableCategory(string id)
    {
        _allowedCategories.Remove(id);

        InvalidateCache();
    }

    public void DisableCategory(BoneCategory category)
    {
        _allowedCategories.Remove(category.Id);

        InvalidateCache();
    }

    public void EnableCategory(string id)
    {
        _allowedCategories.Add(id);

        InvalidateCache();
    }

    public void EnableCategory(BoneCategory category)
    {
        _allowedCategories.Add(category.Id);

        InvalidateCache();
    }

    public void AddExcludedPrefix(string bonePrefix)
    {
        _excludedPrefixes.Add(bonePrefix);

        InvalidateCache();
    }

    public void EnableAll()
    {
        _allowedCategories.Clear();
        foreach(var category in AllCategories)
            _allowedCategories.Add(category.Id);

        InvalidateCache();
    }

    public void DisableAll()
    {
        _allowedCategories.Clear();

        InvalidateCache();
    }

    public void EnableOnly(string id)
    {
        _allowedCategories.Clear();
        _allowedCategories.Add(id);

        InvalidateCache();
    }

    public void EnableOnly(BoneCategory category)
    {
        _allowedCategories.Clear();
        _allowedCategories.Add(category.Id);

        InvalidateCache();
    }

}
