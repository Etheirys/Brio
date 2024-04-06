using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Library.Tags;
using Brio.Resources;
using Dalamud.Interface.Internal;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Numerics;

namespace Brio.Files;

internal class AnamnesisCharaFileInfo : AppliableActorFileInfoBase<AnamnesisCharaFile>
{
    public override string Name => "Character File";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Chara.png");
    public override string Extension => ".chara";

    public AnamnesisCharaFileInfo(EntityManager entityManager)
        : base(entityManager)
    {
    }

    protected override void Apply(AnamnesisCharaFile file, ActorEntity actor, bool asExpression = false)
    {
        ActorAppearanceCapability? capability;
        if(actor.TryGetCapability<ActorAppearanceCapability>(out capability) && capability != null)
        {
            _ = capability.SetAppearance(file, AppearanceImportOptions.All);
        }
    }
}

[Serializable]
internal class AnamnesisCharaFile : JsonDocumentBase
{
    public uint ModelType { get; set; } = 0;
    public Races Race { get; set; }
    public Genders Gender { get; set; }
    public BodyTypes Age { get; set; }
    public Tribes Tribe { get; set; }
    public byte Height { get; set; }
    public byte Head { get; set; }
    public byte Hair { get; set; }
    public bool EnableHighlights { get; set; }
    public byte Skintone { get; set; }
    public byte REyeColor { get; set; }
    public byte HairTone { get; set; }
    public byte Highlights { get; set; }
    public FacialFeature FacialFeatures { get; set; }
    public byte LimbalEyes { get; set; }
    public byte Eyebrows { get; set; }
    public byte LEyeColor { get; set; }
    public byte Eyes { get; set; }
    public byte Nose { get; set; }
    public byte Jaw { get; set; }
    public byte Mouth { get; set; }
    public byte LipsToneFurPattern { get; set; }
    public byte EarMuscleTailSize { get; set; }
    public byte TailEarsType { get; set; }
    public byte Bust { get; set; }
    public byte FacePaint { get; set; }
    public byte FacePaintColor { get; set; }
    public WeaponSave MainHand { get; set; }
    public WeaponSave OffHand { get; set; }
    public ItemSave HeadGear { get; set; }
    public ItemSave Body { get; set; }
    public ItemSave Hands { get; set; }
    public ItemSave Legs { get; set; }
    public ItemSave Feet { get; set; }
    public ItemSave Ears { get; set; }
    public ItemSave Neck { get; set; }
    public ItemSave Wrists { get; set; }
    public ItemSave LeftRing { get; set; }
    public ItemSave RightRing { get; set; }
    public Vector3 SkinColor { get; set; }
    public Vector3 SkinGloss { get; set; }
    public Vector3 LeftEyeColor { get; set; }
    public Vector3 RightEyeColor { get; set; }
    public Vector3 LimbalRingColor { get; set; }
    public Vector3 HairColor { get; set; }
    public Vector3 HairGloss { get; set; }
    public Vector3 HairHighlight { get; set; }
    public Vector4 MouthColor { get; set; }
    public Vector3 BustScale { get; set; }
    public float Transparency { get; set; }
    public float MuscleTone { get; set; }
    public float HeightMultiplier { get; set; }

    public override void GetAutoTags(ref TagCollection tags)
    {
        base.GetAutoTags(ref tags);

        tags.Add(this.Race.ToDisplayName());
        tags.Add(this.Gender.ToDisplayName());
        tags.Add(this.Tribe.ToDisplayName());
    }

    public static implicit operator ActorAppearance(AnamnesisCharaFile chara)
    {
        var appearance = new ActorAppearance
        {
            // Model
            ModelCharaId = (int)chara.ModelType
        };

        // Customize
        appearance.Customize.Gender = chara.Gender;
        appearance.Customize.Race = chara.Race;
        appearance.Customize.Tribe = chara.Tribe;
        appearance.Customize.BodyType = chara.Age;
        appearance.Customize.Height = chara.Height;
        appearance.Customize.FaceType = chara.Head;
        appearance.Customize.HairStyle = chara.Hair;
        appearance.Customize.HighlightsEnabled = chara.EnableHighlights;
        appearance.Customize.SkinTone = chara.Skintone;
        appearance.Customize.REyeColor = chara.REyeColor;
        appearance.Customize.HairColor = chara.HairTone;
        appearance.Customize.HairHighlightColor = chara.Highlights;
        appearance.Customize.FaceFeatures = chara.FacialFeatures;
        appearance.Customize.FaceFeaturesColor = chara.LimbalEyes;
        appearance.Customize.Eyebrows = chara.Eyebrows;
        appearance.Customize.LEyeColor = chara.LEyeColor;
        appearance.Customize.EyeShape = chara.Eyes;
        appearance.Customize.NoseShape = chara.Nose;
        appearance.Customize.JawShape = chara.Jaw;
        appearance.Customize.LipStyle = chara.Mouth;
        appearance.Customize.LipColor = chara.LipsToneFurPattern;
        appearance.Customize.RaceFeatureSize = chara.EarMuscleTailSize;
        appearance.Customize.RaceFeatureType = chara.TailEarsType;
        appearance.Customize.BustSize = chara.Bust;
        appearance.Customize.Facepaint = chara.FacePaint;
        appearance.Customize.FacePaintColor = chara.FacePaintColor;

        // Weapons
        appearance.Weapons.MainHand = chara.MainHand;
        appearance.Weapons.OffHand = chara.OffHand;

        // Gear
        appearance.Equipment.Head = chara.HeadGear;
        appearance.Equipment.Top = chara.Body;
        appearance.Equipment.Arms = chara.Hands;
        appearance.Equipment.Legs = chara.Legs;
        appearance.Equipment.Feet = chara.Feet;
        appearance.Equipment.Ear = chara.Ears;
        appearance.Equipment.Neck = chara.Neck;
        appearance.Equipment.Wrist = chara.Wrists;
        appearance.Equipment.LFinger = chara.LeftRing;
        appearance.Equipment.RFinger = chara.RightRing;

        // Extended Appearance
        appearance.ExtendedAppearance.Transparency = chara.Transparency;

        return appearance;
    }

