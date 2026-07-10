using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Services.Models;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Windows;

public class AutoSaveWindow : Window, IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly AutoSaveService _autoSaveService;
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;

    private const float InfoPaneWidth = 200;

    private IReadOnlyList<AutoSavePoseEntry> _autoSavePoses = [];
    private IReadOnlyList<AutoSaveEntry> _autoSaves = [];
    private AutoSavePoseEntry? _selectedPoseEntry;
    private AutoSaveEntry? _selectedEntry;
    private int _selectedActorIndex = 0;

    public AutoSaveWindow(ConfigurationService configurationService, GPoseService gPoseService, AutoSaveService autoSaveService, EntityManager entityManager) : base($"{Brio.Name} AUTO-SAVE###brio_autosaves_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize)
    {
        Namespace = "brio_autosaves_window";

        this.AllowClickthrough = false;
        this.AllowPinning = false;
        this.ForceMainWindow = true;

        _configurationService = configurationService;
        _gPoseService = gPoseService;
        _autoSaveService = autoSaveService;
        _entityManager = entityManager;

        Size = new Vector2(620, 400);

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = new(620, 400),
            MaximumSize = new(620, 400)
        };
        this.SizeConstraints = constraints;

        this.AllowBackgroundBlur = false;
        _gPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }

    public override void PreDraw()
    {
        ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X - Size!.Value.X) / 2, (ImGui.GetIO().DisplaySize.Y - Size!.Value.Y) / 2), ImGuiCond.Appearing);

        base.PreDraw();
    }

    public override void OnOpen()
    {
        _autoSaveService.IsEnabled = false;
        RefreshList();

        Brio.Log.Info("AutoSaveWindow opened, disabled auto-save service.");

        base.OnOpen();
    }

    public override void OnClose()
    {
        _autoSaveService.IsEnabled = true;

        Brio.Log.Info("AutoSaveWindow closed, re-enabled auto-save service.");

        base.OnClose();
    }

    private bool _destroyAll = true;
    private bool _useRelativeLightPositions = false;
    private bool _useRelativeWorldObjectPositions = false;
    private SceneImportOptions _importOptions = SceneImportOptions.All;
    public override void Draw()
    {
        ImBrio.BlurWindow();

        using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoMagenta))
        {
            ImGui.Text("ATTENTION: Brio will **NOT** make AutoSaves with this window open!");
        }

        var windowSize = ImGui.GetWindowSize();

        using(var leftChild = ImRaii.Child("###left_pane", new Vector2(windowSize.X - InfoPaneWidth, -1), false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if(leftChild.Success is false)
                return;

            float listHeight = ImBrio.GetRemainingHeight() - (ImBrio.GetLineHeight() * 1) - (ImGui.GetStyle().ItemSpacing.Y * 1);

            using(var listChild = ImRaii.Child("###list_pane", new Vector2(ImBrio.GetRemainingWidth(), listHeight), true))
            {
                if(listChild.Success)
                {
                    for(int i = 0; i < _autoSaves.Count; i++)
                    {
                        var entry = _autoSaves[i];
                        bool isSelected = _selectedEntry is not null && _selectedEntry.FolderPath == entry.FolderPath;

                        if(DrawAutoSaveEntry(i, entry, isSelected))
                        {
                            SelectEntry(entry);
                        }
                    }

                    if(_autoSaves.Count == 0)
                        ImGui.TextDisabled("No auto-saves found!");
                }
            }

            using(ImRaii.Disabled(_selectedEntry is null || !_selectedEntry.IsValid))
            {
                if(ImBrio.Button("Load", FontAwesomeIcon.FileImport, new(120, 0), centerTest: true, tooltip: "Load this auto-save"))
                {
                    _autoSaveService.LoadAutoSave(_selectedEntry!, _destroyAll, _useRelativeLightPositions, _useRelativeWorldObjectPositions, _importOptions);
                }

                ImGui.SameLine();
                FileUIHelpers.DrawImportSettingsPopup(ref _importOptions, ref _destroyAll, ref _useRelativeLightPositions, ref _useRelativeWorldObjectPositions);
            }
        }

        ImGui.SameLine();

        using var rightChild = ImRaii.Child("###right_pane", new Vector2(ImBrio.GetRemainingWidth(), -1), false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        if(rightChild.Success == false)
            return;

        float infoHeight = ImBrio.GetRemainingHeight() - (ImBrio.GetLineHeight() + ImGui.GetStyle().ItemSpacing.Y);
        using var infoChild = ImRaii.Child("###info_pane", new Vector2(ImBrio.GetRemainingWidth(), infoHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if(infoChild.Success && _selectedEntry is not null)
        {
            ImGui.TextUnformatted(_selectedEntry.DisplayName);

            if(_autoSavePoses.Count > 0)
            {
                ImGui.Separator();
                ImGui.TextUnformatted("Poses:");

                float applyAreaHeight = ((ImBrio.GetLineHeight() + ImGui.GetStyle().ItemSpacing.Y) * 2) + ImGui.GetStyle().ItemSpacing.Y;
                float posesListHeight = Math.Max(20, ImBrio.GetRemainingHeight() - applyAreaHeight);

                using(var posesListChild = ImRaii.Child("###poses_list", new Vector2(-1, posesListHeight), false))
                {
                    if(posesListChild.Success)
                    {
                        for(int i = 0; i < _autoSavePoses.Count; i++)
                        {
                            var pose = _autoSavePoses[i];
                            bool isPoseSelected = _selectedPoseEntry?.FilePath == pose.FilePath;
                            if(ImGui.Selectable($"{pose.ActorName}###pose_{i}", isPoseSelected))
                                _selectedPoseEntry = pose;
                        }
                    }
                }

                ImGui.Separator();

                var actors = _entityManager.TryGetAllActors().ToList();
                _selectedActorIndex = Math.Clamp(_selectedActorIndex, 0, actors.Count);
                string comboPreview = _selectedActorIndex == 0 ? "Selected Actor" : actors[_selectedActorIndex - 1].FriendlyName;

                ImGui.SetNextItemWidth(ImBrio.GetRemainingWidth());
                if(ImGui.BeginCombo("###actor_combo", comboPreview))
                {
                    if(ImGui.Selectable("Selected Actor###actor_sel", _selectedActorIndex == 0))
                        _selectedActorIndex = 0;

                    for(int i = 0; i < actors.Count; i++)
                    {
                        if(ImGui.Selectable($"{actors[i].FriendlyName}###actor_{i}", _selectedActorIndex == i + 1))
                            _selectedActorIndex = i + 1;
                    }
                    ImGui.EndCombo();
                }

                using(ImRaii.Disabled(_selectedPoseEntry is null))
                {
                    if(ImBrio.Button("Apply Pose", FontAwesomeIcon.Running, new(ImBrio.GetRemainingWidth(), 0), centerTest: true, tooltip: "Apply selected pose to actor"))
                        ApplySelectedPose(actors);
                }
            }
            else
            {
                ImGui.Text("Poses: None");
            }
        }
    }

    private void ApplySelectedPose(List<ActorEntity> actors)
    {
        if(_selectedPoseEntry is null)
            return;

        if(_selectedActorIndex == 0)
        {
            if(_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var cap))
                _autoSaveService.LoadPoseOnActor(_selectedPoseEntry, cap);
            else
                Brio.NotifyError("No actor with posing capability is selected.");
        }
        else
        {
            var actor = actors[_selectedActorIndex - 1];
            if(actor.TryGetCapability<PosingCapability>(out var cap))
                _autoSaveService.LoadPoseOnActor(_selectedPoseEntry, cap);
            else
                Brio.NotifyError($"'{actor.FriendlyName}' cannot be posed.");
        }
    }

    private void SelectEntry(AutoSaveEntry? entry)
    {
        _selectedEntry = entry;
        _selectedPoseEntry = null;
        _selectedActorIndex = 0;
        _autoSavePoses = entry?.HasPoses == true ? _autoSaveService.GetAllAutoSavePoses(entry) : [];
    }

    private static bool DrawAutoSaveEntry(int id, AutoSaveEntry entry, bool isSelected)
    {
        float pad = ImGui.GetStyle().FramePadding.Y;
        float itemHeight = ImBrio.GetLineHeight() + (pad * 2);

        float selectablePOS = ImGui.GetCursorPosY();
        bool clicked = ImGui.Selectable($"###selectable_{id}", ref isSelected, ImGuiSelectableFlags.None, new(ImBrio.GetRemainingWidth(), itemHeight));
        float selectableBottomPOS = ImGui.GetCursorPosY();

        float textY = selectablePOS + pad;

        ImGui.SetCursorPosY(textY);
        ImGui.TextUnformatted(entry.DisplayName);

        string metaData = entry.PoseCount > 0
            ? $"{entry.PoseCount} pose{(entry.PoseCount == 1 ? "" : "s")}  -  {entry.SavedAtDelta}"
            : entry.SavedAtDelta;
        float metaWidth = ImGui.CalcTextSize(metaData).X;

        ImGui.SameLine(ImBrio.GetRemainingWidth() - metaWidth - ImGui.GetStyle().FramePadding.X);
        ImGui.SetCursorPosY(textY);
        ImGui.TextDisabled(metaData);

        ImGui.SetCursorPosY(selectableBottomPOS);

        return clicked;
    }

    private void RefreshList()
    {
        _autoSaves = _autoSaveService.GetAutoSaves();

        var selected = _selectedEntry is not null
            ? _autoSaves.FirstOrDefault(e => e.FolderPath == _selectedEntry.FolderPath)
            : null;

        SelectEntry(selected);
    }

    private void GPoseService_OnGPoseStateChange(bool newState)
    {
        if(newState == false)
        {
            IsOpen = false;
        }
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;

        GC.SuppressFinalize(this);
    }
}
