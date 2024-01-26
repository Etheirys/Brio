using Brio.Files;
using Brio.Library.Actions;
using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Dalamud.Interface.Internal;
using System.Collections.Generic;

namespace Brio.Library;

/// <summary>
/// An entry is a library object
/// </summary>
internal abstract class EntryBase : ITagged
{
    private SourceBase? _source;

    public EntryBase(SourceBase? source)
    {
        _source = source;
    }

    public abstract string Name { get; }
    public abstract IDalamudTextureWrap? Icon { get; }

    public virtual bool IsVisible { get; set; }
    public TagCollection Tags { get; init; } = new();
    public SourceBase? Source => _source;
    public string? SourceInfo { get; set; }
    public List<EntryActionBase> Actions { get; init; } = new();

    public abstract bool PassesFilters(params FilterBase[] filters);

    public virtual bool Search(string[] query)
    {
        return SearchUtility.Matches(this.Name, query);
    }

    public virtual void Dispose()
    {
    }
}
