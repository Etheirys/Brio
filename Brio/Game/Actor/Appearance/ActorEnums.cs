using System;

namespace Brio.Game.Actor.Appearance;

public enum Genders : byte
{
    Masculine = 0,
    Feminine = 1,
}

public enum Races : byte
{
    Hyur = 1,
    Elezen = 2,
    Lalafel = 3,
    Miqote = 4,
    Roegadyn = 5,
    AuRa = 6,
    Hrothgar = 7,
    Viera = 8,
}

public enum Tribes : byte
{
    Midlander = 1,
    Highlander = 2,
    Wildwood = 3,
    Duskwight = 4,
    Plainsfolk = 5,
    Dunesfolk = 6,
    SeekerOfTheSun = 7,
    KeeperOfTheMoon = 8,
    SeaWolf = 9,
    Hellsguard = 10,
    Raen = 11,
    Xaela = 12,
    Helions = 13,
    TheLost = 14,
    Rava = 15,
    Veena = 16,
}

public enum BodyTypes : byte
{
    Normal = 1,
    Old = 3,
    Young = 4,
}

[Flags]
public enum FacialFeature : byte
{
    None = 0,
    First = 1,
    Second = 2,
    Third = 4,
    Fourth = 8,
    Fifth = 16,
    Sixth = 32,
    Seventh = 64,
    LegacyTattoo = 128,
}
