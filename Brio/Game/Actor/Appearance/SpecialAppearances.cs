using Brio.Resources;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;

namespace Brio.Game.Actor.Appearance;

public static class SpecialAppearances
{
    public static WeaponModelId EmperorsMainHand { get; } = new()
    {
        Id = 301,
        Type = 31,
        Variant = 1
    };
    public static WeaponModelId EmperorsOffHand { get; } = new()
    {
        Id = 351,
        Type = 31,
        Variant = 1
    };
    public static EquipmentModelId Smallclothes { get; } = new()
    {
        Id = 9903,
        Variant = 1
    };

    public static EquipmentModelId EmperorsMainSlotsEquipment { get; } = new()
    {
        Id = 279,
        Variant = 1
    };

    public static EquipmentModelId EmperorsAccessorySlotsEquipment { get; } = new()
    {
        Id = 53,
        Variant = 1
    };

    public static EquipmentModelId None { get; } = new()
    {
        Id = 0,
        Variant = 0
    };

    public static EquipmentModelId InvisibleItem { get; } = new()
    {
        Id = 6121,
        Variant = 12
    };

    public static ENpcBase DefaultHumanEventNpc = GameDataProvider.Instance.ENpcBases[1029275];
}
