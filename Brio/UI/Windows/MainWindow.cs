using Brio.Config;
using Brio.Entities;
using Brio.Game.Scene;
using Brio.Input;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Windows;

internal class MainWindow : Window, IDisposable
{
    private readonly SettingsWindow _settingsWindow;
    private readonly InfoWindow _infoWindow;
    private readonly LibraryWindow _libraryWindow;
    private readonly ConfigurationService _configurationService;
    private readonly InputService _inputService;
    private readonly EntityManager _entityManager;
    private readonly EntityHierarchyView _entitySelector;
    private readonly SceneService _sceneService;

    public MainWindow(
        ConfigurationService configService,
        SettingsWindow settingsWindow,
        InfoWindow infoWindow,
        LibraryWindow libraryWindow,
        EntityManager entityManager,
        InputService input,
        SceneService sceneService
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
        _entitySelector = new(_entityManager);
        _sceneService = sceneService;

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

        if(rootEntity is null)
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

    private const int Line1NumberOfButtons = 2;
    private const int Line2NumberOfButtons = 0;
    private void DrawHeaderButtons()
    {
        float buttonWidths = 25;
        float line1FinalWidth = ImBrio.GetRemainingWidth() - ((buttonWidths * Line1NumberOfButtons) + (ImGui.GetStyle().ItemSpacing.X * Line1NumberOfButtons) + ImGui.GetStyle().WindowBorderSize);
        float line2FinalWidth = ImBrio.GetRemainingWidth() - ((buttonWidths * Line2NumberOfButtons) + (ImGui.GetStyle().ItemSpacing.X * Line2NumberOfButtons) + ImGui.GetStyle().WindowBorderSize);

        float line1Width = (line1FinalWidth / 2) - 3;

        if(ImBrio.Button(" Project", FontAwesomeIcon.FolderOpen, new Vector2(line1Width, 0)))
        {
            ImGui.OpenPopup("DrawProjectPopup");
        }

        FileUIHelpers.DrawProjectPopup(_sceneService, _entityManager);

        ImGui.SameLine();
        if(ImBrio.Button("Library", FontAwesomeIcon.Book, new Vector2(line1Width, 0)))
            _libraryWindow.Toggle();

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Open the Library");

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
        // Line 2
    }

    public void Dispose()
    {
        _inputService.RemoveListener(KeyBindEvents.Interface_ToggleBrioWindow, this.OnMainWindowToggle);
        _inputService.RemoveListener(KeyBindEvents.Interface_ToggleBindPromptWindow, this.OnPromptWindowToggle);
    }
}
