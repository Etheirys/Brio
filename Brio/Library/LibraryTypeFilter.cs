using System;
using System.Collections.Generic;

namespace Brio.Library;

public class LibraryTypeFilter : LibraryFilterBase
{
    private HashSet<Type> _types = new();

    public LibraryTypeFilter(string name, params Type[] fileTypes)
        : base(name)
    {
        foreach(Type type in fileTypes)
        {
            _types.Add(type);
        }
    }

    public override bool Filter(ILibraryEntry entry)
    {
        if (entry.FileType != null)
            return _types.Contains(entry.FileType);
        
        return true;
    }
}
