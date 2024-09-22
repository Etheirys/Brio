using Brio.Game.Actor.Extensions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
using DalamudCharacter = Dalamud.Game.ClientState.Objects.Types.ICharacter;

namespace Brio.Game.Actor.Appearance;

internal struct ActorAppearance()
{
    public int ModelCharaId;
    public byte Facewear;
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

        fixed(EquipmentModelId* slot = native->DrawData.EquipmentModelIds)
        {
            actorAppearance.Equipment = *(ActorEquipment*)slot;
        }

        actorAppearance.Facewear = character.BrioDrawData()->Facewear;

        actorAppearance.Customize = *(ActorCustomize*)&native->DrawData.CustomizeData;

        actorAppearance.Runtime.IsHatHidden = native->DrawData.IsHatHidden;
        actorAppearance.Runtime.IsVisorToggled = native->DrawData.IsVisorToggled;

        actorAppearance.Runtime.IsMainHandHidden = character.GetWeaponDrawObjectData(ActorEquipSlot.MainHand)->IsHidden;
        actorAppearance.Runtime.IsOffHandHidden = character.GetWeaponDrawObjectData(ActorEquipSlot.OffHand)->IsHidden;

        actorAppearance.ExtendedAppearance.Transparency = native->Alpha;

        var charaBase = character.GetCharacterBase();
        if(charaBase != null)
        {
            actorAppearance.ExtendedAppearance.CharacterTint = charaBase->Tint;
            actorAppearance.ExtendedAppearance.Wetness = charaBase->Wetness;
            actorAppearance.ExtendedAppearance.WetnessDepth = charaBase->WetnessDepth;
        
            actorAppearance.ExtendedAppearance.HeightMultiplier = charaBase->ScaleFactor2;
        }

        charaBase = character.GetWeaponCharacterBase(ActorEquipSlot.MainHand);
        if(charaBase != null)
            actorAppearance.ExtendedAppearance.MainHandTint = charaBase->Tint;

        charaBase = character.GetWeaponCharacterBase(ActorEquipSlot.OffHand);
        if(charaBase != null)
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


