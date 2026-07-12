using System;

namespace Brio.Core;

// From https://github.com/Ottermandias/Penumbra.String/blob/e7f27e1d850f9afe17006caeef4a7b887378d55a/Functions/MemoryUtility.cs
public static partial class MemoryUtility
{
    /// <summary> Copies <paramref name="count"/> bytes from <paramref name="src"/> to <paramref name="dest"/>. </summary>
    public static unsafe void MemCpyUnchecked(void* dest, void* src, int count)
    {
        var span = new Span<byte>(src, count);
        var target = new Span<byte>(dest, count);
        span.CopyTo(target);
    }

    /// <summary> Compares <paramref name="count"/> bytes from <paramref name="ptr1"/> with <paramref name="ptr2"/> lexicographically. </summary>
    public static unsafe int MemCmpUnchecked(void* ptr1, void* ptr2, int count)
    {
        var lhs = new Span<byte>(ptr1, count);
        var rhs = new Span<byte>(ptr2, count);
        return lhs.SequenceCompareTo(rhs);
    }
}
