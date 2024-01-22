using Brio.Config;
using System;
using System.Collections.Generic;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly List<CategoryBase> _categories = new();

    public LibraryManager (ConfigurationService configurationService)
    {
        this._configurationService = configurationService;

        _categories.Add(new FavoritesCategory());
        _categories.Add(new PosesCategory());
        _categories.Add(new CharactersCategory());
    }

    public void Dispose()
    {
    }

    public IEnumerable<CategoryBase> Categories => _categories;
}
