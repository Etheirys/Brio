using Brio.Library.Sources;
using Brio.Library.Tags;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;

namespace Brio.Library;

public abstract class LibraryEntryBase : ILibraryEntry, ITagged
{
    private List<ILibraryEntry> _allEntries = new List<ILibraryEntry>();
    private List<ILibraryEntry> _filteredEntries = new List<ILibraryEntry>();
    private SourceBase? _source;

    public LibraryEntryBase(SourceBase? source)
    {
        _source = source;
    }

    public IEnumerable<ILibraryEntry>? FilteredEntries => this._filteredEntries;
    public IEnumerable<ILibraryEntry>? AllEntries => this._allEntries;

    public abstract string Name { get; }
    public abstract IDalamudTextureWrap? Icon { get; }
    public virtual IDalamudTextureWrap? PreviewImage => null;
    public virtual Type? FileType => null;

    public virtual bool IsVisible { get; set; }
    public TagCollection Tags { get; init; } = new();
    public SourceBase? Source => _source;

    public void Add(ILibraryEntry entry)
    {
        _allEntries.Add(entry);
    }

    public bool PassesFilters(params LibraryFilterBase[] filters)
    {
        if(FileType != null)
        {
            foreach(LibraryFilterBase filter in filters)
            {
                if(!filter.Filter(this))
                {
                    return false;
                }
            }
        }
        else if(_filteredEntries.Count <= 0)
        {
            return false;
        }

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

    public IEnumerable<ILibraryEntry>? GetFilteredEntries(bool flatten)
    {
        if(!flatten)
            return this.FilteredEntries;

        List<ILibraryEntry> entries = new();
        Flatten(ref entries);
        return entries;
    }

    private void Flatten(ref List<ILibraryEntry> entries)
    {
        if(this.FilteredEntries == null)
            return;

        foreach(ILibraryEntry entry in this.FilteredEntries)
        {
            if(entry.FileType != null)
            {
                entries.Add(entry);
            }

            if (entry is LibraryEntryBase entryBase)
            {
                entryBase.Flatten(ref entries);
            }
        }
    }

    public virtual void Dispose()
    {
        foreach(ILibraryEntry entry in _allEntries)
        {
            entry.Dispose();
        }
    }
}
