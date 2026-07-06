using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Brio.Game.WorldObjects.Interop;

[StructLayout(LayoutKind.Explicit, Size = 2656)]
public unsafe struct WeaponEX
{
    [FieldOffset(0x00)] public Weapon* BaseObject;

}
