using Brio.Resources;
using Dalamud.Interface.Internal;
using System.Collections.Generic;

namespace Brio.Library;

public abstract class LibraryProviderBase : ILibraryEntry
{
    public string Name { get; protected set; } = string.Empty;
    public IEnumerable<ILibraryEntry>? Entries => this._entries;
    public IDalamudTextureWrap? Icon { get; protected set; }

    private List<ILibraryEntry> _entries = new List<ILibraryEntry>();

    public abstract void Scan();

    public void Add(ILibraryEntry entry)
    {
        _entries.Add(entry);
    }
}
