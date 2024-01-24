using Brio.Config;
using Brio.Resources;
using Dalamud.Interface.Internal;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly GameDataProvider _luminaProvider;
    private readonly IPluginLog _log;
    private readonly LibraryRoot _rootItem;

    public bool IsScanning { get; private set; }

    public LibraryManager (ConfigurationService configurationService, GameDataProvider luminaProvider, IPluginLog log)
    {
        _configurationService = configurationService;
        _luminaProvider = luminaProvider;
        _log = log;
        _rootItem = new();

        // TODO: add a configuration option to set the locations of these
        AddProvider(new LibraryFileProvider("Brio Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Poses"));
        AddProvider(new LibraryFileProvider("Brio Characters", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Characters"));
        AddProvider(new LibraryFileProvider("Anamnesis Poses", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Poses"));
        AddProvider(new LibraryFileProvider("Anamnesis Characters", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Characters"));

        // TODO: swap this for a package
        AddProvider(new LibraryFileProvider("Standard Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Anamnesis", "StandardPoses"));

        // Game Data
        AddProvider(new LibraryGameDataNpcProvider(_luminaProvider));
        AddProvider(new LibraryGameDataMountProvider(_luminaProvider));
        AddProvider(new LibraryGameDataCompanionsProvider(_luminaProvider));
        AddProvider(new LibraryGameDataOrnamentsProvider(_luminaProvider));

        Scan();
    }

    public List<LibraryProviderBase> Providers { get; init; } = new();
    public ILibraryEntry Root => _rootItem;

    public void Dispose()
    {
        foreach(LibraryProviderBase provider in Providers)
        {
            provider.Dispose();
        }
    }

    public void AddProvider(LibraryProviderBase provider)
    {
        Providers.Add(provider);
        _rootItem.Add(provider);
    }

    private void Scan()
    {
        Task.Run(ScanAsync);
    }

    private async Task ScanAsync()
    {
        IsScanning = true;

        List<Task> scanTasks = new();
        foreach(LibraryProviderBase provider in Providers)
        {
            scanTasks.Add(Task.Run(()=> ScanProvider(provider)));
        }

        await Task.WhenAll(scanTasks.ToArray());

        IsScanning = false;
    }

    private void ScanProvider(LibraryProviderBase provider)
    {
        try
        {
            provider.Scan();
        }
        catch(Exception ex)
        {
            _log.Error(ex, $"Error in library provider: {provider.Name}");
        }
    }
}

public class LibraryRoot : LibraryEntryBase
{
    public override string Name => "Library";
    public override IDalamudTextureWrap? Icon => null;
}
