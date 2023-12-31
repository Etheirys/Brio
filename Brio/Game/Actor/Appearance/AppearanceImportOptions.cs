using System;

namespace Brio.Game.Actor.Appearance;

[Flags]
internal enum AppearanceImportOptions
{
    Customize = 1 << 0,
    Weapon = 1 << 1,
    Equipment = 1 << 2,
    ExtendedAppearance = 1 << 3,
    Shaders = 1 << 4,

    Gear = Weapon | Equipment,
    Default = Customize | Weapon | Equipment | ExtendedAppearance,
    All = Customize | Weapon | Equipment | ExtendedAppearance | Shaders,
}
