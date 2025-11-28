using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.Scene;
using Brio.MCDF.Game.Services;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using Brio.UI.Controls.Editors;

namespace Brio.UI.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly SettingsWindow _settingsWindow;
    private readonly UpdateWindow _infoWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly ConfigurationService _configurationService;
    private readonly EntityManager _entityManager;
    private readonly EntityHierarchyView _entitySelector;
    private readonly SceneService _sceneService;
    private readonly ProjectWindow _projectWindow;
    private readonly GPoseService _gPoseService;
    private readonly AutoSaveService _autoSaveService;
    private readonly HistoryService _groupedUndoService;
    private readonly MCDFService _mCDFService;
    private readonly Sequencer _sequencer;
    
    public MainWindow(
        ConfigurationService configService,
        SettingsWindow settingsWindow,
        UpdateWindow infoWindow,
        LibraryWindow libraryWindow,
        EntityManager entityManager,
        HistoryService groupedUndoService,
        SceneService sceneService,
        GPoseService gPoseService,
        ProjectWindow projectWindow,
        AutoSaveService autoSaveService,
        MCDFService mCDFService,
        Sequencer sequencer
        )
        : base($" {Brio.Name} [{configService.Version}]###brio_main_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_main_namespace";

        _configurationService = configService;
        _settingsWindow = settingsWindow;
        _libraryWindow = libraryWindow;
        _infoWindow = infoWindow;
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _groupedUndoService = groupedUndoService;
        _entitySelector = new(_entityManager, _gPoseService, _groupedUndoService);
        _sceneService = sceneService;
        _projectWindow = projectWindow;
        _autoSaveService = autoSaveService;
        _mCDFService = mCDFService;
        _sequencer = sequencer;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(280, 1200),
            MinimumSize = new Vector2(280, 200)
        };
    }

    public override void Draw()
    {
        DrawHeaderButtons();

        if(_gPoseService.IsGPosing == false)
        {
            using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
                ImGui.Text("Open GPose to use Brio!");
        }

        var rootEntity = _entityManager.RootEntity;

        if(rootEntity is null)
            return;

        using(var container = ImRaii.Child("###entity_hierarchy_container", new Vector2(-1, ImGui.GetTextLineHeight() * 18f), true))
        {
            if(container.Success)
            {
                _entitySelector.Draw(rootEntity);

                if(_entityManager.SelectedEntityIds.Count > 1)
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, ThemeManager.CurrentTheme.Accent.AccentColor);
                    ImGui.Text($"{_entityManager.SelectedEntityIds.Count} selected");
                }
            }
        }

        try
        {
            EntityHelpers.DrawEntitySection(_entityManager.SelectedEntity);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Failed to draw entity section: [ {_entityManager?.SelectedEntity?.FriendlyName ?? "Unknown"} ] ");
        }
    }

    private void DrawHeaderButtons()
    {
        float buttonWidths = 25 * ImGuiHelpers.GlobalScale;
        float line1FinalWidth = ImBrio.GetRemainingWidth() - ((buttonWidths * 1) + (ImGui.GetStyle().ItemSpacing.X * 2) + ImGui.GetStyle().WindowBorderSize);

        float line1Width = (line1FinalWidth / 2) - 3;

        using(ImRaii.Disabled(_gPoseService.IsGPosing == false))
        {
            // This fixes a bug with text scaling
            {
                Vector2 startPos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new(-100, -100));
                ImBrio.Button("0000", FontAwesomeIcon.Bug, new Vector2(0, 0));
                ImGui.SetCursorPos(startPos);
            }

            if(ImBrio.Button("Project", FontAwesomeIcon.FolderOpen, new Vector2(line1Width, 0), centerTest: true))
                ImGui.OpenPopup("DrawProjectPopup");

            ImGui.SameLine();
            if(ImBrio.Button("Library", FontAwesomeIcon.Book, new Vector2(line1Width, 0), centerTest: true))
                _libraryWindow.Toggle();
        }
        ImGui.SameLine();
        if(ImBrio.FontIconButton(FontAwesomeIcon.Cog, new(buttonWidths, 0)))
            _settingsWindow.Toggle();

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Settings");

        using(ImRaii.Disabled(_gPoseService.IsGPosing == false))
        {
            if(ImBrio.Button("Timeline", FontAwesomeIcon.CodeCommit, new Vector2(line1Width, 0), centerTest: true))
                _sequencer.Toggle();
            ImGui.SameLine();
            if(ImBrio.Button("Placeholder", FontAwesomeIcon.Egg, new Vector2(line1Width, 0), centerTest: true))
                _libraryWindow.Toggle();
        }
        //
        ImGui.SameLine();
        if(ImBrio.FontIconButton(FontAwesomeIcon.InfoCircle, new(buttonWidths, 0)))
            _infoWindow.Toggle();

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Information & Changelog");

        using(ImRaii.Disabled(_mCDFService.IsApplyingMCDF))
            FileUIHelpers.DrawProjectPopup(_sceneService, _entityManager, _projectWindow, _autoSaveService);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
