using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Runtime.InteropServices;

namespace Brio.Game.WorldObjects.Interop;

[StructLayout(LayoutKind.Explicit, Size = 2656)]
public unsafe struct WeaponEX
{
    [FieldOffset(0x00)] public Weapon* BaseObject;

}
