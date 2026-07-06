using Brio.Config;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.MCDF.Game.Services;
using Brio.Services;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Brio.UI.Theming;
using Brio.UI.Windows.Specialized;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

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
    private readonly MCDFService _mCDFService;
    private readonly EntitySectionWindow _entitySectionWindow;
    private readonly ProjectSystem _projectSystem;

    private float MaxHeight => (1050 * ImGuiHelpers.GlobalScale);

    public MainWindow(
        ConfigurationService configService,
        SettingsWindow settingsWindow,
        UpdateWindow infoWindow,
        LibraryWindow libraryWindow,
        EntityManager entityManager,
        SceneService sceneService,
        GPoseService gPoseService,
        ProjectWindow projectWindow,
        AutoSaveService autoSaveService,
        MCDFService mCDFService,
        EntitySectionWindow entitySectionWindow,
        ProjectSystem projectSystem
        )
        : base($" {Brio.Name} [{configService.Version}]###brio_main_window", ImGuiWindowFlags.AlwaysAutoResize)
        //: base($" {Brio.Name} [0.8.0.0]###brio_main_window", ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_main_namespace";

        _configurationService = configService;
        _settingsWindow = settingsWindow;
        _libraryWindow = libraryWindow;
        _infoWindow = infoWindow;
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _entitySelector = new(_entityManager, _gPoseService);
        _sceneService = sceneService;
        _projectWindow = projectWindow;
        _autoSaveService = autoSaveService;
        _mCDFService = mCDFService;
        _entitySectionWindow = entitySectionWindow;
        _projectSystem = projectSystem;

        RespectCloseHotkey = false;
        AllowBackgroundBlur = false;

        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(280, MaxHeight),
            MinimumSize = new Vector2(280, 200)
        };
    }

    public override void Draw()
    {
        ImBrio.BlurWindow(ImGuiWindowFlags.None);

        bool hasScrollbar = ImGui.GetScrollMaxY() > 0;
        if(hasScrollbar)
        {
            var style = ImGui.GetStyle();

            SizeConstraints = new WindowSizeConstraints
            {
                MaximumSize = new Vector2(280 + style.ScrollbarSize, MaxHeight),
                MinimumSize = new Vector2(280 + style.ScrollbarSize, 200)
            };
        }
        else if(hasScrollbar is false)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MaximumSize = new Vector2(280, MaxHeight),
                MinimumSize = new Vector2(280, 200)
            };
        }

        //

        DrawHeaderButtons();

        if(_gPoseService.IsGPosing == false)
        {
            using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoRed))
                ImGui.Text("Open GPose to use Brio!");
        }

        var rootEntity = _entityManager.RootEntity;
        if(rootEntity is null)
            return;

        try
        {
            DrawEntitySelector(rootEntity);

            var selected = _entityManager.SelectedEntity;
            var isUndocked = _entitySectionWindow.IsOpen;

            var pos = ImGui.GetCursorPos();
          
            if(ImBrio.FontIconButtonRight("undock_entity_section", isUndocked ? FontAwesomeIcon.Compress : FontAwesomeIcon.WindowRestore, 1,
                tooltip: isUndocked ? "Redock Entity Widgets" : "Undock Entity Widgets into it's own Window"))
                _entitySectionWindow.IsOpen = !_entitySectionWindow.IsOpen;
       
            ImGui.SetCursorPos(pos);

            using(ImRaii.Disabled(_gPoseService.IsGPosing == false || selected?.IsLoading == true))
                if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "Spawn New..."))
                {
                    SpawnMenu.OpenUnifiedSpawnMenu();
                }

            ImBrio.VerticalSeparator(24, 1);

            EntityHelpers.DrawEntitySection(selected, isUndocked);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Failed to draw entity section: [ {_entityManager?.SelectedEntity?.FriendlyName ?? "Unknown"} ] ");
        }
    }

    private float? _entitySelectorHeight;
    private bool _isDraggingEntitySelector = false;
    public void DrawEntitySelector(Entity rootEntity)
    {
        var containerHeight = _entitySelectorHeight ?? (ImGui.GetTextLineHeight() * 18f);

        using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8f))
        using(var container = ImRaii.Child("###entity_hierarchy_container", new Vector2(-1, containerHeight), true))
        {
            if(container.Success)
            {
                _entitySelector.Draw(rootEntity, _entityManager.DebugEntity);

                if(_entityManager.SelectedEntities.Count > 1)
                {
                    using var color = ImRaii.PushColor(ImGuiCol.Text, ThemeManager.CurrentTheme.Accent.AccentColor);
                    ImGui.Text($"{_entityManager.SelectedEntities.Count} selected");
                }
            }
        }

        // Dragging of the bottom edge of the EntitySelector
        var childMax = ImGui.GetItemRectMax();
        var childX = ImGui.GetContentRegionAvail().X;
        var dragZoneMin = new Vector2(childMax.X - childX, childMax.Y - 4);
        var dragZoneSize = new Vector2(childX, 8);

        // Invisible button to capture input
        ImGui.SetCursorScreenPos(dragZoneMin);
        ImGui.InvisibleButton("###entity_hierarchy_resize_handler", dragZoneSize);
        bool isHovered = ImGui.IsItemHovered();

        if(isHovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);

            if(isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _isDraggingEntitySelector = true;
            }
        }

        if(_isDraggingEntitySelector)
        {
            if(ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                containerHeight += ImGui.GetIO().MouseDelta.Y;
                containerHeight = Math.Clamp(containerHeight, ImGui.GetTextLineHeight() * 5f, ImGui.GetTextLineHeight() * 35f);

                _entitySelectorHeight = containerHeight;

                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
            }
            else
            {
                _isDraggingEntitySelector = false;
            }
        }
    }

    private void DrawHeaderButtons()
    {
        float buttonWidths = 25 ;
        float line1FinalWidth = ImBrio.GetRemainingWidth() - (((buttonWidths * 2) * ImGuiHelpers.GlobalScale) + (ImGui.GetStyle().ItemSpacing.X * 2) + ImGui.GetStyle().WindowBorderSize);

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

            if(ImBrio.Button("Project", FontAwesomeIcon.FileAlt, new Vector2(line1Width, 0), centerTest: true))
                ImGui.OpenPopup("DrawProjectPopup");

            ImGui.SameLine();
            if(ImBrio.Button("Library", FontAwesomeIcon.BookBookmark, new Vector2(line1Width, 0), centerTest: true))
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

        using(ImRaii.Disabled(_mCDFService.IsApplyingMCDF))
            FileUIHelpers.DrawProjectPopup(_sceneService, _entityManager, _projectWindow, _autoSaveService);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
