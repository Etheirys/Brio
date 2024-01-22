using Brio.Config;
using System;
using System.Collections.Generic;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private readonly ConfigurationService _configurationService;

    public LibraryManager (ConfigurationService configurationService)
    {
        this._configurationService = configurationService;

        Categories.Add(new FavoritesCategory());
        Categories.Add(new PosesCategory());
        Categories.Add(new CharactersCategory());
    }

    public List<CategoryBase> Categories { get; init; } = new();

    public void Dispose()
    {
    }
}
