using Brio.Game.Core;
using Brio.Services;
using Brio.Services.Models;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Modals;

public class ExportSceneModal : Modal
{
    private readonly SceneService _sceneService;

    private string _author = string.Empty;
    private string _description = string.Empty;

    public ExportSceneModal(SceneService sceneService) : base("Export Scene###export_scene_modal", new(420, 150), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration)
    {
        _sceneService = sceneService;
    }

    public override void OnClose()
    {
        _author = string.Empty;
        _description = string.Empty;
    }

    public override void DrawContent()
    {
        ImBrio.SeparatorText($" Export Scene ");

        ImBrio.SeparatorText($" Author ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###export_author", ref _author, 100);

        ImBrio.SeparatorText($" Description ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###export_description", ref _description, 250);

        float buttonW = (MinimumSize.X / 2) - 12;

        if(ImBrio.Button("Export", FontAwesomeIcon.FileExport, new(buttonW, 0), centerTest: true, tooltip: "Export Scene to a file"))
        {
            FileUIHelpers.ShowExportSceneModal(_sceneService, string.IsNullOrEmpty(_author) ? null : _author, string.IsNullOrEmpty(_description) ? null : _description);
            Close();
        }

        ImGui.SameLine();

        if(ImBrio.Button("Cancel", FontAwesomeIcon.Times, new(buttonW, 0), centerTest: true))
            Close();
    }
}

public class SaveProjectModal : Modal
{
    private readonly ProjectSystem _projectSystem;

    private string _name = string.Empty;
    private string _description = string.Empty;

    public SaveProjectModal(ProjectSystem projectSystem) : base("Save New Project###save_project_modal", new(420, 150), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration)
    {
        _projectSystem = projectSystem;
    }

    public override void OnClose()
    {
        _name = string.Empty;
        _description = string.Empty;
    }

    public override void DrawContent()
    {
        ImBrio.SeparatorText($" Save New Project ");

        ImBrio.SeparatorText($" Name ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###save_project_name", ref _name, 100);

        ImBrio.SeparatorText($" Description ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###save_project_description", ref _description, 250);

        float buttonW = (MinimumSize.X / 2) - 12;

        using(ImRaii.Disabled(string.IsNullOrEmpty(_name)))
        {
            if(ImBrio.Button("Save", FontAwesomeIcon.Save, new(buttonW, 0), centerTest: true, tooltip: "Save as a new Project"))
            {
                _projectSystem.NewProject(_name, string.IsNullOrEmpty(_description) ? null : _description);
                Close();
            }
        }

        ImGui.SameLine();

        if(ImBrio.Button("Cancel", FontAwesomeIcon.Times, new(buttonW, 0), centerTest: true))
            Close();
    }
}

public class ImportSceneModal : Modal
{
    private readonly SceneService _sceneService;

    private bool _destroyAll = false;
    private bool _useRelativeLightPositions = true;
    private bool _useRelativeWorldObjectPositions = true;

    private SceneImportOptions _importOptions = SceneImportOptions.Default;

    public ImportSceneModal(SceneService sceneService) : base("Import Scene###import_scene_modal", new(440, 150), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration)
    {
        _sceneService = sceneService;
    }

    public override void DrawContent()
    {
        ImBrio.SeparatorText($" Import Scene ");

        ImGui.SameLine();

        FileUIHelpers.DrawImportSettingsPopup(ref _importOptions, ref _destroyAll, ref _useRelativeLightPositions, ref _useRelativeWorldObjectPositions);

        float buttonW = (MinimumSize.X / 2) - 12;

        if(ImBrio.Button("Choose File & Load", FontAwesomeIcon.FileImport, new(buttonW, 0), centerTest: true, tooltip: "Choose a scene file and load it"))
        {
            FileUIHelpers.ShowImportSceneModal(_sceneService, _destroyAll, _useRelativeLightPositions, _useRelativeWorldObjectPositions, _importOptions);
            Close();
        }

        ImGui.SameLine();

        if(ImBrio.Button("Cancel", FontAwesomeIcon.Times, new(buttonW, 0), centerTest: true))
            Close();
    }
}
