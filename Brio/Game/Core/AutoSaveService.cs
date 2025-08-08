using Brio.Config;
using Brio.Files;
using Brio.Game.GPose;
using Brio.Game.Scene;
using Brio.Resources;
using Brio.UI;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using MessagePack;
using System;
using System.IO;
using System.Linq;
using System.Timers;

namespace Brio.Game.Core;

public class AutoSaveService : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly GPoseService _gPoseService;
    private readonly SceneService _sceneService;

    public bool IsEnabled = true;

    public AutoSaveService(IDalamudPluginInterface pluginInterface, IFramework framework, SceneService sceneService, GPoseService gPoseService)
    {
        _pluginInterface = pluginInterface;
        _framework = framework;
        _gPoseService = gPoseService;
        _sceneService = sceneService;

        gPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
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

    public void ShowAutoSaves()
    {
        UIManager.Instance.FileDialogManager.CustomSideBarItems.Clear();
        UIManager.Instance.FileDialogManager.OpenFileDialog(
            "Load AutoSave",
            ".brioautosave",
            (success, paths) =>
            {
                if(success && paths.Count == 1)
                {
                    string path = paths[0];
                    if(path.IsNullOrEmpty() == false)
                    {
                        try
                        {
                            var result = MessagePackSerializer.Deserialize<SceneFile>(File.ReadAllBytes(path));
                            _sceneService.LoadScene(result, true);
                        }
                        catch(Exception ex)
                        {
                            Brio.NotifyError("Brio AutoSave save was corrupted!");
                            Brio.Log.Error(ex, "Exception while loading an AutoSave!");
                        }
                    }
                }
            },
            1,
            AutoSaveFolder,
            true);
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

    private void GPoseService_OnGPoseStateChange(bool newState)
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
        _gPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
    }
}
