using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Runtime.InteropServices;

namespace Brio.Game.Actor.Interop;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct BrioDrawData
{
    [FieldOffset(0x0)]
    public DrawDataContainer DawData;

    [FieldOffset(0x1D0)]
    public ushort Facewear;
}
