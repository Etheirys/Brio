using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ImSequencer.Memory
{
    /// <summary>
    /// Utilities for allocation, freeing, reallocating, moving, copying native memory and conversation from managed to unmanaged.
    /// </summary>
    public static unsafe partial class Utils
    {
        private static IAllocationCallbacks allocator = new AllocationCallbacks();

        public static IAllocationCallbacks Allocator { get => allocator; set => allocator = value; }

        public const int StackAllocLimit = 2048;

        /// <summary>
        /// Swaps the memory content between two pointers of the specified size.
        /// </summary>
        /// <param name="a">Pointer to the first memory location.</param>
        /// <param name="b">Pointer to the second memory location.</param>
        /// <param name="size">Size of each element in bytes.</param>
        public static unsafe void Swap(void* a, void* b, int size)
        {
            byte* byteA = (byte*)a;
            byte* byteB = (byte*)b;

            for (int i = 0; i < size; i++)
            {
                (byteB[i], byteA[i]) = (byteA[i], byteB[i]);
            }
        }

        /// <summary>
        /// Swaps the memory content between two pointers of the specified size.
        /// </summary>
        /// <param name="a">Pointer to the first memory location.</param>
        /// <param name="b">Pointer to the second memory location.</param>
        public static unsafe void Swap<T>(T* a, T* b) where T : unmanaged
        {
            (*b, *a) = (*a, *b);
        }

        /// <summary>
        /// Converts a UTF-8 encoded null-terminated byte pointer to a managed string.
        /// </summary>
        /// <param name="ptr">The pointer to the UTF-8 encoded string.</param>
        /// <returns>A managed string representing the UTF-8 data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? ToStringFromUTF8(byte* ptr)
        {
            return new string((sbyte*)ptr);
        }

        /// <summary>
        /// Counts the number of set bits (1s) in the binary representation of an unsigned integer.
        /// </summary>
        /// <param name="value">The unsigned integer to count bits in.</param>
        /// <returns>The count of set bits in the binary representation of the integer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bitcount(this uint value)
        {
            uint v = value;
            v -= v >> 1 & 0x55555555; // reuse input as temporary
            v = (v & 0x33333333) + (v >> 2 & 0x33333333); // temp
            uint c = (v + (v >> 4) & 0xF0F0F0F) * 0x1010101 >> 24; // count
            return c;
        }

        /// <summary>
        /// Swaps the values of two variables.
        /// </summary>
        /// <typeparam name="T">The type of the variables.</typeparam>
        /// <param name="a">The first variable.</param>
        /// <param name="b">The second variable.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(ref T a, ref T b)
        {
            (b, a) = (a, b);
        }

        /// <summary>
        /// Converts to utf16 pointer.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The pointer, must be freed after usage.</returns>
        public static char* ToUTF16Ptr(this string str)
        {
            char* dst = AllocT<char>(str.Length + 1);
            fixed (char* src = str)
            {
                MemcpyT(src, dst, str.Length, str.Length);
            }
            dst[str.Length] = '\0';
            return dst;
        }

        /// <summary>
        /// Converts an array to an native pointer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ToPtr<T>(this T[] values) where T : unmanaged
        {
            uint bytesToCopy = (uint)values.Length * (uint)sizeof(T);
            T* result = (T*)Alloc(bytesToCopy);
            fixed (T* src = values)
            {
                MemcpyT(src, result, bytesToCopy, bytesToCopy);
            }
            return result;
        }

        /// <summary>
        /// Converts to utf8 pointer.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The pointer, must be freed after usage.</returns>
        public static byte* ToUTF8Ptr(this string str)
        {
            var byteCount = Encoding.UTF8.GetByteCount(str);
            byte* dst = AllocT<byte>(byteCount + 1);
            fixed (char* src = str)
            {
                Encoding.UTF8.GetBytes(src, str.Length, dst, byteCount);
            }
            dst[str.Length] = 0;
            return dst;
        }

        /// <summary>
        /// Copies memory from the source to the destination with specified lengths.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="dstLength">Length of the destination memory to copy to.</param>
        /// <param name="srcLength">Length of the source memory to copy from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memcpy(void* src, void* dst, uint dstLength, uint srcLength)
        {
            Buffer.MemoryCopy(src, dst, dstLength, srcLength);
        }

        /// <summary>
        /// Copies memory from the source to the destination with specified lengths.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="dstLength">Length of the destination memory to copy to.</param>
        /// <param name="srcLength">Length of the source memory to copy from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memcpy(void* src, void* dst, int dstLength, int srcLength)
        {
            Buffer.MemoryCopy(src, dst, dstLength, srcLength);
        }

        /// <summary>
        /// Copies memory from the source to the destination with specified lengths.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="dstLength">Length of the destination memory to copy to.</param>
        /// <param name="srcLength">Length of the source memory to copy from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memcpy(void* src, void* dst, long dstLength, long srcLength)
        {
            Buffer.MemoryCopy(src, dst, dstLength, srcLength);
        }

        /// <summary>
        /// Copies memory from the source to the destination with specified lengths.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="dstLength">Length of the destination memory to copy to.</param>
        /// <param name="srcLength">Length of the source memory to copy from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memcpy(void* src, void* dst, ulong dstLength, ulong srcLength)
        {
            Buffer.MemoryCopy(src, dst, dstLength, srcLength);
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="length">Length of the source and destination memory to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memcpy(void* src, void* dst, uint length)
        {
            Buffer.MemoryCopy(src, dst, length, length);
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="length">Length of the source and destination memory to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memcpy(void* src, void* dst, int length)
        {
            Buffer.MemoryCopy(src, dst, length, length);
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="length">Length of the source and destination memory to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memcpy(void* src, void* dst, long length)
        {
            Buffer.MemoryCopy(src, dst, length, length);
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="length">Length of the source and destination memory to copy.</param>
        public static void Memcpy(void* src, void* dst, ulong length)
        {
            Buffer.MemoryCopy(src, dst, length, length);
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="dstLength">Length of the destination memory to copy to.</param>
        /// <param name="srcLength">Length of the source memory to copy from.</param>
        /// <typeparam name="T">Type of elements to copy.</typeparam>
        public static void MemcpyT<T>(T* src, T* dst, uint dstLength, uint srcLength) where T : unmanaged
        {
            Buffer.MemoryCopy(src, dst, dstLength * sizeof(T), srcLength * sizeof(T));
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="dstLength">Length of the destination memory to copy to.</param>
        /// <param name="srcLength">Length of the source memory to copy from.</param>
        /// <typeparam name="T">Type of elements to copy.</typeparam>
        public static void MemcpyT<T>(T* src, T* dst, int dstLength, int srcLength) where T : unmanaged
        {
            Buffer.MemoryCopy(src, dst, dstLength * sizeof(T), srcLength * sizeof(T));
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="dstLength">Length of the destination memory to copy to.</param>
        /// <param name="srcLength">Length of the source memory to copy from.</param>
        /// <typeparam name="T">Type of elements to copy.</typeparam>
        public static void MemcpyT<T>(T* src, T* dst, nint dstLength, nint srcLength) where T : unmanaged
        {
            Buffer.MemoryCopy(src, dst, dstLength * sizeof(T), srcLength * sizeof(T));
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="length">Length of the source and destination memory to copy.</param>
        /// <typeparam name="T">Type of elements to copy.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemcpyT<T>(T* src, T* dst, uint length) where T : unmanaged
        {
            Buffer.MemoryCopy(src, dst, length * sizeof(T), length * sizeof(T));
        }

        /// <summary>
        /// Copies memory from the source to the destination with the same length for both source and destination.
        /// </summary>
        /// <param name="src">Pointer to the source memory.</param>
        /// <param name="dst">Pointer to the destination memory.</param>
        /// <param name="length">Length of the source and destination memory to copy.</param>
        /// <typeparam name="T">Type of elements to copy.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemcpyT<T>(T* src, T* dst, int length) where T : unmanaged
        {
            Buffer.MemoryCopy(src, dst, length * sizeof(T), length * sizeof(T));
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a span of type T.
        /// </summary>
        /// <typeparam name="T">The type of elements in the span.</typeparam>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemoryT<T>(T* pointer) where T : unmanaged
        {
            Unsafe.InitBlockUnaligned(pointer, 0, (uint)sizeof(T));
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a span of type T.
        /// </summary>
        /// <typeparam name="T">The type of elements in the span.</typeparam>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="length">Number of elements of type T to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemoryT<T>(T* pointer, int length) where T : unmanaged
        {
            ZeroMemory(pointer, sizeof(T) * length);
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a span of type T.
        /// </summary>
        /// <typeparam name="T">The type of elements in the span.</typeparam>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="length">Number of elements of type T to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemoryT<T>(T* pointer, uint length) where T : unmanaged
        {
            ZeroMemory(pointer, sizeof(T) * (int)length);
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a span of type T.
        /// </summary>
        /// <typeparam name="T">The type of elements in the span.</typeparam>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="length">Number of elements of type T to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemoryT<T>(T* pointer, nint length) where T : unmanaged
        {
            ZeroMemory(pointer, sizeof(T) * length);
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a specified number of bytes.
        /// </summary>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="size">Number of bytes to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemory(void* pointer, uint size)
        {
            Unsafe.InitBlockUnaligned(pointer, 0, size);
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a specified number of bytes.
        /// </summary>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="size">Number of bytes to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemory(void* pointer, int size)
        {
            Unsafe.InitBlockUnaligned(pointer, 0, (uint)size);
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a specified number of bytes.
        /// </summary>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="size">Number of bytes to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemory(void* pointer, ulong size)
        {
            while (size > uint.MaxValue)
            {
                Unsafe.InitBlockUnaligned(pointer, 0, uint.MaxValue);
                size -= uint.MaxValue;
            }

            if (size > 0)
            {
                Unsafe.InitBlockUnaligned(pointer, 0, (uint)size);
            }
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a specified number of bytes.
        /// </summary>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="size">Number of bytes to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemory(void* pointer, long size)
        {
            while (size > uint.MaxValue)
            {
                Unsafe.InitBlockUnaligned(pointer, 0, uint.MaxValue);
                size -= uint.MaxValue;
            }

            if (size > 0)
            {
                Unsafe.InitBlockUnaligned(pointer, 0, (uint)size);
            }
        }

        /// <summary>
        /// Sets all bytes in memory to zero for a specified number of bytes.
        /// </summary>
        /// <param name="pointer">Pointer to the memory to clear.</param>
        /// <param name="size">Number of bytes to clear.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMemory(void* pointer, nint size)
        {
            if (sizeof(nint) == 8)
            {
                ZeroMemory(pointer, (long)size);
            }
            else
            {
                ZeroMemory(pointer, (int)size);
            }
        }

        /// <summary>
        /// Allocates and returns a pointer to memory for a single element of type T, initializing it with the provided data.
        /// </summary>
        /// <typeparam name="T">The type of the element to allocate.</typeparam>
        /// <param name="data">The data to initialize the allocated memory with.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocT<T>(T data
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.Alloc(sizeof(T)
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            *result = data;
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to memory for a single element of type T, initializing it with the default value.
        /// </summary>
        /// <typeparam name="T">The type of the element to allocate.</typeparam>
        /// <returns>A pointer to the allocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocT<T>(
#if TRACELEAK
             [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.Alloc(sizeof(T)
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            *result = new T();
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to memory for an array of elements of type T.
        /// </summary>
        /// <typeparam name="T">The type of the elements to allocate.</typeparam>
        /// <param name="count">The number of elements to allocate.</param>
        /// <returns>A pointer to the allocated memory for the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocT<T>(int count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.Alloc(sizeof(T) * count
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to memory for an array of elements of type T.
        /// </summary>
        /// <typeparam name="T">The type of the elements to allocate.</typeparam>
        /// <param name="count">The number of elements to allocate.</param>
        /// <returns>A pointer to the allocated memory for the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocT<T>(uint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.Alloc((nint)(sizeof(T) * count)
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to memory for an array of elements of type T.
        /// </summary>
        /// <typeparam name="T">The type of the elements to allocate.</typeparam>
        /// <param name="count">The number of elements to allocate.</param>
        /// <returns>A pointer to the allocated memory for the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocT<T>(nint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.Alloc(sizeof(T) * count
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to memory for an array of elements of type T.
        /// </summary>
        /// <typeparam name="T">The type of the elements to allocate.</typeparam>
        /// <param name="count">The number of elements to allocate.</param>
        /// <returns>A pointer to the allocated memory for the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocT<T>(nuint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.Alloc((nuint)sizeof(T) * count
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to unmanaged memory.
        /// </summary>
        /// <param name="count">The number of bytes to allocate.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Alloc(nint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = allocator.Alloc(count
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to unmanaged memory.
        /// </summary>
        /// <param name="count">The number of bytes to allocate.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Alloc(nuint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = allocator.Alloc((nint)count
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to unmanaged memory.
        /// </summary>
        /// <param name="size">The size, in bytes, to allocate.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Alloc(int size
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = allocator.Alloc(size
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates and returns a pointer to unmanaged memory.
        /// </summary>
        /// <param name="size">The size, in bytes, to allocate.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Alloc(uint size
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = allocator.Alloc((nint)size
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Reallocates and returns a pointer to unmanaged memory for an array of elements of type T, preserving the existing data.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="pointer">A pointer to the existing memory.</param>
        /// <param name="count">The new number of elements to allocate.</param>
        /// <returns>A pointer to the reallocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ReAllocT<T>(T* pointer, int count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.ReAlloc(pointer, count * sizeof(T)
#if TRACELEAK
                   , $"File: {file}, Line: {line}"
#endif
                   );
            return result;
        }

        /// <summary>
        /// Reallocates and returns a pointer to unmanaged memory for an array of elements of type T, preserving the existing data.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="pointer">A pointer to the existing memory.</param>
        /// <param name="count">The new number of elements to allocate.</param>
        /// <returns>A pointer to the reallocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ReAllocT<T>(T* pointer, uint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.ReAlloc(pointer, (nint)(count * sizeof(T))
#if TRACELEAK
                   , $"File: {file}, Line: {line}"
#endif
                   );
            return result;
        }

        /// <summary>
        /// Reallocates and returns a pointer to unmanaged memory for an array of elements of type T, preserving the existing data.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="pointer">A pointer to the existing memory.</param>
        /// <param name="count">The new number of elements to allocate.</param>
        /// <returns>A pointer to the reallocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ReAllocT<T>(T* pointer, nint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.ReAlloc(pointer, count * sizeof(T)
#if TRACELEAK
                   , $"File: {file}, Line: {line}"
#endif
                   );
            return result;
        }

        /// <summary>
        /// Reallocates and returns a pointer to unmanaged memory for an array of elements of type T, preserving the existing data.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="pointer">A pointer to the existing memory.</param>
        /// <param name="count">The new number of elements to allocate.</param>
        /// <returns>A pointer to the reallocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ReAllocT<T>(T* pointer, nuint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = (T*)allocator.ReAlloc(pointer, count * (nuint)sizeof(T)
#if TRACELEAK
                   , $"File: {file}, Line: {line}"
#endif
                   );
            return result;
        }

        /// <summary>
        /// Reallocates and returns a pointer to unmanaged memory, preserving the existing data.
        /// </summary>
        /// <param name="pointer">A pointer to the existing memory.</param>
        /// <param name="count">The new number of bytes to allocate.</param>
        /// <returns>A pointer to the reallocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ReAlloc(void* pointer, int count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = allocator.ReAlloc(pointer, count
#if TRACELEAK
                   , $"File: {file}, Line: {line}"
#endif
                   );
            return result;
        }

        /// <summary>
        /// Reallocates and returns a pointer to unmanaged memory, preserving the existing data.
        /// </summary>
        /// <param name="pointer">A pointer to the existing memory.</param>
        /// <param name="count">The new number of bytes to allocate.</param>
        /// <returns>A pointer to the reallocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ReAlloc(void* pointer, uint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = allocator.ReAlloc(pointer, (nint)count
#if TRACELEAK
                   , $"File: {file}, Line: {line}"
#endif
                   );
            return result;
        }

        /// <summary>
        /// Reallocates and returns a pointer to unmanaged memory, preserving the existing data.
        /// </summary>
        /// <param name="pointer">A pointer to the existing memory.</param>
        /// <param name="count">The new number of bytes to allocate.</param>
        /// <returns>A pointer to the reallocated memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ReAlloc(void* pointer, nint count
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = allocator.ReAlloc(pointer, count
#if TRACELEAK
                   , $"File: {file}, Line: {line}"
#endif
                   );
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of elements of type T, copies data from the source pointer to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="pointer">A pointer to the source data to be copied.</param>
        /// <param name="length">The number of elements in the array.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocCopyT<T>(T* pointer, int length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = AllocT<T>(length
#if TRACELEAK
                , file, line
#endif
                );
            MemcpyT(pointer, result, length, length);
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of elements of type T, copies data from the source pointer to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="pointer">A pointer to the source data to be copied.</param>
        /// <param name="length">The number of elements in the array.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocCopyT<T>(T* pointer, uint length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T* result = AllocT<T>(length
#if TRACELEAK
                , file, line
#endif
                );
            MemcpyT(pointer, result, length, length);
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of elements of type T, copies data from the source span to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="source">The source span to copy from.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocCopyT<T>(T[] source
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            int length = source.Length;
            T* result = AllocT<T>(length
#if TRACELEAK
                , file, line
#endif
                );
            fixed (T* pointer = source)
            {
                MemcpyT(pointer, result, length, length);
            }

            return result;
        }

        /// <summary>
        /// Allocates memory for an array of elements of type T, copies data from the source span to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="source">The source span to copy from.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocCopyT<T>(Span<T> source
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            int length = source.Length;
            T* result = AllocT<T>(length
#if TRACELEAK
                , file, line
#endif
                );
            for (int i = 0; i < length; i++)
            {
                result[i] = source[i];
            }

            return result;
        }

        /// <summary>
        /// Allocates memory for an array of elements of type T, copies data from the source span to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the array.</typeparam>
        /// <param name="source">The source span to copy from.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocCopyT<T>(ReadOnlySpan<T> source
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            int length = source.Length;
            T* result = AllocT<T>(length
#if TRACELEAK
                , file, line
#endif
                );
            for (int i = 0; i < length; i++)
            {
                result[i] = source[i];
            }

            return result;
        }

        /// <summary>
        /// Allocates memory for an array of bytes, copies data from the source pointer to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <param name="pointer">A pointer to the source data to be copied.</param>
        /// <param name="length">The number of bytes to allocate and copy.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocCopy(void* pointer, int length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = Alloc(length
#if TRACELEAK
                , file, line
#endif
                );
            Memcpy(pointer, result, length, length);
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of bytes, copies data from the source pointer to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <param name="pointer">A pointer to the source data to be copied.</param>
        /// <param name="length">The number of bytes to allocate and copy.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocCopy(void* pointer, uint length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = Alloc(length);
            Memcpy(pointer, result, length, length);
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of bytes, copies data from the source pointer to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <param name="pointer">A pointer to the source data to be copied.</param>
        /// <param name="length">The number of bytes to allocate and copy.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocCopy(void* pointer, nint length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = Alloc(length
#if TRACELEAK
                , file, line
#endif
                );
            Memcpy(pointer, result, length, length);
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of bytes, copies data from the source pointer to the new memory, and returns a pointer to the new memory.
        /// </summary>
        /// <param name="pointer">A pointer to the source data to be copied.</param>
        /// <param name="length">The number of bytes to allocate and copy.</param>
        /// <returns>A pointer to the newly allocated memory with the copied data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocCopy(void* pointer, nuint length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void* result = Alloc(length
#if TRACELEAK
                , file, line
#endif
                );
            Memcpy(pointer, result, length, length);
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of pointers and returns a pointer to the newly allocated memory.
        /// </summary>
        /// <param name="length">The number of pointers to allocate.</param>
        /// <returns>A pointer to the newly allocated memory for an array of pointers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void** AllocArray(uint length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            void** result = (void**)allocator.Alloc((nint)(length * sizeof(void*))
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of pointers and returns a pointer to the newly allocated memory.
        /// </summary>
        /// <param name="length">The number of pointers to allocate.</param>
        /// <returns>A pointer to the newly allocated memory for an array of pointers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T** AllocArrayT<T>(uint length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T** result = (T**)allocator.Alloc((nint)(length * sizeof(void*))
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of pointers and returns a pointer to the newly allocated memory.
        /// </summary>
        /// <param name="length">The number of pointers to allocate.</param>
        /// <returns>A pointer to the newly allocated memory for an array of pointers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T** AllocArrayT<T>(int length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            ) where T : unmanaged
        {
            T** result = (T**)allocator.Alloc((nint)(length * sizeof(void*))
#if TRACELEAK
                , $"File: {file}, Line: {line}"
#endif
                );
            return result;
        }

        /// <summary>
        /// Allocates memory for an array of pointers, sets the memory to zero, and returns a pointer to the newly allocated memory.
        /// </summary>
        /// <param name="length">The number of pointers to allocate and set to zero.</param>
        /// <returns>A pointer to the newly allocated memory for an array of pointers with zeroed memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void** AllocArrayAndZero(uint length
#if TRACELEAK
            , [CallerFilePath] string file = "", [CallerLineNumber] int line = 0
#endif
            )
        {
            var result = AllocArray(length
#if TRACELEAK
                , file, line
#endif
                );
            ZeroMemory(result, (int)(sizeof(nint) * length));
            return result;
        }

        /// <summary>
        /// Frees memory allocated for an unmanaged resource associated with a pointer to a type that implements <see cref="IFreeable"/>.
        /// </summary>
        /// <typeparam name="T">The unmanaged type that implements <see cref="IFreeable"/>.</typeparam>
        /// <param name="pointer">A pointer to the unmanaged resource to be released.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free<T>(T* pointer) where T : unmanaged, IFreeable
        {
            pointer->Release();
            allocator.Free(pointer);
        }

        /// <summary>
        /// Frees memory allocated for an unmanaged resource associated with a pointer.
        /// </summary>
        /// <param name="pointer">A pointer to the unmanaged resource to be released.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* pointer)
        {
            allocator.Free(pointer);
        }

        /// <summary>
        /// Frees memory allocated for an array of pointers.
        /// </summary>
        /// <param name="pointer">A pointer to the array of pointers to be released.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void** pointer)
        {
            allocator.Free(pointer);
        }

        /// <summary>
        /// Copies unmanaged memory to a new managed array.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of elements.</typeparam>
        /// <param name="src">A pointer to the source unmanaged memory.</param>
        /// <param name="length">The number of elements to copy.</param>
        /// <returns>A new managed array containing the copied elements or null if <paramref name="src"/> is null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[]? ToManaged<T>(T* src, int length) where T : unmanaged
        {
            if (src == null)
            {
                return null;
            }

            T[] values = new T[length];
            fixed (T* dst = values)
            {
                MemcpyT(src, dst, length, length);
            }
            return values;
        }

        /// <summary>
        /// Copies unmanaged memory to a new managed array.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of elements.</typeparam>
        /// <param name="src">A pointer to the source unmanaged memory.</param>
        /// <param name="length">The number of elements to copy.</param>
        /// <returns>A new managed array containing the copied elements or null if <paramref name="src"/> is null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[]? ToManaged<T>(T* src, uint length) where T : unmanaged
        {
            if (src == null)
            {
                return null;
            }

            T[] values = new T[length];
            fixed (T* dst = values)
            {
                MemcpyT(src, dst, length, length);
            }
            return values;
        }

        /// <summary>
        /// Sets the memory at the specified pointer to the specified value for a given number of elements.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of elements.</typeparam>
        /// <param name="ptr">A pointer to the memory to set.</param>
        /// <param name="value">The byte value to set the memory to.</param>
        /// <param name="count">The number of elements to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemsetT<T>(T* ptr, T value, int count) where T : unmanaged
        {
            for (int i = 0; i < count; i++)
            {
                ptr[i] = value;
            }
        }

        /// <summary>
        /// Sets the memory at the specified pointer to the specified value for a given number of elements.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of elements.</typeparam>
        /// <param name="ptr">A pointer to the memory to set.</param>
        /// <param name="value">The byte value to set the memory to.</param>
        /// <param name="count">The number of elements to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemsetT<T>(T* ptr, byte value, int count) where T : unmanaged
        {
            new Span<byte>(ptr, count * sizeof(T)).Fill(value);
        }

        /// <summary>
        /// Sets the memory at the specified pointer to the specified value for a given number of elements.
        /// </summary>
        /// <typeparam name="T">The unmanaged type of elements.</typeparam>
        /// <param name="ptr">A pointer to the memory to set.</param>
        /// <param name="value">The byte value to set the memory to.</param>
        /// <param name="count">The number of elements to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemsetT<T>(T* ptr, byte value, uint count) where T : unmanaged
        {
            new Span<byte>(ptr, (int)(count * sizeof(T))).Fill(value);
        }

        /// <summary>
        /// Sets the memory at the specified pointer to the specified value for a given number of bytes.
        /// </summary>
        /// <param name="ptr">A pointer to the memory to set.</param>
        /// <param name="value">The byte value to set the memory to.</param>
        /// <param name="length">The number of bytes to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memset(void* ptr, byte value, int length)
        {
            Unsafe.InitBlockUnaligned(ptr, value, (uint)length);
        }

        /// <summary>
        /// Sets the memory at the specified pointer to the specified value for a given number of bytes.
        /// </summary>
        /// <param name="ptr">A pointer to the memory to set.</param>
        /// <param name="value">The byte value to set the memory to.</param>
        /// <param name="length">The number of bytes to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Memset(void* ptr, byte value, uint length)
        {
            Unsafe.InitBlockUnaligned(ptr, value, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(T* data, int size, T* str, int length) where T : unmanaged, IEquatable<T>
        {
            int cmp = 0;
            for (int i = 0; i < size; i++)
            {
                if (data[i].Equals(str[cmp]))
                {
                    cmp++;
                    if (cmp == length)
                    {
                        return true;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }

            return cmp == length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TSrc, TDst>(TSrc* data, int size, TDst* str, int length, Func<TDst, TSrc> convert) where TSrc : unmanaged, IEquatable<TSrc> where TDst : unmanaged
        {
            if (length > size)
            {
                return false;
            }

            int cmp = 0;
            for (int i = 0; i < size; i++)
            {
                if (data[i].Equals(convert(str[cmp])))
                {
                    cmp++;
                    if (cmp == length)
                    {
                        return true;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }

            return cmp == length;
        }

        public static int Find<T>(T* data, int size, T* str, int length, int pos) where T : unmanaged, IEquatable<T>
        {
            if (length > size - pos)
            {
                return -1;
            }

            int cmp = 0;
            for (int i = pos; i < size; i++)
            {
                if (data[i].Equals(str[cmp]))
                {
                    cmp++;
                    if (cmp == length)
                    {
                        return i - cmp + 1;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }

            return -1;
        }

        public static int Find<T>(T* data, int size, T* str, int length, int pos, IEqualityComparer<T> comparer) where T : unmanaged, IEquatable<T>
        {
            if (length > size - pos)
            {
                return -1;
            }

            int cmp = 0;
            for (int i = pos; i < size; i++)
            {
                if (comparer.Equals(data[i], str[cmp]))
                {
                    cmp++;
                    if (cmp == length)
                    {
                        return i - cmp + 1;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }

            return -1;
        }

        public static int Find<TSrc, TDst>(TSrc* data, int size, TDst* str, int length, int pos, IConverter<TDst, TSrc> convert) where TSrc : unmanaged, IEquatable<TSrc> where TDst : unmanaged
        {
            if (length > size - pos)
            {
                return -1;
            }

            int cmp = 0;
            for (int i = pos; i < size; i++)
            {
                if (data[i].Equals(convert.Convert(str[cmp])))
                {
                    cmp++;
                    if (cmp == length)
                    {
                        return i - cmp + 1;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }

            return -1;
        }

        public static int Find<TSrc, TDst>(TSrc* data, int size, TDst* str, int length, int pos, IConverter<TDst, TSrc> convert, IEqualityComparer<TSrc> comparer) where TSrc : unmanaged, IEquatable<TSrc> where TDst : unmanaged
        {
            if (length > size - pos)
            {
                return -1;
            }

            int cmp = 0;
            for (int i = pos; i < size; i++)
            {
                if (comparer.Equals(data[i], convert.Convert(str[cmp])))
                {
                    cmp++;
                    if (cmp == length)
                    {
                        return i - cmp + 1;
                    }
                }
                else
                {
                    cmp = 0;
                }
            }

            return -1;
        }
    }
}