        if(npc.BNpcCustomize.Row != 0 && npc.BNpcCustomize.Value != null)
        {
            var customize = npc.BNpcCustomize.Value!;

            actorAppearance.Customize.Race = (Races)customize.Race.Row;
            actorAppearance.Customize.Gender = (Genders)customize.Gender;
            actorAppearance.Customize.BodyType = (BodyTypes)customize.BodyType;
            actorAppearance.Customize.Tribe = (Tribes)customize.Tribe.Row;
            actorAppearance.Customize.Height = customize.Height;
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

        if(npc.NpcEquip.Row != 0 && npc.NpcEquip.Value != null)
        {
            var (mainHand, offHand, equipment) = FromNpcEquip(npc.NpcEquip.Value!);
            actorAppearance.Weapons.MainHand = mainHand;
            actorAppearance.Weapons.OffHand = offHand;
            actorAppearance.Equipment = equipment;
        }

        // TODO: Can NPCs have facewear?
        actorAppearance.Facewear = 0;

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
        actorAppearance.Customize.Height = npc.Height;
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


        if(npc.NpcEquip.Row != 0 && npc.NpcEquip.Value != null)
        {
            var (mainHand, offHand, equipment) = FromNpcEquip(npc.NpcEquip.Value!);
            actorAppearance.Weapons.MainHand = mainHand;
            actorAppearance.Weapons.OffHand = offHand;
            actorAppearance.Equipment = equipment;
        }

        if(npc.ModelMainHand != 0)
            actorAppearance.Weapons.MainHand.Value = npc.ModelMainHand;

        if(npc.DyeMainHand.Row != 0)
        {
            actorAppearance.Weapons.MainHand.Stain0 = (byte)npc.DyeMainHand.Row;
            actorAppearance.Weapons.MainHand.Stain1 = (byte)npc.Dye2MainHand.Row;
        }

        if(npc.ModelOffHand != 0)
            actorAppearance.Weapons.OffHand.Value = npc.ModelOffHand;

        if(npc.DyeOffHand.Row != 0)
        {
            actorAppearance.Weapons.OffHand.Stain0 = (byte)npc.DyeOffHand.Row;
            actorAppearance.Weapons.OffHand.Stain1 = (byte)npc.Dye2OffHand.Row;
        }

        if(npc.ModelHead != 0)
            actorAppearance.Equipment.Head.Value = npc.ModelHead;

        if(npc.DyeHead.Row != 0)
        {
            actorAppearance.Equipment.Head.Stain0 = (byte)npc.DyeHead.Row;
            actorAppearance.Equipment.Head.Stain1 = (byte)npc.Dye2Head.Row;
        }

        if(npc.ModelBody != 0)
            actorAppearance.Equipment.Top.Value = npc.ModelBody;

        if(npc.DyeBody.Row != 0)
        {
            actorAppearance.Equipment.Top.Stain0 = (byte)npc.DyeBody.Row;
            actorAppearance.Equipment.Top.Stain1 = (byte)npc.Dye2Body.Row;
        }

        if(npc.ModelHands != 0)
            actorAppearance.Equipment.Arms.Value = npc.ModelHands;

        if(npc.DyeHands.Row != 0)
        {
            actorAppearance.Equipment.Arms.Stain0 = (byte)npc.DyeHands.Row;
            actorAppearance.Equipment.Arms.Stain1 = (byte)npc.Dye2Hands.Row;
        }

        if(npc.ModelLegs != 0)
            actorAppearance.Equipment.Legs.Value = npc.ModelLegs;

        if(npc.DyeLegs.Row != 0)
        {
            actorAppearance.Equipment.Legs.Stain0 = (byte)npc.DyeLegs.Row;
            actorAppearance.Equipment.Legs.Stain1 = (byte)npc.Dye2Legs.Row;
        }

        if(npc.ModelFeet != 0)
            actorAppearance.Equipment.Feet.Value = npc.ModelFeet;

        if(npc.DyeFeet.Row != 0)
        {
            actorAppearance.Equipment.Feet.Stain0 = (byte)npc.DyeFeet.Row;
            actorAppearance.Equipment.Feet.Stain1 = (byte)npc.Dye2Feet.Row;
        }

        if(npc.ModelEars != 0)
            actorAppearance.Equipment.Ear.Value = npc.ModelEars;

        if(npc.DyeEars.Row != 0)
        {
            actorAppearance.Equipment.Ear.Stain0 = (byte)npc.DyeEars.Row;
            actorAppearance.Equipment.Ear.Stain1 = (byte)npc.Dye2Ears.Row;
        }

        if(npc.ModelNeck != 0)
            actorAppearance.Equipment.Neck.Value = npc.ModelNeck;

        if(npc.DyeNeck.Row != 0)
        {
            actorAppearance.Equipment.Neck.Stain0 = (byte)npc.DyeNeck.Row;
            actorAppearance.Equipment.Neck.Stain1 = (byte)npc.Dye2Neck.Row;
        }

        if(npc.ModelWrists != 0)
            actorAppearance.Equipment.Wrist.Value = npc.ModelWrists;

        if(npc.DyeWrists.Row != 0)
        {
            actorAppearance.Equipment.Wrist.Stain0 = (byte)npc.DyeWrists.Row;
            actorAppearance.Equipment.Wrist.Stain1 = (byte)npc.Dye2Wrists.Row;
        }

        if(npc.ModelRightRing != 0)
            actorAppearance.Equipment.RFinger.Value = npc.ModelRightRing;

        if(npc.DyeRightRing.Row != 0)
        {
            actorAppearance.Equipment.RFinger.Stain0 = (byte)npc.DyeRightRing.Row;
            actorAppearance.Equipment.RFinger.Stain1 = (byte)npc.Dye2RightRing.Row;
        }

        if(npc.ModelLeftRing != 0)
            actorAppearance.Equipment.LFinger.Value = npc.ModelLeftRing;

        if(npc.DyeLeftRing.Row != 0)
        {
            actorAppearance.Equipment.LFinger.Stain0 = (byte)npc.DyeLeftRing.Row;
            actorAppearance.Equipment.LFinger.Stain1 = (byte)npc.Dye2LeftRing.Row;
        }

        // TODO: Can NPCs have facewear?
        actorAppearance.Facewear = 0;

        return actorAppearance;
    }

