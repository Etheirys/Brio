using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static FFXIVClientStructs.FFXIV.Client.Graphics.Scene.BgObject;

namespace Brio.Game.WorldObjects.Interop;

// Look how clean this is, Thanks to abyeon & UniversalConquistador, for the big help making this

[StructLayout(LayoutKind.Explicit, Size = 0xE0)]
public unsafe struct BgObjectEx
{
    [FieldOffset(0x00)] public BgObject BaseObject;
    [FieldOffset(0x00)] public BgObjectVirtualTable* VirtualTable;

    [FieldOffset(0x38)] public ulong Flags;
    [FieldOffset(0x89)] public byte HighlightFlags;

    [FieldOffset(0xCA)] public byte Transparency; // 0-255 with 0 being fully visible

    [FieldOffset(144)]
    public ModelResourceHandle* ModelResourceHandle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateCulling()
    {
        fixed(BgObject* bg = &BaseObject)
            if(ModelResourceHandle->LoadState == 7)
            {
                VirtualTable->UpdateCulling(bg);
            }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateTransforms()
    {
        fixed(BgObject* bg = &BaseObject)
            if(ModelResourceHandle->LoadState == 7)
            {
                VirtualTable->UpdateTransforms(bg, false);
            }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BgObjectEx* Create(string path)
    {
        // args2 is not used, otherwise known as, some debugging thing, we should leave it empty
        return (BgObjectEx*)BgObject.Create(path, string.Empty);
    }
}
