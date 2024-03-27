using Brio.Config;
using Brio.Files;
using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.UI;
using Brio.UI.Windows;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private static LibraryManager? _instance;

    private readonly FileService _fileService;
    private readonly ConfigurationService _configurationService;
    private readonly IFramework _framework;
    private readonly LibraryRoot _rootItem;
    private readonly List<SourceBase> _sources = new();
    private readonly IEnumerable<SourceBase> _internalSources;

    private LibraryWindow? _window;

    public delegate void OnScanFinishedDelegate();
    public event OnScanFinishedDelegate? OnScanFinished;

    public bool IsScanning { get; private set; }
    public bool IsLoadingSources { get; private set; }

    public LibraryManager(
        FileService fileService,
        ConfigurationService configurationService,
        IFramework framework,
        IEnumerable<SourceBase> internalSources)
    {
        _instance = this;
        _fileService = fileService;
        _configurationService = configurationService;
        _framework = framework;
        _rootItem = new();
        _internalSources = internalSources;

        _configurationService.Configuration.Library.ReEstablishDefaultPaths();
        _configurationService.OnConfigurationChanged += OnConfigurationChanged;

        OnConfigurationChanged();
    }

    public static void Get<T>(string label, Action<T> callback)
    {
        TypeFilter typeFilter = new(label, typeof(T));
        Get(typeFilter, (r) =>
        {
            if(r is T tr)
            {
                callback.Invoke(tr);
            }
        });
    }

    public static void Get(FilterBase filter, Action<object> callback)
    {
        if(_instance == null || _instance._window == null)
            return;

        _instance._window.OpenModal(filter, callback);
    }

    public void AddSource(SourceBase source)
    {
        _sources.Add(source);
    }

    public void RegisterWindow(LibraryWindow window)
    {
        _window = window;
    }

    public void ShowFilePicker(FilterBase filter, Action<object> callback)
    {
        string title = $"Import {filter.Name}###import_browse";

        // Build the filter string for Dalamud's file picker
        // "Pose File (*.pose | *.cmp){.pose,.cmp}"
        StringBuilder filterBuilder = new();
        StringBuilder typeIdBuilder = new();

        if(filter is TypeFilter typeFilter)
        {
            List<FileTypeInfoBase> allInfos = new();
            foreach(Type filterType in typeFilter.Types)
            {
                FileTypeInfoBase? typeInfo = _fileService.GetFileTypeInfo(filterType);
                if(typeInfo == null)
                    continue;

                allInfos.Add(typeInfo);
            }

            foreach(FileTypeInfoBase typeInfo in allInfos)
            {
                typeIdBuilder.Append(typeInfo.Extension);
            }

            filterBuilder.Append("Any File(");
            for(int i = 0; i < allInfos.Count; i++)
            {
                if(i > 0)
                    filterBuilder.Append(" | ");

                filterBuilder.Append("*");
                filterBuilder.Append(allInfos[i].Extension);
            }

            filterBuilder.Append("){");
            foreach(FileTypeInfoBase typeInfo in allInfos)
            {
                filterBuilder.Append(typeInfo.Extension);
                filterBuilder.Append(",");
            }
            filterBuilder.Append("},");

            foreach(FileTypeInfoBase typeInfo in allInfos)
            {
                filterBuilder.Append(",");
                filterBuilder.Append(typeInfo.Name);
                filterBuilder.Append(" (*");
                filterBuilder.Append(typeInfo.Extension);
                filterBuilder.Append("){");
                filterBuilder.Append(typeInfo.Extension);
                filterBuilder.Append("}");

            }
        }

        // Add the file source directories as shortcuts
        UIManager.Instance.FileDialogManager.CustomSideBarItems.Clear();
        foreach(SourceBase source in _sources)
        {
            if(source is FileSource fs)
            {
                if(!Directory.Exists(fs.DirectoryPath))
                    continue;

                UIManager.Instance.FileDialogManager.CustomSideBarItems.Add((fs.Name, fs.DirectoryPath, FontAwesomeIcon.FolderClosed, 0));
            }
        }

        // Get the last directory the user used for these types
        string typesId = typeIdBuilder.ToString();
        string? lastDirectory = null;
        var lastDirectories = _configurationService.Configuration.Library.LastBrowsePaths;
        lastDirectories.TryGetValue(typesId, out lastDirectory);

        // Show the dalamud file picker
        UIManager.Instance.FileDialogManager.OpenFileDialog(
            title,
            filterBuilder.ToString(),
            (success, paths) =>
            {
                if(success && paths.Count == 1)
                {
                    var path = paths[0];
                    object? result = _fileService.Load(path);

                    if(result == null)
                        return;

                    string? dir = Path.GetDirectoryName(path);

                    if(dir != null)
                    {
                        if(!lastDirectories.ContainsKey(typesId))
                            lastDirectories.Add(typesId, dir);

                        lastDirectories[typesId] = dir;
                        _configurationService.Save();
                    }

                    callback.Invoke(result);
                }
            },
            1,
            lastDirectory,
            true);
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
        Tag.ClearTagCache();

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

            AddSource(new FileSource(_fileService, sourceConfig));
        }

        foreach(SourceBase source in _internalSources)
        {
            _rootItem.Add(source);
        }

        foreach(SourceBase source in _sources)
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
        catch(Exception ex)
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
