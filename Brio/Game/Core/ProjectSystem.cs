using Brio.Files;
using Brio.Game.GPose;
using Brio.Game.Scene;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Brio.Game.Core;

[MessagePackObject]
public record class Project
{
    [Key(0)] public int Version { get; set; } = 1;

    [Key(1)] public required string Name { get; set; }
    [Key(2)] public string? Description { get; set; }

    [Key(3)] public required string Path { get; set; }
    [Key(4)] public string? ImagePath { get; set; }

    [Key(5)] public DateTime? Created { get; set; }
}

[MessagePackObject]
public class BrioProjects
{
    [Key(0)] public List<Project> Projects { get; set; } = [];
}

public class ProjectSystem : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly GPoseService _gPoseService;
    private readonly SceneService _sceneService;

    public bool IsLoading => _sceneService.IsLoading;

    public bool IsEnabled = true;

    private string ProjectSaveFolder => Path.Combine(_pluginInterface.GetPluginConfigDirectory(), "Data", "Projects");
    private string BrioDataPath => Path.Combine(ProjectSaveFolder, "brio.data");

    public BrioProjects BrioProjects { get; set; } = new BrioProjects();

    public ProjectSystem(IDalamudPluginInterface pluginInterface, IFramework framework, SceneService sceneService, GPoseService gPoseService)
    {
        _pluginInterface = pluginInterface;
        _framework = framework;
        _gPoseService = gPoseService;
        _sceneService = sceneService;

        Directory.CreateDirectory(ProjectSaveFolder);
        
        LoadProjectData();

        gPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }


    public void NewProject(string projectName, string? Description)
    {
        var path = Path.Combine(ProjectSaveFolder, $"{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.brioproj");

        try
        {
            var scene = _sceneService.GenerateSceneFile();

            Brio.Log.Verbose($"saving new project: {path}");

            byte[] bytes = MessagePackSerializer.Serialize(scene);
            File.WriteAllBytes(path, bytes);
         
            BrioProjects.Projects.Add(new Project { Name = projectName, Path = path, Description = Description, Created = DateTime.UtcNow });

            SaveProjectData();
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Exception while saving new project: {projectName}");
        }
    }

    public void LoadProject(Project project, bool destroyAll)
    {
        var result = MessagePackSerializer.Deserialize<SceneFile>(File.ReadAllBytes(project.Path));
        _sceneService.LoadScene(result, destroyAll);
    }

    public void DeleteProject(Project project)
    {
        if(File.Exists(project.Path))
        {
            File.Delete(project.Path);

            BrioProjects.Projects.Remove(project);

            SaveProjectData();
        }
    }

    private void LoadProjectData()
    {
        if(File.Exists(BrioDataPath))
        {
            var bytes = File.ReadAllBytes(BrioDataPath);

            BrioProjects = MessagePackSerializer.Deserialize<BrioProjects>(bytes);
        }
    }

    private void SaveProjectData()
    {
        byte[] bytes = MessagePackSerializer.Serialize(BrioProjects);

        File.WriteAllBytes(BrioDataPath, bytes);
    }

    public void DeleteAllProjects()
    {
        try
        {
            var saveFiles = Directory.EnumerateFiles(ProjectSaveFolder)
                                 .Select(f => new FileInfo(f))
                                 .ToList();

            foreach(var file in saveFiles)
            {
                file.Delete();
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Exception While Cleaning all AutoSaves!");
        }
    }

    private void GPoseService_OnGPoseStateChange(bool newState)
    {

    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
    }
}
