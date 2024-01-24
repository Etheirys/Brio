using Brio.Config;
using Brio.Library.Sources;
using Brio.Resources;
using Dalamud.Interface.Internal;
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
        AddSource(new FileSource("Brio Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Poses"));
        AddSource(new FileSource("Brio Characters", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Characters"));
        AddSource(new FileSource("Anamnesis Poses", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Poses"));
        AddSource(new FileSource("Anamnesis Characters", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Characters"));

        // TODO: swap this for a package
        AddSource(new FileSource("Standard Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Anamnesis", "StandardPoses"));

        // Game Data
        AddSource(new GameDataNpcSource(_luminaProvider));
        AddSource(new GameDataMountSource(_luminaProvider));
        AddSource(new GameDataCompanionSource(_luminaProvider));
        AddSource(new GameDataOrnamentSource(_luminaProvider));

        Scan();
    }

    public List<SourceBase> Sources { get; init; } = new();
    public ILibraryEntry Root => _rootItem;

    public void Dispose()
    {
        foreach(SourceBase source in Sources)
        {
            source.Dispose();
        }
    }

    public void AddSource(SourceBase source)
    {
        Sources.Add(source);
        _rootItem.Add(source);
    }

    public void Scan()
    {
        Task.Run(ScanAsync);
    }

    public async Task ScanAsync()
    {
        IsScanning = true;

        List<Task> scanTasks = new();
        foreach(SourceBase source in Sources)
        {
            scanTasks.Add(Task.Run(()=> ScanSource(source)));
        }

        await Task.WhenAll(scanTasks.ToArray());

        IsScanning = false;
    }

    private void ScanSource(SourceBase source)
    {
        try
        {
            source.Scan();
        }
        catch(Exception ex)
        {
            _log.Error(ex, $"Error in library source: {source.Name}");
        }
    }
}

public class LibraryRoot : LibraryEntryBase
{
    public LibraryRoot()
        : base(null)
    {
    }

    public override string Name => "Library";
    public override IDalamudTextureWrap? Icon => null;
}
