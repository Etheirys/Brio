using Brio.Game.Posing.Skeletons;
using System.Collections.Generic;
using System.Linq;
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

    public IReadOnlyList<BoneCategory> AllCategories => _posingService.BoneCategories.Categories;

    public BoneFilter(PosingService posingService)
    {
        _posingService = posingService;

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
    }

    public unsafe bool IsBoneValid(Bone bone, PoseInfoSlot slot, bool considerHidden = false)
    {
        bool foundBone = false;

        if(bone.IsHidden && !considerHidden)
            return false;

        // Look for excludes
        foreach(var excluded in _excludedPrefixes)
        {
            if(bone.Name.StartsWith(excluded))
                return false;
        }

        // Weapon bone names don't matter
        if(slot is PoseInfoSlot.MainHand or PoseInfoSlot.OffHand)
            return WeaponsAllowed;

        if(slot is PoseInfoSlot.Ornament)
            return OrnamentsAllowed;
       
        if(slot is PoseInfoSlot.Prop)
            return PropAllowed;

        // Check if the bone is in any of the categories and that category is visible
        foreach(var category in AllCategories)
        {
            if(category.Type != BoneCategoryTypes.Filter)
                continue;

            foreach(var boneName in category.Bones)
            {
                if(bone.Name.StartsWith(boneName))
                {
                    foundBone = true;

                    if(_allowedCategories.Any(x => category.Id == x))
                        return true;
                }
            }
        }

        // If we didn't find a bone, and the "other" category is visible, we should display it
        if(!foundBone && OtherAllowed)
            return true;

        return false;
    }

    public bool WeaponsAllowed => _allowedCategories.Any((x) => x == "weapon");

    public bool OtherAllowed => _allowedCategories.Any((x) => x == "other");

    public bool OrnamentsAllowed =>  _allowedCategories.Any((x) => x == "ornament");

    public bool PropAllowed => _allowedCategories.Any((x) => x == "prop");

    public bool IsSubCategoryEnabled(string id) => _subCategories[id].Enabled;

    public bool IsCategoryEnabled(string id) => _allowedCategories.Any((x) => x == id);
    public bool IsCategoryEnabled(BoneCategory category) => _allowedCategories.Any(x => category.Id == x);


    public void DisableSubCategory(string id)
    {
        var bones = _subCategories[id];
        foreach(var category in bones.Categories)
        {
            _allowedCategories.Remove(category);
        }
        bones.Enabled = false;
    }

    public void EnableSubCategory(string id)
    {
        var bones = _subCategories[id];
        foreach(var category in bones.Categories)
        {
            _allowedCategories.Add(category);
        }
        bones.Enabled = true;
    }

    public void DisableCategory(string id)
    {
        _allowedCategories.Remove(id);
    }

    public void DisableCategory(BoneCategory category)
    {
        _allowedCategories.Remove(category.Id);
    }

    public void EnableCategory(string id)
    {
        _allowedCategories.Add(id);
    }

    public void EnableCategory(BoneCategory category)
    {
        _allowedCategories.Add(category.Id);
    }

    public void AddExcludedPrefix(string bonePrefix)
    {
        _excludedPrefixes.Add(bonePrefix);
    }

    public void EnableAll()
    {
        _allowedCategories.Clear();
        foreach(var category in AllCategories)
            _allowedCategories.Add(category.Id);
    }

    public void DisableAll()
    {
        _allowedCategories.Clear();
    }

    public void EnableOnly(string id)
    {
        _allowedCategories.Clear();
        _allowedCategories.Add(id);
    }

    public void EnableOnly(BoneCategory category)
    {
        _allowedCategories.Clear();
        _allowedCategories.Add(category.Id);
    }

}
