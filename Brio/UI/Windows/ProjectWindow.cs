using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.MCDF.Game.Services;
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

    int selected = 0;
    static Project? selectedItem;
    private const float InfoPaneWidth = 200;

    public ProjectWindow(ProjectSystem projectSystem, MCDFService mCDFService, GPoseService gPoseService) : base($"Project Window BETA###brio_project_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Namespace = "brio_project_namespace";

        _projectSystem = projectSystem;
        _gPoseService = gPoseService;
        _mCDFService = mCDFService;

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = new(600, 400),
            MaximumSize = new(785, 435)
        };
        this.SizeConstraints = constraints;

        _gPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }

    string currentActorName = string.Empty;
    string currentActorDis = string.Empty;
    public override void Draw()
    {
        using(ImRaii.PushColor(ImGuiCol.Text, UIConstants.GizmoMagenta))
            ImGui.Text("Project System is in Beta. NOTE: Projects might not be compatible from version to version!");

        using(ImRaii.Disabled(_mCDFService.IsApplyingMCDF || _mCDFService.IsSavingMCDF))
        {
            ImBrio.ToggleButtonStrip("library_filters_selector", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ["Load", "Save"]);
            //var firstPos = ImGui.GetCursorPos();

            if(selected == 0 || _projectSystem.IsLoading)
            {
                DrawLoad();

            }
            else
            {
                var windowSize = ImGui.GetWindowSize();

                var lastPos = ImGui.GetCursorPos();

                //ImGui.SetCursorPos(firstPos);

                using(var child = ImRaii.Child("###new_pane", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetRemainingHeight()), false))
                {
                    if(child.Success)
                    {
                        ImGui.Text($"Save New Project");

                        ImGui.InputText("Project Name###brio_popup_name", ref currentActorName, 35);
                        ImGui.InputText("Project Description###brio_popup_dis", ref currentActorDis, 35);

                        using(ImRaii.Disabled(string.IsNullOrEmpty(currentActorName)))
                            if(ImBrio.Button("Save Project", FontAwesomeIcon.Save, new(135, 0), centerTest: true, hoverText: "Save Project"))
                            {
                                _projectSystem.NewProject(currentActorName, currentActorDis);

                                selected = 0;

                                currentActorName = string.Empty;
                                currentActorDis = string.Empty;
                            }
                    }
                }
            }
        }

        if(_mCDFService.IsApplyingMCDF || _mCDFService.IsSavingMCDF)
        {
            EntityHelpers.DrawSpinner();
        }
    }

    bool destroyAll = false;
    bool doDelete = false;
    public void DrawLoad()
    {
        var windowSize = ImGui.GetWindowSize();
        using(var child = ImRaii.Child("###left_pane", new Vector2(windowSize.X - InfoPaneWidth, -1), false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if(child.Success == false)
                return;

            float entriesPaneHeight = ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight() - ImGui.GetStyle().ItemSpacing.Y;
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
                if(ImBrio.Button("Load", FontAwesomeIcon.FileImport, new(120, 0), centerTest: true, hoverText: "Load Project"))
                {
                    _projectSystem.LoadProject(selectedItem!, destroyAll);
                }

                ImGui.SameLine();

                if(ImGui.Checkbox("Override Current Scene", ref destroyAll))
                {

                }
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
                ImGui.Checkbox("###doDelete", ref doDelete);
                ImGui.SameLine();
                using(ImRaii.Disabled(doDelete == false))
                    if(ImBrio.Button("Delete", FontAwesomeIcon.Trash, new(120, 0), centerTest: true, hoverText: "Delete Project"))
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
