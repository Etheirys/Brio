using Brio.Config;
using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Resources;
using Brio.UI.Windows;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private static LibraryManager? _instance;

    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigurationService _configurationService;
    private readonly GameDataProvider _luminaProvider;
    private readonly IFramework _framework;
    private readonly LibraryRoot _rootItem;
    private readonly List<SourceBase> _sources = new();
    private readonly List<SourceBase> _internalSources = new();

    private LibraryWindow? _window;

    public delegate void OnScanFinishedDelegate();
    public event OnScanFinishedDelegate? OnScanFinished;

    public bool IsScanning { get; private set; }
    public bool IsLoadingSources { get; private set; }

    public LibraryManager(
        IServiceProvider serviceProvider,
        ConfigurationService configurationService,
        GameDataProvider luminaProvider,
        IFramework framework)
    {
        _instance = this;
        _serviceProvider = serviceProvider;
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

        OnConfigurationChanged();
    }

    public static void Get<T>(string label, Action<T> callback)
    {
        TypeFilter typeFilter = new(label, typeof(T));
        Get(typeFilter, (r) =>
        {
            if (r is T tr)
            {
                callback.Invoke(tr);
            }
        });
    }

    public static void Get(FilterBase filter, Action<object> callback)
    {
        if(_instance == null || _instance._window == null)
            return;

        _instance._window.OpenModal(filter, (ItemEntryBase item) =>
        {
            object? result = item.Load();

            if(result == null)
                return;

            callback.Invoke(result);
        });
    }

    public void AddSource(SourceBase source)
    {
        _sources.Add(source);
    }

    private void AddInternalSource(SourceBase source)
    {
        _internalSources.Add(source);
    }

    public void RegisterWindow(LibraryWindow window)
    {
        _window = window;
    }

    private void OnConfigurationChanged()
    {
        if(IsLoadingSources || IsScanning)
            return;

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
        IsLoadingSources = true;

        _rootItem.Clear();
        _sources.Clear();
        
        // Directory configurations
        foreach(var sourceConfig in _configurationService.Configuration.Library.Files)
        {
            if(!sourceConfig.Enabled)
                continue;

            AddSource(new FileSource(this, sourceConfig));
        }

        foreach(SourceBase source in _internalSources)
        {
            _rootItem.Add(source);
        }

        foreach (SourceBase source in _sources)
        {
            _rootItem.Add(source);
        }

        Scan();

        IsLoadingSources = false;
    }

    public void Scan()
    {
        Task.Run(ScanAsync);
    }

    public async Task ScanAsync()
    {
        lock(this)
        {
            if(IsScanning)
                return;

            IsScanning = true;
        }

        try
        {
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
        }
        catch (Exception ex)
        {
            Brio.Log.Error(ex, "Error during library scan");
        }

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
