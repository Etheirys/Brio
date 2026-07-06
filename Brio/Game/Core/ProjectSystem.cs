using Brio.Game.GPose;
using Brio.Services;
using Brio.Services.Models;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Brio.Game.Core;

[MessagePackObject]
public class BrioProjects
{
    [Key(0)] public List<Project> Projects { get; set; } = [];
}

[MessagePackObject]
public record class Project
{
    [Key(0)] public int Version { get; set; } = 2;

    [Key(1)] public required string Name { get; set; }
    [Key(2)] public string? Description { get; set; }

    [Key(3)] public required string Path { get; set; }
    [Key(4)] public string? ImagePath { get; set; }

    [Key(5)] public DateTime? Created { get; set; }
    [Key(6)] public DateTime? LastModified { get; set; }
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

    public Project? CurrentProject { get; private set; }

    public ProjectSystem(IDalamudPluginInterface pluginInterface, IFramework framework, SceneService sceneService, GPoseService gPoseService)
    {
        _pluginInterface = pluginInterface;
        _framework = framework;
        _gPoseService = gPoseService;
        _sceneService = sceneService;

        Directory.CreateDirectory(ProjectSaveFolder);

        LoadProjectData();

        gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }


    public void NewProject(string projectName, string? Description)
    {
        var path = Path.Combine(ProjectSaveFolder, $"{projectName}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.brioproj");

        try
        {
            var scene = _sceneService.CaptureScene();

            Brio.Log.Verbose($"saving new project: {path}");

            byte[] bytes = _sceneService.Serialize(scene);
            File.WriteAllBytes(path, bytes);

            var project = new Project { Name = projectName, Path = path, Description = Description, Created = DateTime.UtcNow };
            BrioProjects.Projects.Add(project);

            SaveProjectData();

            CurrentProject = project;
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Exception while saving new project: {projectName}");
        }
    }

    public void SaveProject(Project project)
    {
        try
        {
            var scene = _sceneService.CaptureScene();

            byte[] bytes = _sceneService.Serialize(scene);
            File.WriteAllBytes(project.Path, bytes);

            project.LastModified = DateTime.UtcNow;

            SaveProjectData();
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Exception while saving project: {project.Name}");
        }
    }

    public void LoadProject(Project project, bool destroyAll, bool useRelativeLightPositions = true, bool useRelativeWorldObjectPositions = true, SceneImportOptions importOptions = SceneImportOptions.Default)
    {
        var result = _sceneService.Deserialize(File.ReadAllBytes(project.Path));
        _sceneService.ImportScene(result, destroyAll, useRelativeLightPositions, useRelativeWorldObjectPositions, importOptions);

        CurrentProject = project;
    }

    public void DeleteProject(Project project)
    {
        if(File.Exists(project.Path))
        {
            File.Delete(project.Path);

            BrioProjects.Projects.Remove(project);

            if(CurrentProject is not null && CurrentProject.Equals(project))
                CurrentProject = null;

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

    private void OnGPoseStateChange(bool newState)
    {
        // Why is this here? To open the thing on GPose starts ?? TODO(ken)
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
