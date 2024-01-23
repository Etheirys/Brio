using Brio.Config;
using Brio.Resources;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;

namespace Brio.Library;

internal class LibraryManager : IDisposable, ILibraryEntry
{
    private readonly ConfigurationService _configurationService;

    public LibraryManager (ConfigurationService configurationService)
    {
        this._configurationService = configurationService;

        Categories.Add(new FavoritesCategory());
        Categories.Add(new PosesCategory());
        Categories.Add(new CharactersCategory());

        // TODO: add a configuration option to set the locations of these
        Providers.Add(new LibraryFileProvider("Brio Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Poses"));
        Providers.Add(new LibraryFileProvider("Brio Characters", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Characters"));
        Providers.Add(new LibraryFileProvider("Anamnesis Poses", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Poses"));
        Providers.Add(new LibraryFileProvider("Anamnesis Characters", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Characters"));

        Scan();
    }

    public List<CategoryBase> Categories { get; init; } = new();
    public List<LibraryProviderBase> Providers { get; init; } = new();
    public string Name => "Library";

    public IEnumerable<ILibraryEntry>? Entries => this.Providers;
    public IDalamudTextureWrap? Icon => null;

    public void Add(ILibraryEntry entry)
    {
        if (entry is LibraryProviderBase provider)
        {
            Providers.Add(provider);
        }
    }

    public void Dispose()
    {
    }

    private void Scan()
    {
        foreach (LibraryProviderBase provider in Providers)
        {
            provider.Scan();
        }
    }
}

public interface ILibraryEntry
{
    public string Name { get; }
    public IEnumerable<ILibraryEntry>? Entries { get; }
    public IDalamudTextureWrap? Icon { get; }
    public void Add(ILibraryEntry entry);
}
