using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities;
using Brio.Files;
using Brio.MCDF.Game.Services;
using Brio.Resources;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace Brio.Game.Core;

public record class AutoSaveEntry
{
    public required string FolderPath { get; init; }
    public required string DisplayName { get; init; }
    public required bool HasPoses { get; init; }

    public string BrioSavePath => Path.Combine(FolderPath, "SceneAutoSave.brioautosave");
    public bool IsValid => File.Exists(BrioSavePath);
}

public record class AutoSavePoseEntry
{
    public required string ActorName { get; init; }
    public required string FilePath { get; init; }

    public bool IsCompanion => ActorName.EndsWith("-Companion", StringComparison.OrdinalIgnoreCase);
}

public class AutoSaveService : IDisposable, IMediatorSubscriber
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly SceneService _sceneService;
    private readonly MCDFService _mCDFService;
    private readonly Mediator _mediator;

    public Mediator Mediator => _mediator;

    public bool IsEnabled = true;

    public AutoSaveService(IDalamudPluginInterface pluginInterface, IFramework framework, Mediator mediator, SceneService sceneService, MCDFService mCDFService)
    {
        _pluginInterface = pluginInterface;
        _framework = framework;
        _sceneService = sceneService;
        _mCDFService = mCDFService;
        _mediator = mediator;

        _mediator.Subscribe<GposeEndMessage>(this, _ => OnGPoseStateChange(false));
        _mediator.Subscribe<GposeStartMessage>(this, _ => OnGPoseStateChange(true));
    }

    private Timer? _timer = null;
    private string AutoSaveFolder => Path.Combine(_pluginInterface.GetPluginConfigDirectory(), "Data", "AutoSaves");

    public void Start()
    {
        if(Directory.Exists(AutoSaveFolder) == false)
        {
            Directory.CreateDirectory(AutoSaveFolder);
        }

        _timer = new Timer(TimeSpan.FromSeconds(ConfigurationService.Instance.Configuration.AutoSave.AutoSaveInterval));
        _timer.Elapsed += OnElapsed;

        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();

        Brio.Log.Verbose($"AutoSave: Stop");

        if(ConfigurationService.Instance.Configuration.AutoSave.CleanAutoSaveOnLeavingGpose)
        {
            CleanAllSaves();
        }
        else
        {
            CleanOldSaves();
        }
    }

    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        if(IsEnabled)
            _framework.RunOnFrameworkThread(AutoSave);
    }

    private void AutoSave()
    {
        Update();

        if(ConfigurationService.Instance.Configuration.AutoSave.AutoSaveSystemEnabled)
        {
            if(_mCDFService.IsApplyingMCDF)
            {
                Brio.Log.Verbose($"IsApplyingMCDF aborting this autosave ");
                return;
            }

            try
            {
                var scene = _sceneService.GenerateSceneFile();

                byte[] bytes = MessagePackSerializer.Serialize(scene);

                var path = Path.Combine(AutoSaveFolder, $"autosave-{DateTime.Now:yyyy-MM-dd}-{DateTime.Now:hh-mm-ss}");

                Brio.Log.Verbose($"AutoSaving: {path}");

                if(Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }

                File.WriteAllBytes(Path.Combine(path, "SceneAutoSave.brioautosave"), bytes);

                if(ConfigurationService.Instance.Configuration.AutoSave.AutoSaveIndividualPoses)
                {
                    var posespath = Path.Combine(path, "Poses");

                    if(Directory.Exists(posespath) == false)
                    {
                        Directory.CreateDirectory(posespath);
                    }

                    foreach(var actor in scene.Actors)
                    {
                        ResourceProvider.Instance.SaveFileDocument(Path.Combine(posespath, $"{actor.FriendlyName}.pose"), actor.PoseFile);

                        if(actor.HasChild && actor.Child?.PoseFile != null)
                        {
                            ResourceProvider.Instance.SaveFileDocument(Path.Combine(posespath, $"{actor.FriendlyName}-Companion.pose"), actor.Child.PoseFile);
                        }
                    }
                }

                Brio.Log.Verbose($"AutoSaved!");

                CleanOldSaves();
            }
            catch(Exception ex)
            {
                Brio.Log.Error(ex, "Exception AutoSaving!");
            }
        }
    }

    internal void Update()
    {
        Brio.Log.Verbose($"AutoSave: Update");

        var timeInterval = TimeSpan.FromSeconds(ConfigurationService.Instance.Configuration.AutoSave.AutoSaveInterval).TotalMilliseconds;
        if(_timer is not null && _timer.Interval != timeInterval)
        {
            _timer.Interval = timeInterval;
        }
    }

    public IReadOnlyList<AutoSaveEntry> GetAutoSaves()
    {
        if(Directory.Exists(AutoSaveFolder) == false)
            return [];

        return [.. Directory.EnumerateDirectories(AutoSaveFolder)
            .Select(d => new DirectoryInfo(d))
            .OrderByDescending(d => d.LastWriteTime)
            .Select(d => new AutoSaveEntry
            {
                FolderPath = d.FullName,
                DisplayName = $"Auto-Save {d.LastWriteTime:g}",
                HasPoses = Directory.Exists(Path.Combine(d.FullName, "Poses"))
            })];
    }

    public void LoadAutoSave(AutoSaveEntry entry, bool destroyAll = false)
    {
        try
        {
            var result = MessagePackSerializer.Deserialize<SceneFile>(File.ReadAllBytes(entry.BrioSavePath));
            _sceneService.LoadScene(result, destroyAll);
        }
        catch(Exception ex)
        {
            Brio.NotifyError("Brio AutoSave was corrupted!");
            Brio.Log.Error(ex, "Exception while loading an AutoSave!");
        }
    }

    public void LoadPoseOnActor(AutoSavePoseEntry poseEntry, PosingCapability capability)
    {
        capability.ImportPose(poseEntry.FilePath);
    }

    public IReadOnlyList<AutoSavePoseEntry> GetAllAutoSavePoses(AutoSaveEntry entry)
    {
        var posesPath = Path.Combine(entry.FolderPath, "Poses");
        if(Directory.Exists(posesPath) == false)
            return [];

        return [.. Directory.EnumerateFiles(posesPath, "*.pose")
            .OrderBy(f => f)
            .Select(f => new AutoSavePoseEntry
            {
                ActorName = Path.GetFileNameWithoutExtension(f),
                FilePath = f
            })];
    }

    public void CleanOldSaves()
    {
        try
        {
            var saveFolders = Directory.EnumerateDirectories(AutoSaveFolder)
                                 .Select(d => new DirectoryInfo(d))
                                 .OrderByDescending(d => d.LastWriteTime)
                                 .ToList();

            if(saveFolders.Count > ConfigurationService.Instance.Configuration.AutoSave.MaxAutoSaves)
            {

            }

            // Keep the newest files and delete the rest
            var foldersToDelete = saveFolders.Skip(ConfigurationService.Instance.Configuration.AutoSave.MaxAutoSaves);

            foreach(var folder in foldersToDelete)
            {
                folder.Delete(true);
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Exception While Cleaning old AutoSaves!");
        }
    }

    public void CleanAllSaves()
    {
        try
        {
            var saveFolders = Directory.EnumerateDirectories(AutoSaveFolder)
                                 .Select(d => new DirectoryInfo(d))
                                 .ToList();

            foreach(var folder in saveFolders)
            {
                folder.Delete(true);
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Exception While Cleaning all AutoSaves!");
        }
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(newState)
        {
            Brio.Log.Verbose($"AutoSave: GPoseStateChange Start");
            Start();
        }
        else
        {
            Brio.Log.Verbose($"AutoSave: GPoseStateChange Stop");
            Stop();
        }
    }

    public void Dispose()
    {
        Stop();

        Mediator.UnsubscribeAll(this);

        GC.SuppressFinalize(this);
    }
}
