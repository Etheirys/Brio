namespace ImSequencer.Memory
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Utilities for managed and unmanaged arrays.
    /// </summary>
    public static unsafe class ArrayUtils
    {
        /// <summary>
        /// Adds a value to an array and resizes it to accommodate the new value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Reference to the array to which the value will be added.</param>
        /// <param name="value">The value to add to the array.</param>
        public static void Add<T>(ref T[] array, T value)
        {
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = value;
        }

        /// <summary>
        /// Removes the first occurrence of a value from an array and resizes it accordingly.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">Reference to the array from which the value will be removed.</param>
        /// <param name="value">The value to remove from the array.</param>
        public static void Remove<T>(ref T[] array, T value)
        {
            int index = Array.IndexOf(array, value);
            var count = array.Length - index;
            Buffer.BlockCopy(array, index + 1, array, index, count);
            Array.Resize(ref array, array.Length - 1);
        }

        /// <summary>
        /// Adds a value to a list if it doesn't already exist and returns a boolean indicating success.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to which the value will be added.</param>
        /// <param name="t">The value to add to the list.</param>
        /// <returns>Returns true if the value was added (not already in the list), otherwise false.</returns>
        public static bool AddUnique<T>(this IList<T> list, T t)
        {
            if (list.Contains(t))
            {
                return false;
            }

            list.Add(t);

            return true;
        }

        /// <summary>
        /// Finds the index of a specified item within an unmanaged array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="ptr">Pointer to the beginning of the array.</param>
        /// <param name="item">Pointer to the item to search for.</param>
        /// <param name="count">The number of items in the array.</param>
        /// <returns>The index of the item if found; otherwise, -1.</returns>
        public static int IndexOf<T>(T* ptr, T* item, int count) where T : unmanaged
        {
            for (int i = 0; i < count; i++)
            {
                if (&ptr[i] == item)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
