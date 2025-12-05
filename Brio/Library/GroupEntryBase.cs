using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using System;
using System.Collections.Generic;

namespace Brio.Library;

/// <summary>
/// An group entry is an entry in the library that contains other entries, such as a directory or folder.
/// </summary>
public abstract class GroupEntryBase : EntryBase
{
    private List<EntryBase> _allEntries = new List<EntryBase>();
    private List<EntryBase> _filteredEntries = new List<EntryBase>();

    protected GroupEntryBase(SourceBase? source)
        : base(source)
    {
    }

    public IEnumerable<EntryBase>? FilteredEntries => this._filteredEntries;
    public IEnumerable<EntryBase>? AllEntries => this._allEntries;

    public void Add(EntryBase entry)
    {
        _allEntries.Add(entry);
    }

    public void Clear()
    {
        _allEntries.Clear();
        _filteredEntries.Clear();
    }

    public override bool PassesFilters(params FilterBase[] filters)
    {
        return _filteredEntries.Count > 0;
    }

    public void FilterEntries(params FilterBase[] filters)
    {
        if(_allEntries.Count <= 0)
            return;

        try
        {
            _filteredEntries.Clear();

            if(filters.Length <= 0)
            {
                _filteredEntries.AddRange(_allEntries);
            }
            else
            {
                foreach(EntryBase entry in _allEntries)
                {
                    if(entry == null)
                        continue;

                    if(entry is GroupEntryBase dir)
                    {
                        dir.FilterEntries(filters);
                    }

                    if(entry.PassesFilters(filters))
                    {
                        _filteredEntries.Add(entry);
                    }
                }
            }
        }
        catch(System.Exception ex)
        {
#if DEBUG
            Brio.Log.Error(ex, "Exception while filtering entries");
#else
            _ = ex;
#endif
        }
    }

    public IEnumerable<EntryBase>? GetFilteredEntries(bool flatten)
    {
        if(!flatten)
            return this.FilteredEntries;

        List<EntryBase> entries = new();
        Flatten(ref entries);
        return entries;
    }

    private void Flatten(ref List<EntryBase> entries)
    {
        if(this.FilteredEntries == null)
            return;

        foreach(EntryBase entry in this.FilteredEntries)
        {
            if(entry is GroupEntryBase dir)
            {
                dir.Flatten(ref entries);
            }
            else
            {
                entries.Add(entry);
            }
        }
    }

    public void GetAllTags(ref TagCollection tags)
    {
        if(this.FilteredEntries == null)
            return;

        try
        {
            foreach(EntryBase entry in this.FilteredEntries)
            {
                if(entry.Tags != null)
                {
                    tags.AddRange(entry.Tags);
                }

                if(entry is GroupEntryBase dir)
                {
                    dir.GetAllTags(ref tags);
                }
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Exception while getting all tags from group entry");
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach(EntryBase entry in _allEntries)
        {
            if(entry == null)
                continue;

            entry.Dispose();
        }
    }
}
