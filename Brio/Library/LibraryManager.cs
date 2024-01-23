using Brio.Config;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly LibraryRoot _rootItem;

    public LibraryManager (ConfigurationService configurationService)
    {
        _configurationService = configurationService;
        _rootItem = new();

        // TODO: add a configuration option to set the locations of these
        AddProvider(new LibraryFileProvider("Brio Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Poses"));
        AddProvider(new LibraryFileProvider("Brio Characters", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Characters"));
        AddProvider(new LibraryFileProvider("Anamnesis Poses", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Poses"));
        AddProvider(new LibraryFileProvider("Anamnesis Characters", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Characters"));

        // TODO: swap this for a package
        AddProvider(new LibraryFileProvider("Standard Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Anamnesis", "StandardPoses"));

        Scan();
    }

    public List<LibraryProviderBase> Providers { get; init; } = new();
    public ILibraryEntry Root => _rootItem;

    public void Dispose()
    {
    }

    public void AddProvider(LibraryProviderBase provider)
    {
        Providers.Add(provider);
        _rootItem.Add(provider);
    }

    private void Scan()
    {
        foreach (LibraryProviderBase provider in Providers)
        {
            provider.Scan();
        }
    }
}

public class LibraryRoot : LibraryEntryBase
{
    public override string Name => "Library";
    public override IDalamudTextureWrap? Icon => null;
}
