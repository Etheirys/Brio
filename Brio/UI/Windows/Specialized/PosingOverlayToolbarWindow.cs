using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuizmoNET;
using System.Numerics;
using Brio.Capabilities.Posing;
using Brio.Entities;
using OneOf.Types;
using Brio.Game.Posing;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.Game.GPose;
using System;

namespace Brio.UI.Windows.Specialized;

internal class PosingOverlayToolbarWindow : Window
{
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly EntityManager _entityManager;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingService _posingService;

    private readonly BoneSearchControl _boneSearchControl = new();

    private bool _pushedStyle = false;

    private const string _boneFilterPopupName = "bone_filter_popup";

    public PosingOverlayToolbarWindow(PosingOverlayWindow overlayWindow, EntityManager entityManager, PosingTransformWindow overlayTransformWindow, PosingService posingService) : base($"Brio - Overlay###brio_posing_overlay_toolbar_window", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        Namespace = "brio_posing_overlay_toolbar_namespace";


        _overlayWindow = overlayWindow;
        _entityManager = entityManager;
        _overlayTransformWindow = overlayTransformWindow;
        _posingService = posingService;
        ShowCloseButton = false;
    }

    public override void PreOpenCheck()
    {
        IsOpen = _overlayWindow.IsOpen;
        base.PreOpenCheck();
    }

    public override bool DrawConditions()
    {
        if (!_overlayWindow.IsOpen)
            return false;

        if (!_entityManager.SelectedHasCapability<PosingCapability>())
            return false;

        return base.DrawConditions();
    }

    public override void PreDraw()
    {
        base.PreDraw();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        _pushedStyle = true;
    }

