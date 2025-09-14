using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Game.Input;
using Brio.Game.Posing;
using Brio.Input;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using OneOf.Types;
using System;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class PosingOverlayToolbarWindow : Window
{
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly EntityManager _entityManager;
    private readonly HistoryService _groupedUndoService;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingService _posingService;
    private readonly ConfigurationService _configurationService;
    private readonly GameInputService _gameInputService;

    private readonly BoneSearchControl _boneSearchControl = new();

    private bool _pushedStyle = false;

    private const string _boneFilterPopupName = "bone_filter_popup";

    public PosingOverlayToolbarWindow(PosingOverlayWindow overlayWindow, HistoryService groupedUndoService, GameInputService gameInputService, EntityManager entityManager, PosingTransformWindow overlayTransformWindow, PosingService posingService, ConfigurationService configurationService) : base($"{Brio.Name} OVERLAY###brio_posing_overlay_toolbar_window", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        Namespace = "brio_posing_overlay_toolbar_namespace";

        _overlayWindow = overlayWindow;
        _entityManager = entityManager;
        _overlayTransformWindow = overlayTransformWindow;
        _posingService = posingService;
        _configurationService = configurationService;
        _groupedUndoService = groupedUndoService;
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
        _pushedStyle = true;
    }

    public override void Draw()
    {
        if(_pushedStyle)
        {
            _pushedStyle = false;
            ImGui.PopStyleVar(1);
        }

        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
            return;

        if(!_entityManager.TryGetCapabilityFromSelectedEntity<ActionTimelineCapability>(out var timelineCapability))
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

        DrawButtons(posing, timelineCapability);
        DrawBoneFilterPopup();
    }

    public override void PostDraw()
    {
        if(_pushedStyle)
        {
            _pushedStyle = false;
            ImGui.PopStyleVar(1);
        }

        base.PostDraw();
    }

    private void DrawButtons(PosingCapability posing, ActionTimelineCapability timelineCapability)
    {
        float button3XSize = ImGui.GetTextLineHeight() * 3.2f;
        float button4XSize = ImGui.GetTextLineHeight() * 2.4f;
        float button2XSize = ImGui.GetTextLineHeight() * 5f;

        Vector2 button2XSizeVevtor2 = new(button2XSize, button2XSize / 2);

        ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{(_posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe.ToIconString() : FontAwesomeIcon.Atom.ToIconString())}###select_mode", new Vector2(button3XSize)) || InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_ToggleWorld))
                _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;
        }
        ImBrio.AttachToolTip(_posingService.CoordinateMode == PosingCoordinateMode.Local ? "Switch to World" : "Switch to Local");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _overlayTransformWindow.IsOpen ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.LocationCrosshairs.ToIconString()}###toggle_transforms_window", new Vector2(button3XSize)))
                    _overlayTransformWindow.IsOpen = !_overlayTransformWindow.IsOpen;
            }
        }
        ImBrio.AttachToolTip("Toggle Transform Window");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.WindowClose.ToIconString()}###close_overlay", new Vector2(button3XSize)))
                _overlayWindow.IsOpen = false;
        }
        ImBrio.AttachToolTip("Close Overlay");

        ImGui.Separator();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Translate ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}###select_position", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Translate))
                    _posingService.Operation = PosingOperation.Translate;
            }
        }
        ImBrio.AttachToolTip("Position");

        ImGui.SameLine();


        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Rotate ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsSpin.ToIconString()}###select_rotate", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Rotate))
                    _posingService.Operation = PosingOperation.Rotate;
            }
        }
        ImBrio.AttachToolTip("Rotation");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Scale ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ExpandAlt.ToIconString()}###select_scale", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Scale))
                    _posingService.Operation = PosingOperation.Scale;
            }
        }
        ImBrio.AttachToolTip("Scale");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Universal ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Cubes.ToIconString()}###select_universal", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Universal))
                {
                    _posingService.Operation = PosingOperation.Universal;
                }
            }
        }
        ImBrio.AttachToolTip("Universal");

        //
        // -------------
        //

        ImGui.Separator();

        var bone = posing.Selected.Match(
              boneSelect => posing.SkeletonPosing.GetBone(boneSelect),
              _ => null,
              _ => null
            );

        // IK Button
        bool enabled = false;
        if(posing.Selected.Value is BonePoseInfoId boneId)
        {
            var bonePose = posing.SkeletonPosing.GetBonePose(boneId);
            var ik = bonePose.DefaultIK;
            enabled = ik.Enabled && BrioStyle.EnableStyle;
        }

        using(ImRaii.Disabled(!(bone?.EligibleForIK == true)))
        {
            using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, enabled))
            {
                if(ImGui.Button($"IK###bone_ik", new Vector2(button4XSize)))
                    ImGui.OpenPopup("overlay_bone_ik");
            }
        }
        ImBrio.AttachToolTip("Inverse Kinematics");

        ImGui.SameLine();

        // Bone Filter Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Bone.ToIconString()}###toggle_filter_window", new Vector2(button4XSize)))
                ImGui.OpenPopup(_boneFilterPopupName);
        }
        ImBrio.AttachToolTip("Bone Filter");

        ImGui.SameLine();

        // Bone Search Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}###bone_search", new Vector2(button4XSize)))
                ImGui.OpenPopup("overlay_bone_search_popup");
        }
        ImBrio.AttachToolTip("Bone Search");

        ImGui.SameLine();

        // MirrorMode Button

        PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(button4XSize));

        //
        // -------------
        //

        // Set IK Button

        using(ImRaii.Disabled(posing.SkeletonPosing.PoseInfo.HasIKStacks is false))
        {
            if(ImGui.Button($"IK###clear_ik", new Vector2(button4XSize)))
            {
                posing.SkeletonPosing.ResetIK();
            }
            ImBrio.AttachToolTip($"Set IK {(posing.SkeletonPosing.PoseInfo.HasIKStacks ? "Enable and make a change with IK to Set IK" : "")}");

            var center = ImGui.GetItemRectMin() + (ImGui.GetItemRectSize() / 2);
            var radius = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.8f);
            var thickness = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.1f);

            ImGui.GetWindowDrawList().AddCircle(center, radius, ImGui.GetColorU32(ImGuiCol.Text) & 0x80FFFFFF, 32, thickness);

            if(posing.SkeletonPosing.PoseInfo.HasIKStacks is false)
            {
                thickness += 0.2f;
                var offset = (radius - thickness) / MathF.Sqrt(2.0f);
                var lineStart = center + new Vector2(-offset, -offset);
                var lineEnd = center + new Vector2(offset, offset);
                ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, 0x400000FF, thickness);
            }
        }

        ImGui.SameLine();

        // Select Parent Button

        var parentBone = bone?.Parent;

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(parentBone == null))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowUp.ToIconString()}###select_parent", new Vector2(button4XSize)))
                    posing.Selected = new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character);
            }
        }
        ImBrio.AttachToolTip("Select Parent");

        ImGui.SameLine();

        // Clear Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(posing.Selected.Value is None))
            {
                if(ImGui.Button($"{FontAwesomeIcon.MinusSquare.ToIconString()}###clear_selected", new Vector2(button4XSize)))
                    posing.ClearSelection();
            }
        }
        ImBrio.AttachToolTip("Clear Selection");

        ImGui.SameLine();

        // Freeze Button

        using(ImRaii.PushColor(ImGuiCol.Text, timelineCapability.SpeedMultiplierOverride == 0 ? UIConstants.ToggleButtonActive : UIConstants.ToggleButtonInactive))
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Snowflake.ToIconString()}###freeze_toggle", new Vector2(button4XSize)))
            {
                if(timelineCapability.SpeedMultiplierOverride == 0)
                {
                    timelineCapability.ResetOverallSpeedOverride();
                }
                else
                {
                    timelineCapability.SetOverallSpeedOverride(0);
                }
            }
        }
        ImBrio.AttachToolTip($"{(timelineCapability.SpeedMultiplierOverride == 0 ? "Un-" : "")}Freeze Character");

        //
        // -------------
        //

        ImGui.Separator();

        // Undo Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.CanUndo))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Backward.ToIconString()}###undo_pose", new Vector2(button3XSize)) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Undo) && posing.CanUndo))
                {
                    posing.Undo();
                }
            }
        }
        ImBrio.AttachToolTip("Undo");

        ImGui.SameLine();

        // Redo Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.CanRedo))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Forward.ToIconString()}###redo_pose", new Vector2(button3XSize)) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Redo) && posing.CanRedo))
                {
                    posing.Redo();
                }
            }
        }
        ImBrio.AttachToolTip("Redo");

        ImGui.SameLine();

        // Reset Pose Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!posing.HasOverride))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}###reset_pose", new Vector2(button3XSize)))
                    posing.Reset(false, false);
            }
        }
        ImBrio.AttachToolTip("Reset Pose");

        //
        // -------------
        //

        ImGui.Separator();

        // Load Pose Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.FileDownload.ToIconString()}###import_pose", button2XSizeVevtor2))
                ImGui.OpenPopup("DrawImportPoseMenuPopup");
        }
        ImBrio.AttachToolTip("Import Pose");

        FileUIHelpers.DrawImportPoseMenuPopup(posing, false);

        ImGui.SameLine();

        // Save Pose Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Save.ToIconString()}###export_pose", button2XSizeVevtor2))
                FileUIHelpers.ShowExportPoseModal(posing);
        }
        ImBrio.AttachToolTip("Save Pose");

        ImGui.PopStyleColor();

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
}
