
using System.Runtime.InteropServices;

namespace Brio.Core;

public static class NativeHelpers
{
    public static (nint Aligned, nint Unaligned) AllocateAlignedMemory(int sizeInBytes, int alignment)
    {
        int alignedSize = sizeInBytes + alignment - 1;
        nint unalignedMemory = Marshal.AllocHGlobal(alignedSize);
        int alignmentOffset = (int)(alignment - (unalignedMemory % alignment));
        nint alignedMemory = unalignedMemory + alignmentOffset;

        return (alignedMemory, unalignedMemory);
    }

    public static void FreeAlignedMemory((nint Aligned, nint Unaligned) addrs)
    {
        Marshal.FreeHGlobal(addrs.Unaligned);
    }

    public static void FreeMemory(nint addr)
    {
        Marshal.FreeHGlobal(addr);
    }
}
