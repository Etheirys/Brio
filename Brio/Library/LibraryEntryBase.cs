﻿using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;

namespace Brio.Library;

public abstract class LibraryEntryBase : ILibraryEntry
{
    private List<ILibraryEntry> _allEntries = new List<ILibraryEntry>();
    private List<ILibraryEntry> _filteredEntries = new List<ILibraryEntry>();

    public IEnumerable<ILibraryEntry>? FilteredEntries => this._filteredEntries;
    public IEnumerable<ILibraryEntry>? AllEntries => this._allEntries;

    public abstract string Name { get; }
    public abstract IDalamudTextureWrap? Icon { get; }
    public virtual Type? FileType => null;

    public void Add(ILibraryEntry entry)
    {
        _allEntries.Add(entry);
    }

    public bool PassesFilters(params LibraryFilterBase[] filters)
    {
        foreach(LibraryFilterBase filter in filters)
        {
            if(!filter.Filter(this))
            {
                return false;
            }
        }

        if(FileType == null && _filteredEntries.Count <= 0)
            return false;

        return true;
    }

    public void FilterEntries(params LibraryFilterBase[] filters)
    {
        if(_allEntries.Count <= 0)
            return;

        _filteredEntries.Clear();

        if(filters.Length <= 0)
        {
            _filteredEntries.AddRange(_allEntries);
        }
        else
        {
            foreach(ILibraryEntry entry in _allEntries)
            {
                entry.FilterEntries(filters);

                if(entry.PassesFilters(filters))
                {
                    _filteredEntries.Add(entry);
                }
            }
        }
    }
}
