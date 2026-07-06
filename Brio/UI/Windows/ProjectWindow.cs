using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.MCDF.Game.Services;
using Brio.Services.Models;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace Brio.UI.Windows;

public class ProjectWindow : Window, IDisposable
{
    private readonly ProjectSystem _projectSystem;
    private readonly GPoseService _gPoseService;
    private readonly MCDFService _mCDFService;

    static Project? selectedItem;
    private const float InfoPaneWidth = 175;

    public ProjectWindow(ProjectSystem projectSystem, MCDFService mCDFService, GPoseService gPoseService) : base($"{Brio.Name} LOAD PROJECT###brio_project_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Namespace = "brio_project_namespace";

        _projectSystem = projectSystem;
        _gPoseService = gPoseService;
        _mCDFService = mCDFService;

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = new(450, 350),
            MaximumSize = new(805, 500)
        };
        this.SizeConstraints = constraints;
        this.AllowBackgroundBlur = false;

        _gPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }

    public override void PreDraw()
    {
        this.Size = new Vector2(380, 500);
        ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X - Size!.Value.X) / 2, (ImGui.GetIO().DisplaySize.Y - Size!.Value.Y) / 2), ImGuiCond.Appearing);
    }

    public override void Draw()
    {
        ImBrio.BlurWindow();

        using(ImRaii.Disabled(_mCDFService.IsApplyingMCDF || _mCDFService.IsSavingMCDF))
        {
            DrawLoad();
        }

        if(_mCDFService.IsApplyingMCDF || _mCDFService.IsSavingMCDF)
        {
            EntityHelpers.DrawSpinner();
        }
    }

    bool destroyAll = false;
    bool useRelativeLightPositions = true;
    bool useRelativeWorldObjectPositions = true;
    SceneImportOptions importOptions = SceneImportOptions.All;
    public void DrawLoad()
    {
        var windowSize = ImGui.GetWindowSize();
        using(var child = ImRaii.Child("###left_pane", new Vector2(windowSize.X - InfoPaneWidth, -1), false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if(child.Success == false)
                return;

            float entriesPaneHeight = ImBrio.GetRemainingHeight() - (ImBrio.GetLineHeight() * 1) - (ImGui.GetStyle().ItemSpacing.Y * 1);
            float entriesPaneWidth = ImBrio.GetRemainingWidth();
            using(var entriesChild = ImRaii.Child("###entries_pane", new Vector2(entriesPaneWidth, entriesPaneHeight), true))
            {
                if(entriesChild.Success == false)
                    return;

                int x = 0;
                foreach(var item in _projectSystem.BrioProjects.Projects)
                {
                    bool selected = false;

                    if(selectedItem is not null && selectedItem.Equals(item))
                        selected = true;

                    x++;
                    var selected2 = DrawSourceItem(x, item, selected);

                    if(selected2)
                    {
                        selectedItem = item;
                    }
                }
            }

            using(ImRaii.Disabled(selectedItem is null))
            {
                if(ImBrio.Button("Load", FontAwesomeIcon.FileImport, new(120, 0), centerTest: true, tooltip: "Load Project"))
                {
                    _projectSystem.LoadProject(selectedItem!, destroyAll, useRelativeLightPositions, useRelativeWorldObjectPositions, importOptions);
                }

                ImGui.SameLine();
                FileUIHelpers.DrawImportSettingsPopup(ref importOptions, ref destroyAll, ref useRelativeLightPositions, ref useRelativeWorldObjectPositions);
            }
        }

        ImGui.SameLine();

        using(var child = ImRaii.Child("###right_pane", new Vector2(ImBrio.GetRemainingWidth(), -1), false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if(child.Success == false)
                return;

            float paneHeight = ImBrio.GetRemainingHeight() - (ImBrio.GetLineHeight() + ImGui.GetStyle().ItemSpacing.Y);

            using(var child2 = ImRaii.Child("###library_info_pane", new Vector2(ImBrio.GetRemainingWidth(), paneHeight), true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(child2.Success == false)
                    return;

                if(selectedItem is not null)
                {
                    ImGui.Text(selectedItem.Name);

                    if(string.IsNullOrEmpty(selectedItem.Description) == false)
                        ImGui.Text(selectedItem.Description);
                }
            }
            using(ImRaii.Disabled(selectedItem is null))
            {
                if(ImBrio.HoldButton("proj_delete", "Delete", FontAwesomeIcon.Trash, 1.1f, new(120, 0), centerTest: true, tooltip: "[HOLD]\nDelete Project"))
                {
                    _projectSystem.DeleteProject(selectedItem!);
                    selectedItem = null;
                }
            }
        }

    }

    private static bool DrawSourceItem(int id, Project project, bool isSelected)
    {
        float itemHeight = (ImBrio.GetLineHeight() * 3) + ImGui.GetStyle().ItemSpacing.Y;

        float itemTop = ImGui.GetCursorPosY();
        bool isSelectedItem = isSelected;
        ImGui.Selectable($"###settings_source_{id}_selectable", ref isSelectedItem, ImGuiSelectableFlags.None, new(ImBrio.GetRemainingWidth(), itemHeight));

        bool isHover = ImGui.IsItemHovered();

        ImGui.SetCursorPosY(itemTop + ImGui.GetStyle().FramePadding.Y);

        ImGui.Text(project.Name ?? "No Name Source");

        if(project.Created.HasValue)
            ImGui.Text($"Created: {project.Created.Value:g}");

        ImGui.Text(project.Description ?? "No Description");

        ImGui.SetCursorPosY(itemTop + itemHeight + ImGui.GetStyle().ItemSpacing.Y);

        ImGui.Separator();

        return isSelectedItem;
    }

    private void GPoseService_OnGPoseStateChange(bool newState)
    {
        if(newState == false)
        {
            this.IsOpen = false;
        }
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;

        GC.SuppressFinalize(this);
    }
}
