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
    private readonly List<SourceBase> _sources = new();
    private readonly List<SourceBase> _internalSources = new();

    public bool IsScanning { get; private set; }

    public LibraryManager (ConfigurationService configurationService, GameDataProvider luminaProvider, IPluginLog log)
    {
        _configurationService = configurationService;
        _luminaProvider = luminaProvider;
        _log = log;
        _rootItem = new();

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;

        // Game Data
        _internalSources.Add(new GameDataNpcSource(_luminaProvider));
        _internalSources.Add(new GameDataMountSource(_luminaProvider));
        _internalSources.Add(new GameDataCompanionSource(_luminaProvider));
        _internalSources.Add(new GameDataOrnamentSource(_luminaProvider));

        // TODO: swap this for a package
        _internalSources.Add(new FileSource("Standard Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Anamnesis", "StandardPoses"));

        LoadSources();
        Scan();
    }

    private void OnConfigurationChanged()
    {
        LoadSources();
        Scan();
    }

    public GroupEntryBase Root => _rootItem;

    public void Dispose()
    {
        foreach(SourceBase source in _internalSources)
        {
            source.Dispose();
        }

        foreach(SourceBase source in _sources)
        {
            source.Dispose();
        }
    }

    public void LoadSources()
    {
        _rootItem.Clear();
        _sources.Clear();

        // Internal sources
        _sources.AddRange(_internalSources);
        
        // Directory configurations
        foreach((string name, string path) in _configurationService.Configuration.Library.Directories)
        {
            _sources.Add(new FileSource(name, path));
        }

        foreach (SourceBase source in _sources)
        {
            _rootItem.Add(source);
        }
    }

    public void Scan()
    {
        Task.Run(ScanAsync);
    }

    public async Task ScanAsync()
    {
        IsScanning = true;

        List<Task> scanTasks = new();
        foreach(SourceBase source in _internalSources)
        {
            scanTasks.Add(Task.Run(() => ScanSource(source)));
        }

        foreach(SourceBase source in _sources)
        {
            scanTasks.Add(Task.Run(() => ScanSource(source)));
        }

        await Task.WhenAll(scanTasks.ToArray());

        IsScanning = false;
    }

    private void ScanSource(SourceBase source)
    {
        try
        {
            source.Clear();
            source.Scan();
        }
        catch(Exception ex)
        {
            _log.Error(ex, $"Error in library source: {source.Name}");
        }
    }
}

internal class LibraryRoot : GroupEntryBase
{
    public LibraryRoot()
        : base(null)
    {
    }

    public override string Name => "Library";
    public override IDalamudTextureWrap? Icon => null;
}
