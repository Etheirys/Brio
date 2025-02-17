using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Runtime.InteropServices;

namespace Brio.Game.Actor.Appearance;

[StructLayout(LayoutKind.Explicit, Size = Count)]
public unsafe struct ActorEquipment
{
    public const int Count = 0x50;

    [FieldOffset(0x00)] public fixed byte Data[Count];
    [FieldOffset(0x00)] public EquipmentModelId Head;
    [FieldOffset(0x08)] public EquipmentModelId Top;
    [FieldOffset(0x10)] public EquipmentModelId Arms;
    [FieldOffset(0x18)] public EquipmentModelId Legs;
    [FieldOffset(0x20)] public EquipmentModelId Feet;
    [FieldOffset(0x28)] public EquipmentModelId Ear;
    [FieldOffset(0x30)] public EquipmentModelId Neck;
    [FieldOffset(0x38)] public EquipmentModelId Wrist;
    [FieldOffset(0x40)] public EquipmentModelId RFinger;
    [FieldOffset(0x48)] public EquipmentModelId LFinger;


    public static ActorEquipment Smallclothes()
    {
        var equipment = new ActorEquipment();
        ApplyToAll(ref equipment, SpecialAppearances.Smallclothes);
        equipment.Ear = SpecialAppearances.None;
        equipment.Neck = SpecialAppearances.None;
        equipment.Wrist = SpecialAppearances.None;
        equipment.RFinger = SpecialAppearances.None;
        equipment.LFinger = SpecialAppearances.None;
        return equipment;
    }

    public static ActorEquipment Emperors()
    {
        var equipment = new ActorEquipment
        {
            Head = SpecialAppearances.EmperorsMainSlotsEquipment,
            Top = SpecialAppearances.EmperorsMainSlotsEquipment,
            Arms = SpecialAppearances.EmperorsMainSlotsEquipment,
            Legs = SpecialAppearances.EmperorsMainSlotsEquipment,
            Feet = SpecialAppearances.EmperorsMainSlotsEquipment,
            Ear = SpecialAppearances.EmperorsAccessorySlotsEquipment,
            Neck = SpecialAppearances.EmperorsAccessorySlotsEquipment,
            Wrist = SpecialAppearances.EmperorsAccessorySlotsEquipment,
            RFinger = SpecialAppearances.EmperorsAccessorySlotsEquipment,
            LFinger = SpecialAppearances.EmperorsAccessorySlotsEquipment
        };
        return equipment;
    }

    private static void ApplyToAll(ref ActorEquipment equip, EquipmentModelId model)
    {
        var slots = Count / sizeof(EquipmentModelId);

        fixed(byte* dataPtr = equip.Data)
        {
            for(int i = 0; i < slots; i++)
            {
                var addr = (EquipmentModelId*)(dataPtr + i * sizeof(EquipmentModelId));
                *addr = model;
            }
        }
    }
}