    public static implicit operator AnamnesisCharaFile(ActorAppearance appearance)
    {
        var charaFile = new AnamnesisCharaFile
        {
            // Model
            ModelType = (uint)appearance.ModelCharaId,

            // Customize
            Gender = appearance.Customize.Gender,
            Race = appearance.Customize.Race,
            Tribe = appearance.Customize.Tribe,
            Age = appearance.Customize.BodyType,
            Height = appearance.Customize.Height,
            Head = appearance.Customize.FaceType,
            Hair = appearance.Customize.HairStyle,
            EnableHighlights = appearance.Customize.HighlightsEnabled,
            Skintone = appearance.Customize.SkinTone,
            REyeColor = appearance.Customize.REyeColor,
            HairTone = appearance.Customize.HairColor,
            Highlights = appearance.Customize.HairHighlightColor,
            FacialFeatures = appearance.Customize.FaceFeatures,
            LimbalEyes = appearance.Customize.FaceFeaturesColor,
            Eyebrows = appearance.Customize.Eyebrows,
            LEyeColor = appearance.Customize.LEyeColor,
            Eyes = appearance.Customize.EyeShape,
            Nose = appearance.Customize.NoseShape,
            Jaw = appearance.Customize.JawShape,
            Mouth = appearance.Customize.LipStyle,
            LipsToneFurPattern = appearance.Customize.LipColor,
            EarMuscleTailSize = appearance.Customize.RaceFeatureSize,
            TailEarsType = appearance.Customize.RaceFeatureType,
            Bust = appearance.Customize.BustSize,
            FacePaint = appearance.Customize.Facepaint,
            FacePaintColor = appearance.Customize.FacePaintColor,

            // Weapons
            MainHand = appearance.Weapons.MainHand,
            OffHand = appearance.Weapons.OffHand,

            // Gear
            HeadGear = appearance.Equipment.Head,
            Body = appearance.Equipment.Top,
            Hands = appearance.Equipment.Arms,
            Legs = appearance.Equipment.Legs,
            Feet = appearance.Equipment.Feet,
            Ears = appearance.Equipment.Ear,
            Neck = appearance.Equipment.Neck,
            Wrists = appearance.Equipment.Wrist,
            LeftRing = appearance.Equipment.LFinger,
            RightRing = appearance.Equipment.RFinger,

            // Extended Appearance
            Transparency = appearance.ExtendedAppearance.Transparency
        };

        return charaFile;
    }

    internal struct ItemSave
    {
        public ushort ModelBase { get; set; }
        public byte ModelVariant { get; set; }
        public byte DyeId { get; set; }

        public static implicit operator EquipmentModelId(ItemSave save) => new()
        {
            Id = save.ModelBase,
            Variant = save.ModelVariant,
            Stain = save.DyeId,
        };

        public static implicit operator ItemSave(EquipmentModelId modelId) => new()
        {
            ModelBase = modelId.Id,
            ModelVariant = modelId.Variant,
            DyeId = modelId.Stain,
        };
    }

    internal struct WeaponSave
    {
        public Vector3 Color { get; set; }
        public Vector3 Scale { get; set; }
        public ushort ModelSet { get; set; }
        public ushort ModelBase { get; set; }
        public ushort ModelVariant { get; set; }
        public byte DyeId { get; set; }

        public static implicit operator WeaponModelId(WeaponSave save) => new()
        {
            Id = save.ModelSet,
            Variant = save.ModelVariant,
            Type = save.ModelBase,
            Stain = save.DyeId
        };

        public static implicit operator WeaponSave(WeaponModelId modelId) => new()
        {
            ModelSet = modelId.Id,
            ModelVariant = modelId.Variant,
            ModelBase = modelId.Type,
            DyeId = modelId.Stain
        };
    }
}