    public override void Draw()
    {
        if(_pushedStyle)
        {
            _pushedStyle = false;
            ImGui.PopStyleVar(2);
        }

        if (!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
            return;

        DrawHeaderButtons();
        DrawButtons(posing);
        DrawBoneFilterPopup();
    }

    public override void PostDraw()
    {
        if(_pushedStyle)
        {
            _pushedStyle = false;
            ImGui.PopStyleVar(2);
        }

        base.PostDraw();
    }

    private void DrawButtons(PosingCapability posing)
    {

        float buttonSize = ImGui.GetTextLineHeight() * 3.2f;

        ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{(_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe.ToIconString() : FontAwesomeIcon.Atom.ToIconString())}###select_mode", new Vector2(buttonSize)))
                _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(_posingService.CoordinateMode == PosingCoordinateMode.Local ? "Switch to Local" : "Switch to World");

        ImGui.SameLine();

        using (ImRaii.PushColor(ImGuiCol.Text, _overlayTransformWindow.IsOpen ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button($"{FontAwesomeIcon.LocationCrosshairs.ToIconString()}###toggle_transforms_window", new Vector2(buttonSize)))
                    _overlayTransformWindow.IsOpen = !_overlayTransformWindow.IsOpen;
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Toggle Transform Window");

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{FontAwesomeIcon.WindowClose.ToIconString()}###close_overlay", new Vector2(buttonSize)))
                _overlayWindow.IsOpen = false;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Close Overlay");

        ImGui.Separator();

        using (ImRaii.PushColor(ImGuiCol.Text, _overlayWindow.Operation == OPERATION.TRANSLATE ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}###select_position", new Vector2(buttonSize)))
                    _overlayWindow.Operation = OPERATION.TRANSLATE;
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Position");

        ImGui.SameLine();


        using (ImRaii.PushColor(ImGuiCol.Text, _overlayWindow.Operation == OPERATION.ROTATE ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button($"{FontAwesomeIcon.ArrowsSpin.ToIconString()}###select_rotate", new Vector2(buttonSize)))
                    _overlayWindow.Operation = OPERATION.ROTATE;
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Rotation");

        ImGui.SameLine();


        using (ImRaii.PushColor(ImGuiCol.Text, _overlayWindow.Operation == OPERATION.SCALE ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.Button($"{FontAwesomeIcon.ExpandAlt.ToIconString()}###select_scale", new Vector2(buttonSize)))
                    _overlayWindow.Operation = OPERATION.SCALE;
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Scale");

        ImGui.Separator();



        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{FontAwesomeIcon.Bone.ToIconString()}###toggle_filter_window", new Vector2(buttonSize)))
                ImGui.OpenPopup(_boneFilterPopupName);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Bone Filter");

        ImGui.SameLine();

        PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(buttonSize));

        ImGui.SameLine();

        var parentBone = posing.Selected.Match(
          boneSelect => posing.SkeletonPosing.GetBone(boneSelect)?.GetFirstVisibleParent(),
          _ => null,
          _ => null
       );

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using (ImRaii.Disabled(parentBone == null))
            {
                if (ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconString()}###select_parent", new Vector2(buttonSize)))
                    posing.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Select Parent");


        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}###bone_search", new Vector2(buttonSize)))
                ImGui.OpenPopup("overlay_bone_search_popup");
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Bone Search");

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using (ImRaii.Disabled(posing.Selected.Value is None))
            {
                if (ImGui.Button($"{FontAwesomeIcon.MinusSquare.ToIconString()}###clear_selected", new Vector2(buttonSize)))
                    posing.ClearSelection();
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Clear Selection");

        ImGui.Separator();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using (ImRaii.Disabled(!posing.HasUndoStack))
            {
                if (ImGui.Button($"{FontAwesomeIcon.Backward.ToIconString()}###undo_pose", new Vector2(buttonSize)))
                    posing.Undo();
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Undo");

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using (ImRaii.Disabled(!posing.HasRedoStack))
            {
                if (ImGui.Button($"{FontAwesomeIcon.Forward.ToIconString()}###redo_pose", new Vector2(buttonSize)))
                    posing.Redo();
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Redo");

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using (ImRaii.Disabled(!posing.HasOverride))
            {
                if (ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}###reset_pose", new Vector2(buttonSize)))
                    posing.Reset();
            }
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Reset Pose");

        ImGui.Separator();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{FontAwesomeIcon.FileImport.ToIconString()}###import_pose", new Vector2(buttonSize)))
                FileUIHelpers.ShowImportPoseModal(posing);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Import Pose");

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{FontAwesomeIcon.FileExport.ToIconString()}###export_pose", new Vector2(buttonSize)))
                FileUIHelpers.ShowExportPoseModal(posing); ;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Export Pose");

        ImGui.SameLine();

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (ImGui.Button($"{FontAwesomeIcon.Cog.ToIconString()}###import_options", new Vector2(buttonSize)))
                ImGui.OpenPopup("import_options_popup_pose_tooblar");
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Import Options");

        ImGui.PopStyleColor();

        using (var popup = ImRaii.Popup("import_options_popup_pose_tooblar"))
        {
            if (popup.Success)
            {
                PosingEditorCommon.DrawImportOptionEditor(_posingService.ImporterOptions);
            }
        }

        using (var popup = ImRaii.Popup("overlay_bone_search_popup"))
        {
            if (popup.Success)
            {
                _boneSearchControl.Draw("overlay_bone_search", posing);
            }
        }
    }

    private void DrawBoneFilterPopup()
    {
        using (var popup = ImRaii.Popup(_boneFilterPopupName))
        {
            if (popup.Success)
            {
                PosingEditorCommon.DrawBoneFilterEditor(_posingService.OverlayFilter);
            }
        }
    }

    public override void OnClose()
    {
        _overlayTransformWindow.IsOpen = false;
        base.OnClose();
    }

    private void DrawHeaderButtons()
    {
        var initialPos = ImGui.GetCursorPos();
        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);

        const string helpText = "Alt - Hide Overlay\nShift - Disable Gizmo\nCtrl - Disable Skeleton";

        ImGui.SetCursorPosY(0);
        ImBrio.FontIconButtonRight("overlay_help", FontAwesomeIcon.QuestionCircle, 1f, helpText, bordered: false);

        ImGui.PopClipRect();
        ImGui.SetCursorPos(initialPos);
    }
}
