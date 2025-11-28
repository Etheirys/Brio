using System.Runtime.InteropServices;

namespace ImSequencer.Memory
{
    public unsafe class AllocationCallbacks : IAllocationCallbacks
    {
#if TRACELEAK
        private readonly List<Allocation> allocations = [];

        public IReadOnlyList<Allocation> Allocations => allocations;
#endif
#if TRACELEAK

        public struct Allocation
        {
            public void* Ptr;
            public nint Size;
            public string Caller;

            public Allocation(void* ptr, nint size, string caller)
            {
                Ptr = ptr;
                Size = size;
                Caller = caller;
            }
        }

#endif

#if TRACELEAK

        private void Remove(void* ptr)
        {
            if (ptr == null)
            {
                return;
            }

            lock (allocations)
            {
                for (int i = 0; i < allocations.Count; i++)
                {
                    Allocation allocation = allocations[i];
                    if (allocation.Ptr == ptr)
                    {
                        allocations.RemoveAt(i);
                        return;
                    }
                }
            }
        }

#endif

        public void ReportInstances()
        {
#if TRACELEAK
            lock (allocations)
            {
                foreach (Allocation allocation in allocations)
                {
                    Logger.Warn($"*** Live allocation ({allocation.Caller}):\n\t{(nint)allocation.Ptr:X}\n\tSize: {allocation.Size}\n");
                }
            }
#endif
        }

        public void ReportDuplicateInstances()
        {
#if TRACELEAK
            lock (allocations)
            {
                foreach (Allocation allocation in allocations)
                {
                    foreach (Allocation allocationCmp in allocations)
                    {
                        if (allocation.Caller == allocationCmp.Caller && allocation.Size == allocationCmp.Size)
                        {
                            Logger.Warn($"*** Possible duplicate instance ({allocation.Caller}):\n\t{(nint)allocation.Ptr:X}\n\tSize: {allocation.Size}\n");
                        }
                    }
                }
            }
#endif
        }

        public void* Alloc(nuint size
#if TRACELEAK
            , string name
#endif
            )
        {
#if NET5_0_OR_GREATER
            void* ptr = NativeMemory.Alloc(size);
#else
            void* ptr = (void*)Marshal.AllocHGlobal((nint)size);
#endif

#if TRACELEAK
            Allocation allocation = new(ptr, size, name);
            lock (allocations)
            {
                allocations.Add(allocation);
            }
#endif
            return ptr;
        }

        public void* ReAlloc(void* ptr, nuint size
#if TRACELEAK
            , string name
#endif
            )
        {
#if TRACELEAK
            Remove(ptr);
#endif
#if NET5_0_OR_GREATER
            ptr = NativeMemory.Realloc(ptr, size);
#else
            ptr = (void*)Marshal.ReAllocHGlobal((nint)ptr, (nint)size);
#endif
#if TRACELEAK
            Allocation allocation = new(ptr, size, name);
            lock (allocations)
            {
                allocations.Add(allocation);
            }
#endif
            return ptr;
        }

        public void Free(void* ptr)
        {
#if TRACELEAK
            Remove(ptr);
#endif
#if NET5_0_OR_GREATER
            NativeMemory.Free(ptr);
#else
            Marshal.FreeHGlobal((nint)ptr);
#endif
        }

#if !NETSTANDARD2_1_OR_GREATER && !NET5_0_OR_GREATER

        public unsafe void* Alloc(nint size)
        {
            return Alloc((nuint)size);
        }

        public unsafe void* Alloc(int size)
        {
            return Alloc((nint)size);
        }

        public unsafe void* ReAlloc(void* ptr, nint size)
        {
            return ReAlloc(ptr, (nuint)size);
        }

        public unsafe void* ReAlloc(void* ptr, int size)
        {
            return ReAlloc(ptr, (nuint)size);
        }
#endif
    }
}