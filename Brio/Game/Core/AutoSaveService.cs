using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Game.GPose;
using Brio.MCDF.Game.Services;
using Brio.Resources;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Brio.Services.Models;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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
    public required int PoseCount { get; init; }

    public required string SavedAtDelta { get; init; }

    public string BrioSavePath => Path.Combine(FolderPath, "SceneAutoSave.brioautosave");
    public bool IsValid => File.Exists(BrioSavePath);
}

public record class AutoSavePoseEntry
{
    public required string ActorName { get; init; }
    public required string FilePath { get; init; }

    public bool IsCompanion => ActorName.EndsWith("-Companion", StringComparison.OrdinalIgnoreCase);
}

public class AutoSaveService : MediatorSubscriberBase, IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly SceneService _sceneService;
    private readonly MCDFService _mCDFService;
    private readonly Mediator _mediator;
    private readonly GPoseService _gPoseService;

    public bool IsEnabled { get; set; } = true;

    public AutoSaveService(IDalamudPluginInterface pluginInterface, IFramework framework, GPoseService gPoseService, Mediator mediator, SceneService sceneService, MCDFService mCDFService) : base(mediator)
    {
        _pluginInterface = pluginInterface;
        _framework = framework;
        _sceneService = sceneService;
        _mCDFService = mCDFService;
        _mediator = mediator;
        _gPoseService = gPoseService;

        if(_gPoseService.IsGPosing)
            Start();

        _mediator.Subscribe<GposeEndMessage>(this, _ => OnGPoseStateChange(false));
        _mediator.Subscribe<GposeStartMessage>(this, _ => OnGPoseStateChange(true));
    }

    private Timer? _timer = null;
    private string AutoSaveFolder => Path.Combine(_pluginInterface.GetPluginConfigDirectory(), "Data", "AutoSaves");

    public void Start()
    {
        _timer?.Dispose();

        if(!Directory.Exists(AutoSaveFolder))
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
        if(IsEnabled && _gPoseService.IsGPosing)
        {
            _framework.RunOnFrameworkThread(AutoSave);
        }
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
                var scene = _sceneService.CaptureScene();

                byte[] bytes = _sceneService.Serialize(scene);

                var now = DateTime.Now;
                var path = Path.Combine(AutoSaveFolder, $"autosave-{now:yyyy-MM-dd}-{now:HH-mm-ss-ff}");

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
            .Select(d =>
            {
                var posesPath = Path.Combine(d.FullName, "Poses");

                bool hasPoses = Directory.Exists(posesPath);
                int poseCount = hasPoses
                    ? Directory.EnumerateFiles(posesPath, "*.pose").Count()
                    : 0;


                var timeDelta = DateTime.Now - d.LastWriteTime;
                var savedAt = string.Empty;
                if(timeDelta.TotalSeconds < 60)
                    savedAt = $"{(int)timeDelta.TotalSeconds}s ago";
                if(timeDelta.TotalMinutes < 60)
                    savedAt = $"{(int)timeDelta.TotalMinutes}m {timeDelta.Seconds}s ago";
                if(timeDelta.TotalHours < 24)
                    savedAt = $"{(int)timeDelta.TotalHours}h {timeDelta.Minutes}m ago";
                else
                    savedAt = $"{(int)timeDelta.TotalDays}d {timeDelta.Hours}h ago";

                return new AutoSaveEntry
                {
                    FolderPath = d.FullName,
                    DisplayName = $"Auto-Save {d.LastWriteTime:g}",
                    SavedAtDelta = savedAt,
                    HasPoses = hasPoses,
                    PoseCount = poseCount
                };
            })];
    }

    public void LoadAutoSave(AutoSaveEntry entry, bool destroyAll = false, bool useRelativeLightPositions = true, bool useRelativeWorldObjectPositions = true, SceneImportOptions importOptions = SceneImportOptions.Default)
    {
        try
        {
            var result = _sceneService.Deserialize(File.ReadAllBytes(entry.BrioSavePath));
            _sceneService.ImportScene(result, destroyAll, useRelativeLightPositions, useRelativeWorldObjectPositions, importOptions);
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
        if(!Directory.Exists(AutoSaveFolder)) 
            return;

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

    public override void Dispose()
    {
        base.Dispose();

        Stop();
        _timer?.Dispose();

        GC.SuppressFinalize(this);
    }
}
