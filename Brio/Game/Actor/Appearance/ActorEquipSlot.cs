using System;

namespace Brio.Game.Actor.Appearance;

[Flags]
internal enum ActorEquipSlot
{
    None = 0,
    MainHand = 1 << 0,
    OffHand = 1 << 1,
    Prop = 1 << 2,
    Head = 1 << 3,
    Body = 1 << 4,
    Hands = 1 << 5,
    Legs = 1 << 6,
    Feet = 1 << 7,
    Ears = 1 << 8,
    Neck = 1 << 9,
    Wrists = 1 << 10,
    RightRing = 1 << 11,
    LeftRing = 1 << 12,

    Weapons = MainHand | OffHand | Prop,
    Armor = Head | Body | Hands | Legs | Feet,
    Accessories = Ears | Neck | Wrists | RightRing | LeftRing,
    AllButWeapons = Armor | Accessories,
    All = MainHand | OffHand | Prop | Head | Body | Hands | Legs | Feet | Ears | Neck | Wrists | RightRing | LeftRing
}
