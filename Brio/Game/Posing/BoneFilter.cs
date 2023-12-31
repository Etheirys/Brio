using Brio.Game.Posing.Skeletons;
using System.Collections.Generic;
using System.Linq;
using static Brio.Game.Posing.BoneCategories;

namespace Brio.Game.Posing;

internal class BoneFilter
{
    private readonly PosingService _posingService;

    private readonly HashSet<string> _allowedCategories = [];

    public IReadOnlyList<BoneCategory> AllCategories => _posingService.BoneCategories.Categories;

    public BoneFilter(PosingService posingService)
    {
        _posingService = posingService;

        foreach (var category in _posingService.BoneCategories.Categories)
            _allowedCategories.Add(category.Id);
    }

    public unsafe bool IsBoneValid(Bone bone, PoseInfoSlot slot, bool considerHidden = false)
    {
        bool foundBone = false;

        if (bone.IsHidden && !considerHidden)
            return false;

        // Weapon bone names don't matter
        if (slot == PoseInfoSlot.MainHand || slot == PoseInfoSlot.OffHand)
            if (WeaponsAllowed)
                return true;
            else
                return false;

        // Check if the bone is in any of the categories and that category is visible
        foreach (var category in AllCategories)
        {
            if (category.Type != BoneCategoryTypes.Filter)
                continue;

            foreach (var boneName in category.Bones)
            {
                if (bone.Name.StartsWith(boneName))
                {
                    foundBone = true;

                    if (_allowedCategories.Any(x => category.Id == x))
                        return true;
                }
            }
        }

        // If we didn't find a bone, and the "other" category is visible, we should display it
        if (!foundBone && OtherAllowed)
            return true;

        return false;
    }

    public bool WeaponsAllowed => _allowedCategories.Any((x) => x == "weapon");

    public bool OtherAllowed => _allowedCategories.Any((x) => x == "other");

    public bool IsCategoryEnabled(string id) => _allowedCategories.Any((x) => x == id);
    public bool IsCategoryEnabled(BoneCategory category) => _allowedCategories.Any(x => category.Id == x);

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

    public void EnableAll()
    {
        _allowedCategories.Clear();
        foreach (var category in AllCategories)
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
