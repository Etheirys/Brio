namespace ImSequencer.Memory
{
    public interface IAllocationCallbacks
    {
        unsafe void* Alloc(nuint size
#if TRACELEAK
            , string name
#endif
    );

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        unsafe void* Alloc(nint size
#if TRACELEAK
         , string name
#endif
         )
        {
            return Alloc((nuint)size
#if TRACELEAK
                , name
#endif
                );
        }

        unsafe void* Alloc(int size
#if TRACELEAK
         , string name
#endif
         )
        {
            return Alloc((nint)size
#if TRACELEAK
                , name
#endif
                );
        }
#else

        unsafe void* Alloc(nint size
#if TRACELEAK
         , string name
#endif
 );

        unsafe void* Alloc(int size
#if TRACELEAK
         , string name
#endif
         );

#endif

        unsafe void* ReAlloc(void* ptr, nuint size
#if TRACELEAK
    , string name
#endif
    );

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        unsafe void* ReAlloc(void* ptr, nint size
#if TRACELEAK
         , string name
#endif
 )
        {
            return ReAlloc(ptr, (nuint)size
#if TRACELEAK
                , name
#endif
                );
        }

        unsafe void* ReAlloc(void* ptr, int size
#if TRACELEAK
         , string name
#endif
         )
        {
            return ReAlloc(ptr, (nuint)size
#if TRACELEAK
                , name
#endif
                );
        }
#else

        unsafe void* ReAlloc(void* ptr, nint size
#if TRACELEAK
         , string name
#endif
);

        unsafe void* ReAlloc(void* ptr, int size
#if TRACELEAK
         , string name
#endif
         );

#endif

        unsafe void Free(void* ptr);
    }
}