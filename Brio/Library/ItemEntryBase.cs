using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Dalamud.Interface.Internal;
using System;

namespace Brio.Library;

/// <summary>
/// An item entry is an entry in teh library that the user can load, or otherwise perform actions on.
/// </summary>
internal abstract class ItemEntryBase : EntryBase
{
    protected ItemEntryBase(SourceBase? source)
        : base(source)
    {
    }

    public virtual string? Description { get; }
    public virtual string? Author { get; }
    public virtual string? Version { get; }
    public virtual IDalamudTextureWrap? PreviewImage { get; }
    public abstract Type LoadsType { get; }

    public override bool PassesFilters(params FilterBase[] filters)
    {
        foreach(FilterBase filter in filters)
        {
            if(!filter.Filter(this))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Search(string[] query)
    {
        bool match = base.Search(query);
        match |= SearchUtility.Matches(this.Description, query);
        match |= SearchUtility.Matches(this.Author, query);
        match |= SearchUtility.Matches(this.Version, query);
        return match;
    }
}