    private static (WeaponModelId, WeaponModelId, ActorEquipment) FromNpcEquip(NpcEquip npcEquip)
    {
        var equipment = new ActorEquipment();
        var mainHand = new WeaponModelId();
        var offHand = new WeaponModelId();

        equipment.Head.Value = npcEquip.ModelHead;
        equipment.Head.Stain0 = (byte)npcEquip.DyeHead.Row;
        equipment.Head.Stain1 = (byte)npcEquip.Dye2Head.Row;
        equipment.Top.Value = npcEquip.ModelBody;
        equipment.Top.Stain0 = (byte)npcEquip.DyeBody.Row;
        equipment.Top.Stain1 = (byte)npcEquip.Dye2Body.Row;
        equipment.Arms.Value = npcEquip.ModelHands;
        equipment.Arms.Stain0 = (byte)npcEquip.DyeHands.Row;
        equipment.Arms.Stain1 = (byte)npcEquip.Dye2Hands.Row;
        equipment.Legs.Value = npcEquip.ModelLegs;
        equipment.Legs.Stain0 = (byte)npcEquip.DyeLegs.Row;
        equipment.Legs.Stain1 = (byte)npcEquip.Dye2Legs.Row;
        equipment.Feet.Value = npcEquip.ModelFeet;
        equipment.Feet.Stain0 = (byte)npcEquip.DyeFeet.Row;
        equipment.Feet.Stain1 = (byte)npcEquip.Dye2Feet.Row;
        equipment.Ear.Value = npcEquip.ModelEars;
        equipment.Ear.Stain0 = (byte)npcEquip.DyeEars.Row;
        equipment.Ear.Stain1 = (byte)npcEquip.Dye2Ears.Row;
        equipment.Neck.Value = npcEquip.ModelNeck;
        equipment.Neck.Stain0 = (byte)npcEquip.DyeNeck.Row;
        equipment.Neck.Stain1 = (byte)npcEquip.Dye2Neck.Row;
        equipment.Wrist.Value = npcEquip.ModelWrists;
        equipment.Wrist.Stain0 = (byte)npcEquip.DyeWrists.Row;
        equipment.Wrist.Stain1 = (byte)npcEquip.Dye2Wrists.Row;
        equipment.RFinger.Value = npcEquip.ModelRightRing;
        equipment.RFinger.Stain0 = (byte)npcEquip.DyeRightRing.Row;
        equipment.RFinger.Stain1 = (byte)npcEquip.Dye2RightRing.Row;
        equipment.LFinger.Value = npcEquip.ModelLeftRing;
        equipment.LFinger.Stain0 = (byte)npcEquip.DyeLeftRing.Row;
        equipment.LFinger.Stain1 = (byte)npcEquip.Dye2LeftRing.Row;

        mainHand.Value = npcEquip.ModelMainHand;
        mainHand.Stain0 = (byte)npcEquip.DyeMainHand.Row;
        mainHand.Stain1 = (byte)npcEquip.Dye2MainHand.Row;
        offHand.Value = npcEquip.ModelOffHand;
        offHand.Stain0 = (byte)npcEquip.DyeOffHand.Row;
        offHand.Stain1 = (byte)npcEquip.Dye2OffHand.Row;

        return (mainHand, offHand, equipment);
    }
}
