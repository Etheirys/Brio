using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Brio.Game.Actor.Extensions;
using Lumina.Excel.GeneratedSheets;
using DalamudCharacter = Dalamud.Game.ClientState.Objects.Types.Character;

namespace Brio.Game.Actor.Appearance;

internal struct ActorAppearance()
{
    public int ModelCharaId;
    public ActorWeapons Weapons = new();
    public ActorEquipment Equipment = new();
    public ActorCustomize Customize = new();
    public ActorRuntimeOptions Runtime = new();
    public ActorExtendedAppearance ExtendedAppearance = new();

    public unsafe static ActorAppearance FromCharacter(DalamudCharacter character)
    {
        var native = character.Native();
        ActorAppearance actorAppearance = new()
        {
            ModelCharaId = native->CharacterData.ModelCharaId
        };

        actorAppearance.Weapons.MainHand = native->DrawData.Weapon(DrawDataContainer.WeaponSlot.MainHand).ModelId;
        actorAppearance.Weapons.OffHand = native->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).ModelId;

        actorAppearance.Equipment = *(ActorEquipment*)&native->DrawData.Head;

        actorAppearance.Customize = *(ActorCustomize*)&native->DrawData.CustomizeData;

        actorAppearance.Runtime.IsHatHidden = native->DrawData.IsHatHidden;
        actorAppearance.Runtime.IsVisorToggled = native->DrawData.IsVisorToggled;

        actorAppearance.Runtime.IsMainHandHidden = character.GetWeaponDrawObjectData(ActorEquipSlot.MainHand)->IsHidden;
        actorAppearance.Runtime.IsOffHandHidden = character.GetWeaponDrawObjectData(ActorEquipSlot.OffHand)->IsHidden;

        actorAppearance.ExtendedAppearance.Transparency = native->Alpha;

        var charaBase = character.GetCharacterBase();
        if (charaBase != null)
        {
            actorAppearance.ExtendedAppearance.CharacterTint = charaBase->Tint;
            actorAppearance.ExtendedAppearance.Wetness = charaBase->CharacterBase.SwimmingWetness;
            actorAppearance.ExtendedAppearance.WetnessDepth = charaBase->CharacterBase.WetnessDepth;
        }

        charaBase = character.GetWeaponCharacterBase(ActorEquipSlot.MainHand);
        if (charaBase != null)
            actorAppearance.ExtendedAppearance.MainHandTint = charaBase->Tint;

        charaBase = character.GetWeaponCharacterBase(ActorEquipSlot.OffHand);
        if (charaBase != null)
            actorAppearance.ExtendedAppearance.OffHandTint = charaBase->Tint;

