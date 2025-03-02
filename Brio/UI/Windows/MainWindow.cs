﻿using Brio.Config;
using Brio.Entities;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.Scene;
using Brio.Input;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly SettingsWindow _settingsWindow;
    private readonly InfoWindow _infoWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly ConfigurationService _configurationService;
    private readonly InputService _inputService;
    private readonly EntityManager _entityManager;
    private readonly EntityHierarchyView _entitySelector;
    private readonly SceneService _sceneService;
    private readonly ProjectWindow _projectWindow;
    private readonly GPoseService _gPoseService;
    private readonly AutoSaveService _autoSaveService;

    public MainWindow(
        ConfigurationService configService,
        SettingsWindow settingsWindow,
        InfoWindow infoWindow,
        LibraryWindow libraryWindow,
        EntityManager entityManager,
        InputService input,
        SceneService sceneService,
        GPoseService gPoseService,
        ProjectWindow projectWindow,
        AutoSaveService autoSaveService
        )
        : base($"{Brio.Name} Scene Manager [{configService.Version}]###brio_main_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_main_namespace";

        _configurationService = configService;
        _settingsWindow = settingsWindow;
        _libraryWindow = libraryWindow;
        _infoWindow = infoWindow;
        _inputService = input;
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _entitySelector = new(_entityManager, _gPoseService);
        _sceneService = sceneService;
        _projectWindow = projectWindow;
        _autoSaveService = autoSaveService;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(270, 1030),
            MinimumSize = new Vector2(270, 200)
        };

        input.AddListener(KeyBindEvents.Interface_ToggleBrioWindow, this.OnMainWindowToggle);
        input.AddListener(KeyBindEvents.Interface_ToggleBindPromptWindow, this.OnPromptWindowToggle);
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
            }
        }

        EntityHelpers.DrawEntitySection(_entityManager.SelectedEntity);
    }

    private void OnMainWindowToggle()
    {
        this.IsOpen = !this.IsOpen;
    }

    private void OnPromptWindowToggle()
    {
        _configurationService.Configuration.Input.ShowPromptsInGPose = !_configurationService.Configuration.Input.ShowPromptsInGPose;

        _configurationService.ApplyChange();
    }

    private void DrawHeaderButtons()
    {
        float buttonWidths = 25;
        float line1FinalWidth = ImBrio.GetRemainingWidth() - ((buttonWidths * 2) + (ImGui.GetStyle().ItemSpacing.X * 2) + ImGui.GetStyle().WindowBorderSize);

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

            if(ImBrio.Button("Project", FontAwesomeIcon.FolderOpen, new Vector2(line1Width, 0)))
                ImGui.OpenPopup("DrawProjectPopup");

            ImGui.SameLine();
            if(ImBrio.Button("Library", FontAwesomeIcon.Book, new Vector2(line1Width, 0)))
                _libraryWindow.Toggle();
        }

        ImGui.SameLine();
        if(ImBrio.FontIconButton(FontAwesomeIcon.InfoCircle, new(buttonWidths, 0)))
            _infoWindow.Toggle();

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Information & Changelog");

        ImGui.SameLine();
        if(ImBrio.FontIconButton(FontAwesomeIcon.Cog, new(buttonWidths, 0)))
            _settingsWindow.Toggle();

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Settings");

        //
        FileUIHelpers.DrawProjectPopup(_sceneService, _entityManager, _projectWindow, _autoSaveService);
    }

    public void Dispose()
    {
        _inputService.RemoveListener(KeyBindEvents.Interface_ToggleBrioWindow, this.OnMainWindowToggle);
        _inputService.RemoveListener(KeyBindEvents.Interface_ToggleBindPromptWindow, this.OnPromptWindowToggle);
    }
}
