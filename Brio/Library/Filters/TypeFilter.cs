using System;
using System.Collections.Generic;

namespace Brio.Library.Filters;

public class TypeFilter : FilterBase
{
    private HashSet<Type> _types = new();

    public TypeFilter(string name, params Type[] fileTypes)
        : base(name)
    {
        foreach(Type type in fileTypes)
        {
            _types.Add(type);
        }
    }

    public override void Clear()
    {
        _types.Clear();
    }

    public override bool Filter(ILibraryEntry entry)
    {
        if(entry.FileType != null)
            return _types.Contains(entry.FileType);

        return true;
    }
}
