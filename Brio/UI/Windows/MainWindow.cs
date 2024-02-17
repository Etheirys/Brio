﻿using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Input;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Windows;

internal class MainWindow : Window
{
    private readonly SettingsWindow _settingsWindow;
    private readonly InfoWindow _infoWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly EntityManager _entityManager;
    private readonly ConfigurationService _configurationService;

    private readonly EntityHierarchyView _entitySelector;

    public MainWindow(ConfigurationService configService, SettingsWindow settingsWindow, InfoWindow infoWindow, EntityManager entityManager,  LibraryWindow libraryWindow, InputService input) : base($"{Brio.Name} Scene Manager [{configService.Version}]###brio_main_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_main_namespace";

        _configurationService = configService;
        _settingsWindow = settingsWindow;
        _libraryWindow = libraryWindow;
        _infoWindow = infoWindow;
        _entityManager = entityManager;
        _entitySelector = new(_entityManager);

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(270, 5000),
            MinimumSize = new Vector2(270, 200)
        };

        input.AddListener(KeyBindEvents.Interface_ToggleBrioWindow, this.OnMainWindowToggle);
        input.AddListener(KeyBindEvents.Interface_ToggleBindPromptWindow, this.OnPromptWindowToggle);
    }

    public override void Draw()
    {
        DrawHeaderButtons();

        var rootEntity = _entityManager.RootEntity;

        if(rootEntity == null)
            return;

        using(var container = ImRaii.Child("###entity_hierarchy_container", new Vector2(-1, ImGui.GetTextLineHeight() * 15f), true))
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
        var initialPos = ImGui.GetCursorPos();
        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);

        ImGui.SetCursorPosY(0);
        if(ImBrio.FontIconButtonRight("settings_toggle", FontAwesomeIcon.Cog, 2.3f, "Settings", bordered: false))
            _settingsWindow.Toggle();

        ImGui.SetCursorPosY(0);
        if(ImBrio.FontIconButtonRight("info_toggle", FontAwesomeIcon.InfoCircle, 3.3f, "Info", bordered: false))
            _infoWindow.Toggle();

        ImGui.SetCursorPosY(0);
        if(ImBrio.FontIconButtonRight("library_toggle", FontAwesomeIcon.Book, 4.3f, "Library", bordered: false))
            _libraryWindow.Toggle();

        ImGui.PopClipRect();
        ImGui.SetCursorPos(initialPos);
    }
}
