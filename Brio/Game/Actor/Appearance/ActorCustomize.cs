using System.Runtime.InteropServices;

namespace Brio.Game.Actor.Appearance;

[StructLayout(LayoutKind.Explicit, Size = Count)]
public struct ActorCustomize
{
    public const int Count = 0x1A;

    [FieldOffset(0x00)] public unsafe fixed byte Data[Count];
    [FieldOffset(0x00)] public Races Race;
    [FieldOffset(0x01)] public Genders Gender;
    [FieldOffset(0x02)] public BodyTypes BodyType;
    [FieldOffset(0x03)] public byte Height;
    [FieldOffset(0x04)] public Tribes Tribe;
    [FieldOffset(0x05)] public byte FaceType;
    [FieldOffset(0x06)] public byte HairStyle;
    [FieldOffset(0x07)] public byte HasHighlights;
    [FieldOffset(0x08)] public byte SkinTone;
    [FieldOffset(0x09)] public byte REyeColor;
    [FieldOffset(0x0A)] public byte HairColor;
    [FieldOffset(0x0B)] public byte HairHighlightColor;
    [FieldOffset(0x0C)] public FacialFeature FaceFeatures;
    [FieldOffset(0x0D)] public byte FaceFeaturesColor;
    [FieldOffset(0x0E)] public byte Eyebrows;
    [FieldOffset(0x0F)] public byte LEyeColor;
    [FieldOffset(0x10)] public byte EyeShape;
    [FieldOffset(0x11)] public byte NoseShape;
    [FieldOffset(0x12)] public byte JawShape;
    [FieldOffset(0x13)] public byte LipStyle;
    [FieldOffset(0x14)] public byte LipColor;
    [FieldOffset(0x15)] public byte RaceFeatureSize;
    [FieldOffset(0x16)] public byte RaceFeatureType;
    [FieldOffset(0x17)] public byte BustSize;
    [FieldOffset(0x18)] public byte Facepaint;
    [FieldOffset(0x19)] public byte FacePaintColor;


    private const byte _toggleMask = 128;

    public bool HighlightsEnabled
    {
        readonly get => (HasHighlights & _toggleMask) != 0;
        set => HasHighlights = (byte)(value ? _toggleMask : 0);
    }

    public byte RealFacepaint
    {
        readonly get => (byte)(Facepaint >= _toggleMask ? Facepaint ^ _toggleMask : Facepaint);
        set => Facepaint = (byte)(FacepaintFlipped ? value | _toggleMask : value);
    }

    public bool FacepaintFlipped
    {
        readonly get => ((Facepaint & _toggleMask) != 0);
        set => Facepaint = (byte)(value ? Facepaint | _toggleMask : Facepaint ^ _toggleMask);
    }

    public byte RealEyeShape
    {
        readonly get => (byte)(EyeShape >= _toggleMask ? EyeShape ^ _toggleMask : EyeShape);
        set => EyeShape = (byte)(HasSmallIris ? value | _toggleMask : value);
    }

    public bool HasSmallIris
    {
        readonly get => ((EyeShape & _toggleMask) != 0);
        set => EyeShape = (byte)(value ? EyeShape | _toggleMask : EyeShape ^ _toggleMask);
    }

    public bool LipColorEnabled
    {
        readonly get => (LipStyle & _toggleMask) != 0;
        set => LipStyle = (byte)(value ? _toggleMask : 0);
    }

    public byte RealLipStyle
    {
        readonly get => (byte)(LipStyle >= _toggleMask ? LipStyle ^ _toggleMask : LipStyle);
        set => LipStyle = (byte)(LipColorEnabled ? value | _toggleMask : value);
    }

}
