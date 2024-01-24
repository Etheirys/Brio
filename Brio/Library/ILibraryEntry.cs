using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;

namespace Brio.Library;

public interface ILibraryEntry : IDisposable, ITagged
{
    public string Name { get; }
    public string? Description { get; }
    public string? Author { get; }
    public string? Version { get; }

    public IDalamudTextureWrap? Icon { get; }
    public IDalamudTextureWrap? PreviewImage { get; }
    public Type? FileType { get; }
    public SourceBase? Source { get; }
    public string? SourceInfo { get; }
    public bool IsVisible { get; set; }

    public IEnumerable<ILibraryEntry>? FilteredEntries { get; }
    public IEnumerable<ILibraryEntry>? AllEntries { get; }

    public void Add(ILibraryEntry entry);
    public bool PassesFilters(params FilterBase[] filters);
    public void FilterEntries(params FilterBase[] filters);
    public IEnumerable<ILibraryEntry>? GetFilteredEntries(bool flatten);
    public void GetAllTags(ref TagCollection tags);
}
