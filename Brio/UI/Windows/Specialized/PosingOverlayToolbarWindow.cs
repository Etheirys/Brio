using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities;
using Brio.Game.Input;
using Brio.Game.Posing;
using Brio.Input;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class PosingOverlayToolbarWindow : Window
{
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly EntityManager _entityManager;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingService _posingService;
    private readonly ConfigurationService _configurationService;
    private readonly GameInputService _gameInputService;

    private readonly BoneSearchControl _boneSearchControl = new();

    private bool _pushedStyle = false;

    private const string _boneFilterPopupName = "bone_filter_popup";

    public PosingOverlayToolbarWindow(PosingOverlayWindow overlayWindow, GameInputService gameInputService, EntityManager entityManager, PosingTransformWindow overlayTransformWindow, PosingService posingService, ConfigurationService configurationService) : base($"Brio - Overlay###brio_posing_overlay_toolbar_window", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        Namespace = "brio_posing_overlay_toolbar_namespace";

        _overlayWindow = overlayWindow;
        _entityManager = entityManager;
        _overlayTransformWindow = overlayTransformWindow;
        _posingService = posingService;
        _configurationService = configurationService;
        _gameInputService = gameInputService;

        ShowCloseButton = false;
    }

    public override void PreOpenCheck()
    {
        IsOpen = _overlayWindow.IsOpen;

        _gameInputService.AllowEscape = true;

        if(UIManager.IsPosingGraphicalWindowOpen && _configurationService.Configuration.Posing.HideToolbarWhenAdvandedPosingOpen)
        {
            IsOpen = false;
        }

        base.PreOpenCheck();
    }

    public override bool DrawConditions()
    {
        _gameInputService.AllowEscape = true;

        if(!_overlayWindow.IsOpen)
            return false;

        if(!_entityManager.SelectedHasCapability<PosingCapability>())
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

        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
            return;

        if(posing.Selected.Value is not null and BonePoseInfoId)
        {
            _gameInputService.AllowEscape = false;

            if(InputManagerService.ActionKeysPressed(InputAction.Posing_Esc)) 
            {
                posing.ClearSelection();
            }
        }
        else
        {
            _gameInputService.AllowEscape = true;
        }

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

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{(_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe.ToIconString() : FontAwesomeIcon.Atom.ToIconString())}###select_mode", new Vector2(buttonSize)))
                _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(_posingService.CoordinateMode == PosingCoordinateMode.Local ? "Switch to World" : "Switch to Local");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _overlayTransformWindow.IsOpen ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.LocationCrosshairs.ToIconString()}###toggle_transforms_window", new Vector2(buttonSize)))
                    _overlayTransformWindow.IsOpen = !_overlayTransformWindow.IsOpen;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Toggle Transform Window");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.WindowClose.ToIconString()}###close_overlay", new Vector2(buttonSize)))
                _overlayWindow.IsOpen = false;
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Close Overlay");

        ImGui.Separator();

        float buttonOperationSize = ImGui.GetTextLineHeight() * 2.4f;


        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Translate ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}###select_position", new Vector2(buttonOperationSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Translate))
                    _posingService.Operation = PosingOperation.Translate;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Position");

        ImGui.SameLine();


        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Rotate ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsSpin.ToIconString()}###select_rotate", new Vector2(buttonOperationSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Rotate))
                    _posingService.Operation = PosingOperation.Rotate;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Rotation");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Scale ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ExpandAlt.ToIconString()}###select_scale", new Vector2(buttonOperationSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Scale))
                    _posingService.Operation = PosingOperation.Scale;
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Scale");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Universal ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Cubes.ToIconString()}###select_universal", new Vector2(buttonOperationSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Universal))
                {
                    _posingService.Operation = PosingOperation.Universal;
                }
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Universal");

        ImGui.Separator();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Bone.ToIconString()}###toggle_filter_window", new Vector2(buttonSize)))
                ImGui.OpenPopup(_boneFilterPopupName);
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Bone Filter");

        ImGui.SameLine();

        PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(buttonSize));

        ImGui.SameLine();

        var bone = posing.Selected.Match(
          boneSelect => posing.SkeletonPosing.GetBone(boneSelect),
          _ => null,
          _ => null
       );

        var parentBone = bone?.Parent;

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(parentBone == null))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconString()}###select_parent", new Vector2(buttonSize)))
                    posing.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Select Parent");


        using(ImRaii.Disabled(!(bone?.EligibleForIK == true)))
        {
            if(ImGui.Button($"IK###bone_ik", new Vector2(buttonSize)))
                ImGui.OpenPopup("overlay_bone_ik");
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Inverse Kinematics");

        ImGui.SameLine();


        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}###bone_search", new Vector2(buttonSize)))
                ImGui.OpenPopup("overlay_bone_search_popup");
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Bone Search");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(posing.Selected.Value is None))
            {
                if(ImGui.Button($"{FontAwesomeIcon.MinusSquare.ToIconString()}###clear_selected", new Vector2(buttonSize)))
                    posing.ClearSelection();
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Clear Selection");

        ImGui.Separator();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.HasUndoStack))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Backward.ToIconString()}###undo_pose", new Vector2(buttonSize)))
                    posing.Undo();
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Undo");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.HasRedoStack))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Forward.ToIconString()}###redo_pose", new Vector2(buttonSize)))
                    posing.Redo();
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Redo");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.HasOverride))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}###reset_pose", new Vector2(buttonSize)))
                    posing.Reset(false, false);
            }
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Reset Pose");

        ImGui.Separator();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.FileImport.ToIconString()}###import_pose", new Vector2(buttonSize)))
                ImGui.OpenPopup("DrawImportPoseMenuPopup");
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Import Pose");

        FileUIHelpers.DrawImportPoseMenuPopup(posing, false);

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.FileExport.ToIconString()}###export_pose", new Vector2(buttonSize)))
                FileUIHelpers.ShowExportPoseModal(posing);
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Export Pose");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Cog.ToIconString()}###import_options", new Vector2(buttonSize)))
                ImGui.OpenPopup("import_options_popup_pose_tooblar");
        }
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Import Options");

        ImGui.PopStyleColor();

        using(var popup = ImRaii.Popup("import_options_popup_pose_tooblar"))
        {
            if(popup.Success)
            {
                PosingEditorCommon.DrawImportOptionEditor(_posingService.DefaultImporterOptions);
            }
        }

        using(var popup = ImRaii.Popup("overlay_bone_search_popup"))
        {
            if(popup.Success)
            {
                _boneSearchControl.Draw("overlay_bone_search", posing);
            }
        }

        using(var popup = ImRaii.Popup("overlay_bone_ik"))
        {
            if(popup.Success)
            {
                if(posing.Selected.Value is BonePoseInfoId id)
                {
                    var info = posing.SkeletonPosing.GetBonePose(id);
                    BoneIKEditor.Draw(info, posing);
                }
            }
        }
    }

    private void DrawBoneFilterPopup()
    {
        using(var popup = ImRaii.Popup(_boneFilterPopupName))
        {
            if(popup.Success)
            {
                PosingEditorCommon.DrawBoneFilterEditor(_posingService.OverlayFilter);
            }
        }
    }

    private static void DrawHeaderButtons()
    {
        var initialPos = ImGui.GetCursorPos();
        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);

        const string helpText = "Alt - Hide Overlay\nShift - Disable Gizmo\nCtrl - Disable Skeleton";

        ImGui.SetCursorPosY(0);
        ImBrio.FontIconButtonRight("overlay_help", FontAwesomeIcon.QuestionCircle, 2f, helpText, bordered: false);

        ImGui.PopClipRect();
        ImGui.SetCursorPos(initialPos);
    }
}