        return actorAppearance;
    }

    public static ActorAppearance FromModelChara(int modelId)
    {
        ActorAppearance actorAppearance = new()
        {
            ModelCharaId = modelId
        };
        return actorAppearance;
    }

    public static ActorAppearance FromBNpc(BNpcBase npc)
    {
        ActorAppearance actorAppearance = new()
        {
            ModelCharaId = (int)npc.ModelChara.Row
        };


        if (npc.BNpcCustomize.Row != 0 && npc.BNpcCustomize.Value != null)
        {
            var customize = npc.BNpcCustomize.Value!;

            actorAppearance.Customize.Race = (Races)customize.Race.Row;
            actorAppearance.Customize.Gender = (Genders)customize.Gender;
            actorAppearance.Customize.BodyType = (BodyTypes)customize.BodyType;
            actorAppearance.Customize.Tribe = (Tribes)customize.Tribe.Row;
            actorAppearance.Customize.FaceType = customize.Face;
            actorAppearance.Customize.HairStyle = customize.HairStyle;
            actorAppearance.Customize.HasHighlights = customize.HairHighlight;
            actorAppearance.Customize.SkinTone = customize.SkinColor;
            actorAppearance.Customize.REyeColor = customize.EyeHeterochromia;
            actorAppearance.Customize.HairColor = customize.HairColor;
            actorAppearance.Customize.HairHighlightColor = customize.HairHighlightColor;
            actorAppearance.Customize.FaceFeatures = (FacialFeature)customize.FacialFeature;
            actorAppearance.Customize.FaceFeaturesColor = customize.FacialFeatureColor;
            actorAppearance.Customize.Eyebrows = customize.Eyebrows;
            actorAppearance.Customize.LEyeColor = customize.EyeColor;
            actorAppearance.Customize.EyeShape = customize.EyeShape;
            actorAppearance.Customize.NoseShape = customize.Nose;
            actorAppearance.Customize.JawShape = customize.Jaw;
            actorAppearance.Customize.LipStyle = customize.Mouth;
            actorAppearance.Customize.LipColor = customize.LipColor;
            actorAppearance.Customize.RaceFeatureSize = customize.BustOrTone1;
            actorAppearance.Customize.RaceFeatureType = customize.ExtraFeature1;
            actorAppearance.Customize.BustSize = customize.ExtraFeature2OrBust;
            actorAppearance.Customize.Facepaint = customize.FacePaint;
            actorAppearance.Customize.FacePaintColor = customize.FacePaintColor;
        }

        if (npc.NpcEquip.Row != 0 && npc.NpcEquip.Value != null)
        {
            var (mainHand, offHand, equipment) = FromNpcEquip(npc.NpcEquip.Value!);
            actorAppearance.Weapons.MainHand = mainHand;
            actorAppearance.Weapons.OffHand = offHand;
            actorAppearance.Equipment = equipment;
        }

        return actorAppearance;
    }

    public static ActorAppearance FromENpc(ENpcBase npc)
    {
        ActorAppearance actorAppearance = new()
        {
            ModelCharaId = (int)npc.ModelChara.Row
        };

        actorAppearance.Customize.Race = (Races)npc.Race.Row;
        actorAppearance.Customize.Gender = (Genders)npc.Gender;
        actorAppearance.Customize.BodyType = (BodyTypes)npc.BodyType;
        actorAppearance.Customize.Tribe = (Tribes)npc.Tribe.Row;
        actorAppearance.Customize.FaceType = npc.Face;
        actorAppearance.Customize.HairStyle = npc.HairStyle;
        actorAppearance.Customize.HasHighlights = npc.HairHighlight;
        actorAppearance.Customize.SkinTone = npc.SkinColor;
        actorAppearance.Customize.REyeColor = npc.EyeHeterochromia;
        actorAppearance.Customize.HairColor = npc.HairColor;
        actorAppearance.Customize.HairHighlightColor = npc.HairHighlightColor;
        actorAppearance.Customize.FaceFeatures = (FacialFeature)npc.FacialFeature;
        actorAppearance.Customize.FaceFeaturesColor = npc.FacialFeatureColor;
        actorAppearance.Customize.Eyebrows = npc.Eyebrows;
        actorAppearance.Customize.LEyeColor = npc.EyeColor;
        actorAppearance.Customize.EyeShape = npc.EyeShape;
        actorAppearance.Customize.NoseShape = npc.Nose;
        actorAppearance.Customize.JawShape = npc.Jaw;
        actorAppearance.Customize.LipStyle = npc.Mouth;
        actorAppearance.Customize.LipColor = npc.LipColor;
        actorAppearance.Customize.RaceFeatureSize = npc.BustOrTone1;
        actorAppearance.Customize.RaceFeatureType = npc.ExtraFeature1;
        actorAppearance.Customize.BustSize = npc.ExtraFeature2OrBust;
        actorAppearance.Customize.Facepaint = npc.FacePaint;
        actorAppearance.Customize.FacePaintColor = npc.FacePaintColor;


        if (npc.NpcEquip.Row != 0 && npc.NpcEquip.Value != null)
        {
            var (mainHand, offHand, equipment) = FromNpcEquip(npc.NpcEquip.Value!);
            actorAppearance.Weapons.MainHand = mainHand;
            actorAppearance.Weapons.OffHand = offHand;
            actorAppearance.Equipment = equipment;
        }

        if (npc.ModelMainHand != 0)
            actorAppearance.Weapons.MainHand.Value = npc.ModelMainHand;

        if (npc.DyeMainHand.Row != 0)
            actorAppearance.Weapons.MainHand.Stain = (byte)npc.DyeMainHand.Row;

        if (npc.ModelOffHand != 0)
            actorAppearance.Weapons.OffHand.Value = npc.ModelOffHand;

        if (npc.DyeOffHand.Row != 0)
            actorAppearance.Weapons.OffHand.Stain = (byte)npc.DyeOffHand.Row;

        if (npc.ModelHead != 0)
            actorAppearance.Equipment.Head.Value = npc.ModelHead;

        if (npc.DyeHead.Row != 0)
            actorAppearance.Equipment.Head.Stain = (byte)npc.DyeHead.Row;

        if (npc.ModelBody != 0)
            actorAppearance.Equipment.Top.Value = npc.ModelBody;

        if (npc.DyeBody.Row != 0)
            actorAppearance.Equipment.Top.Stain = (byte)npc.DyeBody.Row;

        if (npc.ModelHands != 0)
            actorAppearance.Equipment.Arms.Value = npc.ModelHands;

        if (npc.DyeHands.Row != 0)
            actorAppearance.Equipment.Arms.Stain = (byte)npc.DyeHands.Row;

        if (npc.ModelLegs != 0)
            actorAppearance.Equipment.Legs.Value = npc.ModelLegs;

        if (npc.DyeLegs.Row != 0)
            actorAppearance.Equipment.Legs.Stain = (byte)npc.DyeLegs.Row;

        if (npc.ModelFeet != 0)
            actorAppearance.Equipment.Feet.Value = npc.ModelFeet;

        if (npc.DyeFeet.Row != 0)
            actorAppearance.Equipment.Feet.Stain = (byte)npc.DyeFeet.Row;

        if (npc.ModelEars != 0)
            actorAppearance.Equipment.Ear.Value = npc.ModelEars;

        if (npc.DyeEars.Row != 0)
            actorAppearance.Equipment.Ear.Stain = (byte)npc.DyeEars.Row;

        if (npc.ModelNeck != 0)
            actorAppearance.Equipment.Neck.Value = npc.ModelNeck;

        if (npc.DyeNeck.Row != 0)
            actorAppearance.Equipment.Neck.Stain = (byte)npc.DyeNeck.Row;

        if (npc.ModelWrists != 0)
            actorAppearance.Equipment.Wrist.Value = npc.ModelWrists;

        if (npc.DyeWrists.Row != 0)
            actorAppearance.Equipment.Wrist.Stain = (byte)npc.DyeWrists.Row;

        if (npc.ModelRightRing != 0)
            actorAppearance.Equipment.RFinger.Value = npc.ModelRightRing;

        if (npc.DyeRightRing.Row != 0)
            actorAppearance.Equipment.RFinger.Stain = (byte)npc.DyeRightRing.Row;

        if (npc.ModelLeftRing != 0)
            actorAppearance.Equipment.LFinger.Value = npc.ModelLeftRing;

        if (npc.DyeLeftRing.Row != 0)
            actorAppearance.Equipment.LFinger.Stain = (byte)npc.DyeLeftRing.Row;


        return actorAppearance;
    }

    private static (WeaponModelId, WeaponModelId, ActorEquipment) FromNpcEquip(NpcEquip npcEquip)
    {
        var equipment = new ActorEquipment();
        var mainHand = new WeaponModelId();
        var offHand = new WeaponModelId();

        equipment.Head.Value = npcEquip.ModelHead;
        equipment.Head.Stain = (byte)npcEquip.DyeHead.Row;
        equipment.Top.Value = npcEquip.ModelBody;
        equipment.Top.Stain = (byte)npcEquip.DyeBody.Row;
        equipment.Arms.Value = npcEquip.ModelHands;
        equipment.Arms.Stain = (byte)npcEquip.DyeHands.Row;
        equipment.Legs.Value = npcEquip.ModelLegs;
        equipment.Legs.Stain = (byte)npcEquip.DyeLegs.Row;
        equipment.Feet.Value = npcEquip.ModelFeet;
        equipment.Feet.Stain = (byte)npcEquip.DyeFeet.Row;
        equipment.Ear.Value = npcEquip.ModelEars;
        equipment.Ear.Stain = (byte)npcEquip.DyeEars.Row;
        equipment.Neck.Value = npcEquip.ModelNeck;
        equipment.Neck.Stain = (byte)npcEquip.DyeNeck.Row;
        equipment.Wrist.Value = npcEquip.ModelWrists;
        equipment.Wrist.Stain = (byte)npcEquip.DyeWrists.Row;
        equipment.RFinger.Value = npcEquip.ModelRightRing;
        equipment.RFinger.Stain = (byte)npcEquip.DyeRightRing.Row;
        equipment.LFinger.Value = npcEquip.ModelLeftRing;
        equipment.LFinger.Stain = (byte)npcEquip.DyeLeftRing.Row;

        mainHand.Value = npcEquip.ModelMainHand;
        mainHand.Stain = (byte)npcEquip.DyeMainHand.Row;
        offHand.Value = npcEquip.ModelOffHand;
        offHand.Stain = (byte)npcEquip.DyeOffHand.Row;

        return (mainHand, offHand, equipment);
    }
}