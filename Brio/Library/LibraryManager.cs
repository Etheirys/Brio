using Brio.Config;
using Brio.Library.Actions;
using Brio.Library.Sources;
using Brio.Resources;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly GameDataProvider _luminaProvider;
    private readonly IFramework _framework;
    private readonly LibraryRoot _rootItem;
    private readonly List<SourceBase> _sources = new();
    private readonly List<SourceBase> _internalSources = new();
    private readonly HashSet<EntryActionBase> _actions = new();

    public delegate void OnScanFinishedDelegate();
    public event OnScanFinishedDelegate? OnScanFinished;

    public bool IsScanning { get; private set; }

    public LibraryManager(ConfigurationService configurationService, GameDataProvider luminaProvider, IFramework framework)
    {
        _configurationService = configurationService;
        _luminaProvider = luminaProvider;
        _framework = framework;
        _rootItem = new(this, configurationService);

        _configurationService.Configuration.Library.CheckDefaults();
        _configurationService.OnConfigurationChanged += OnConfigurationChanged;

        // Game Data
        AddInternalSource(new GameDataNpcSource(this, _luminaProvider));
        AddInternalSource(new GameDataMountSource(this, _luminaProvider));
        AddInternalSource(new GameDataCompanionSource(this, _luminaProvider));
        AddInternalSource(new GameDataOrnamentSource(this, _luminaProvider));

        // TODO: swap this for a package
        AddInternalSource(new FileSource(this, "Standard Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Anamnesis", "StandardPoses"));

        LoadSources();
        Scan();
    }

    public void AddSource(SourceBase source)
    {
        _sources.Add(source);
    }

    private void AddInternalSource(SourceBase source)
    {
        _internalSources.Add(source);
    }

    public void RegisterAction(EntryActionBase action)
    {
        _actions.Add(action);
    }

    public void UnregisterAction(EntryActionBase action)
    {
        _actions.Remove(action);
    }

    public List<EntryActionBase> GetActions(EntryBase entry)
    {
        List<EntryActionBase> results = new();
        foreach (EntryActionBase action in _actions)
        {
            if (action.Filter(entry))
            {
                results.Add(action);
            }
        }

        return results;
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
        Brio.Log.Info("Loading library sources");

        _rootItem.Clear();
        _sources.Clear();

        // Internal sources
        _sources.AddRange(_internalSources);
        
        // Directory configurations
        foreach(var sourceConfig in _configurationService.Configuration.Library.Files)
        {
            if(!sourceConfig.Enabled)
                continue;

            AddSource(new FileSource(this, sourceConfig));
        }

        foreach (SourceBase source in _sources)
        {
            _rootItem.Add(source);
        }

        Scan();
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

        await _framework.RunOnFrameworkThread(() =>
        {
            OnScanFinished?.Invoke();
        });
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
            Brio.Log.Error(ex, $"Error in library source: {source.Name}");
        }
    }
}
