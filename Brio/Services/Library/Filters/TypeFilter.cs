using System;
using System.Collections.Generic;

namespace Brio.Library.Filters;

public class TypeFilter : FilterBase
{
    private HashSet<Type> _types = new();

    public TypeFilter(string name, params Type[] loadTypes)
        : base(name)
    {
        foreach(Type type in loadTypes)
        {
            _types.Add(type);
        }
    }

    public IEnumerable<Type> Types => _types;

    public override void Clear()
    {
        _types.Clear();
    }

    public override bool Filter(EntryBase entry)
    {
        if(entry is ItemEntryBase file)
        {
            return _types.Contains(file.LoadsType);
        }


        return true;
    }
}
