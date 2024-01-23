﻿using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;

namespace Brio.Library;

public interface ILibraryEntry
{
    public string Name { get; }
    public IEnumerable<ILibraryEntry>? FilteredEntries { get; }
    public IEnumerable<ILibraryEntry>? AllEntries { get; }
    public IDalamudTextureWrap? Icon { get; }
    public Type? FileType { get; }

    public void Add(ILibraryEntry entry);
    public bool PassesFilters(params LibraryFilterBase[] filters);
    public void FilterEntries(params LibraryFilterBase[] filters);
}
