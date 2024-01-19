using Lumina.Excel.GeneratedSheets;

namespace Brio.Game.Actor.Appearance;

internal static class AppearanceExtensions
{
    public static Tribes[] GetValidTribes(this Races race)
    {
        var firstValid = race.GetFirstValidTribe();
        return
        [
            firstValid,
            (Tribes)((byte)firstValid + 1)
        ];
    }

    public static Tribes GetFirstValidTribe(this Races race) => (Tribes)((byte)race * 2 - 1);

    public static Races GetRace(this Tribes tribe) => (Races)(((byte)tribe + 1) / 2);

    public static Genders[] GetAllowedGenders(this Races race)
    {
        if(race == Races.Hrothgar)
            return [Genders.Masculine];

        return [Genders.Masculine, Genders.Feminine];
    }

    public static BodyTypes[] GetAllowedBodyTypes(this Tribes tribe, Genders gender)
    {
        var race = tribe.GetRace();

        if(tribe == Tribes.Midlander)
            return [BodyTypes.Normal, BodyTypes.Old, BodyTypes.Young];

        if(race == Races.Elezen)
            return [BodyTypes.Normal, BodyTypes.Old, BodyTypes.Young];

        if(race == Races.AuRa)
            return [BodyTypes.Normal, BodyTypes.Old, BodyTypes.Young];

        if(race == Races.Miqote && gender == Genders.Feminine)
            return [BodyTypes.Normal, BodyTypes.Young];

        return [BodyTypes.Normal];
    }

    public static Genders[] GetAllowedGenders(this Tribes tribe) => GetAllowedGenders(tribe.GetRace());


    public static ActorEquipSlot GetEquipSlots(this EquipSlotCategory category)
    {
        ActorEquipSlot result = ActorEquipSlot.None;

        if(category.MainHand == 1)
            result |= ActorEquipSlot.MainHand;

        if(category.OffHand == 1)
            result |= ActorEquipSlot.OffHand;

        if(category.Head == 1)
            result |= ActorEquipSlot.Head;

        if(category.Body == 1)
            result |= ActorEquipSlot.Body;

        if(category.Gloves == 1)
            result |= ActorEquipSlot.Hands;

        if(category.Legs == 1)
            result |= ActorEquipSlot.Legs;

        if(category.Feet == 1)
            result |= ActorEquipSlot.Feet;

        if(category.Ears == 1)
            result |= ActorEquipSlot.Ears;

        if(category.Neck == 1)
            result |= ActorEquipSlot.Neck;

        if(category.Wrists == 1)
            result |= ActorEquipSlot.Wrists;

        if(category.FingerR == 1)
            result |= ActorEquipSlot.RightRing;

        if(category.FingerL == 1)
            result |= ActorEquipSlot.LeftRing;

        return result;
    }

    public static string GetEquipSlotFallback(this ActorEquipSlot slot)
    {
        if(slot.HasFlag(ActorEquipSlot.All) || slot.HasFlag(ActorEquipSlot.Armor) || slot.HasFlag(ActorEquipSlot.AllButWeapons))
            return "Images.Body.png";

        if(slot.HasFlag(ActorEquipSlot.Weapons))
            return "Images.MainHand.png";

        if(slot.HasFlag(ActorEquipSlot.Accessories))
            return "Images.Ears.png";

        if(slot.HasFlag(ActorEquipSlot.MainHand))
            return "Images.MainHand.png";

        if(slot.HasFlag(ActorEquipSlot.OffHand))
            return "Images.OffHand.png";

        if(slot.HasFlag(ActorEquipSlot.Head))
            return "Images.Head.png";

        if(slot.HasFlag(ActorEquipSlot.Body))
            return "Images.Body.png";

        if(slot.HasFlag(ActorEquipSlot.Hands))
            return "Images.Hands.png";

        if(slot.HasFlag(ActorEquipSlot.Legs))
            return "Images.Legs.png";

        if(slot.HasFlag(ActorEquipSlot.Feet))
            return "Images.Feet.png";

        if(slot.HasFlag(ActorEquipSlot.Ears))
            return "Images.Ears.png";

        if(slot.HasFlag(ActorEquipSlot.Neck))
            return "Images.Neck.png";

        if(slot.HasFlag(ActorEquipSlot.Wrists))
            return "Images.Wrists.png";

        if(slot.HasFlag(ActorEquipSlot.RightRing))
            return "Images.Ring.png";

        if(slot.HasFlag(ActorEquipSlot.LeftRing))
            return "Images.Ring.png";

        return "Images.UnknownIcon.png";
    }
}
