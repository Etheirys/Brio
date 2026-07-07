using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Capabilities.World;
using Brio.Capabilities.WorldObjects;
using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Game.Input;
using Brio.Game.Posing;
using Brio.Game.WorldObjects.Objects;
using Brio.Input;
using Brio.Services;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using OneOf.Types;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class PosingOverlayToolbarWindow : Window
{
    private const string _boneFilterPopupName = "bone_filter_popup";

    private readonly BoneSearchControl _boneSearchControl = new();

    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly ConfigurationService _configurationService;
    private readonly GameInputService _gameInputService;
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly PosingService _posingService;
    private readonly EntityManager _entityManager;
    private readonly LightWindow _lightWindow;
    private readonly IFramework _framework;

    private bool _pushedStyle = false;
    public PosingOverlayToolbarWindow(PosingOverlayWindow overlayWindow, IFramework framework, LightWindow lightWindow, GameInputService gameInputService, EntityManager entityManager, PosingTransformWindow overlayTransformWindow, PosingService posingService, ConfigurationService configurationService) : base($"{Brio.Name} OVERLAY###brio_posing_overlay_toolbar_window", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        Namespace = "brio_posing_overlay_toolbar_namespace";

        _overlayTransformWindow = overlayTransformWindow;
        _configurationService = configurationService;
        _gameInputService = gameInputService;
        _overlayWindow = overlayWindow;
        _entityManager = entityManager;
        _posingService = posingService;
        _lightWindow = lightWindow;
        _framework = framework;

        ShowCloseButton = false;
        this.AllowBackgroundBlur = false;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(((button4XSize * 4) + 30) * ImGuiHelpers.GlobalScale, 400),
            MaximumSize = new Vector2(((button4XSize * 4) + 30) * ImGuiHelpers.GlobalScale, 400)
        };
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

        return base.DrawConditions();
    }

    public override void PreDraw()
    {
        base.PreDraw();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowTitleAlign, new Vector2(0.5f, 0.5f));
        ImGui.PushStyleColor(ImGuiCol.NavWindowingHighlight, UIConstants.Transparent);

       _pushedStyle = true;
    }
    public override void PostDraw()
    {
        if(_pushedStyle)
        {
            _pushedStyle = false;

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(1);
        }

        base.PostDraw();
    }

    public override void Draw()
    {
        ImBrio.BlurWindow();

        if(_pushedStyle)
        {
            _pushedStyle = false;
            ImGui.PopStyleVar(1);
        }

        _entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing);
        _entityManager.TryGetCapabilityFromSelectedEntity<LightTransformCapability>(out var lightTransform);
        _entityManager.TryGetCapabilityFromSelectedEntity<WorldObjectTransformCapability>(out var worldTransform);

        bool hasMultipleActorsSelected = _entityManager.SelectedEntities.Count > 1;

        if(posing?.Selected.Value is not null and BonePoseInfoId)
        {
            _gameInputService.AllowEscape = false;

            if(InputManagerService.ActionKeysPressed(InputAction.Posing_Esc))
            {
                posing.ClearSelection();
            }
        }
        else if(hasMultipleActorsSelected)
        {
            _gameInputService.AllowEscape = false;

            if(InputManagerService.ActionKeysPressed(InputAction.Posing_Esc))
            {
                var primaryEntityId = _entityManager.SelectedEntityById;
                if(primaryEntityId.HasValue)
                {
                    _entityManager.SetSelectedEntity(primaryEntityId.Value);
                }
            }
        }
        else
        {
            _gameInputService.AllowEscape = true;
        }

        DrawButtons(posing, lightTransform, worldTransform, hasMultipleActorsSelected);
        DrawBoneFilterPopup();
    }

    public float button3XSize => ImGui.GetTextLineHeight() * 3.2f;
    public float button4XSize => ImGui.GetTextLineHeight() * 2.4f;
    public float button2XSize => ImGui.GetTextLineHeight() * 5f;

    public Vector2 button2XSizeVector2 => new(button2XSize, button2XSize / 2f);
    public Vector2 button3x3Vector2 => new(button3XSize, button3XSize / 1.2f);
    public Vector2 button3XSizeVector2 => new(button3XSize, button3XSize / 1.2f);
    public Vector2 button4XSizeVector2 => new(button4XSize, button3XSize);

    private void DrawButtons(PosingCapability? posing, LightTransformCapability? lightTransform, WorldObjectTransformCapability? worldTransform, bool hasMultipleActorsSelected)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, UIConstants.Transparent);

        using(ImRaii.Disabled(posing is null))
        {
            if(ImBrio.BoneOverlayVisibilityButton("bones_visible", posing?.Actor.IsOverlayVisible ?? false, button4XSizeVector2))
            {
                posing?.Actor.IsOverlayVisible = !posing.Actor.IsOverlayVisible;
            }
        }
        ImBrio.AttachToolTip(posing is null ? "(This Entity has no Bones)" : posing?.Actor.IsOverlayVisible ?? false ? "Hide Actor's bones in overlay" : "Always show Actor's bones in overlay");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _overlayTransformWindow.IsOpen ? UIConstants.ToggleButtonActive : ThemeManager.CurrentTheme.Text.Text))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.LocationCrosshairs.ToIconString()}###toggle_transforms_window", button4XSizeVector2))
                    _overlayTransformWindow.IsOpen = !_overlayTransformWindow.IsOpen;
            }
        }
        ImBrio.AttachToolTip("Toggle Transform Window");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _lightWindow.IsOpen ? UIConstants.ToggleButtonActive : ThemeManager.CurrentTheme.Text.Text))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Lightbulb.ToIconString()}###toggle_light_window", button4XSizeVector2))
                    _lightWindow.IsOpen = !_lightWindow.IsOpen;
            }
        }
        ImBrio.AttachToolTip("Toggle Light Window");

        ImGui.SameLine();

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.WindowClose.ToIconString()}###close_overlay", button4XSizeVector2))
                _overlayWindow.IsOpen = false;
        }
        ImBrio.AttachToolTip("Close Overlay");

        //
        // ------------- Gizmo

        if(ImBrio.SeparatorTextButton("Gizmo",
            _posingService.CoordinateMode == PosingCoordinateMode.Local ? FontAwesomeIcon.Globe : FontAwesomeIcon.Atom,
            tooltip: _posingService.CoordinateMode == PosingCoordinateMode.Local ? "Switch to World" : "Switch to Local")
            || InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_ToggleWorld))
        {
            _posingService.CoordinateMode = _posingService.CoordinateMode == PosingCoordinateMode.Local ? PosingCoordinateMode.World : PosingCoordinateMode.Local;
        }

        //

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Translate ? UIConstants.ToggleButtonActive : ThemeManager.CurrentTheme.Text.Text))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsUpDownLeftRight.ToIconString()}###select_position", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Translate))
                    _posingService.Operation = PosingOperation.Translate;
            }
        }
        ImBrio.AttachToolTip("Position");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Rotate ? UIConstants.ToggleButtonActive : ThemeManager.CurrentTheme.Text.Text))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ArrowsSpin.ToIconString()}###select_rotate", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Rotate))
                    _posingService.Operation = PosingOperation.Rotate;
            }
        }
        ImBrio.AttachToolTip("Rotation");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Scale ? UIConstants.ToggleButtonActive : ThemeManager.CurrentTheme.Text.Text))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.ExpandAlt.ToIconString()}###select_scale", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Scale))
                    _posingService.Operation = PosingOperation.Scale;
            }
        }
        ImBrio.AttachToolTip("Scale");

        ImGui.SameLine();

        using(ImRaii.PushColor(ImGuiCol.Text, _posingService.Operation == PosingOperation.Universal ? UIConstants.ToggleButtonActive : ThemeManager.CurrentTheme.Text.Text))
        {
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.CompressArrowsAlt.ToIconString()}###select_universal", new Vector2(button4XSize)) || InputManagerService.ActionKeysPressed(InputAction.Posing_Universal))
                {
                    _posingService.Operation = PosingOperation.Universal;
                }
            }
        }
        ImBrio.AttachToolTip("Universal");

        //
        // ------------- Entity Specific
        //

        ImBrio.SeparatorText(hasMultipleActorsSelected ? "Multiple Selected" : _entityManager.SelectedEntity?.FriendlyName ?? "No Selection");

        var bone = posing?.Selected.Match(
              boneSelect => posing.SkeletonPosing.GetBone(boneSelect),
              _ => null,
              _ => null
            ) ?? null;

        using(ImRaii.Disabled(posing is null))
        {
            // IK Button
            bool enabled = false;
            if(posing?.Selected.Value is BonePoseInfoId boneId)
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

            // Clear Button

            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                using(ImRaii.Disabled(posing?.Selected.Value is None))
                {
                    if(ImGui.Button($"{FontAwesomeIcon.MinusSquare.ToIconString()}###clear_selected", new Vector2(button4XSize)))
                    {
                        if(hasMultipleActorsSelected)
                        {
                            var primaryEntityId = _entityManager.SelectedEntityById;
                            if(primaryEntityId.HasValue)
                            {
                                _entityManager.SetSelectedEntity(primaryEntityId.Value);
                            }
                        }
                        else
                        {
                            posing?.ClearSelection();
                        }
                    }
                }
            }
            ImBrio.AttachToolTip("Clear Selection");

            ImGui.SameLine();

            // Select Parent Button

            var parentBone = bone?.Parent;

            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                using(ImRaii.Disabled(parentBone == null))
                {
                    if(ImGui.Button($"{FontAwesomeIcon.LevelUpAlt.ToIconString()}###select_parent", new Vector2(button4XSize)))
                        posing?.SetBoneSelection(new BonePoseInfoId(parentBone!.Name, parentBone!.PartialId, PoseInfoSlot.Character), false);
                }
            }
            ImBrio.AttachToolTip("Select Parent");

            ImGui.SameLine();

            // MirrorMode Button

            PosingEditorCommon.DrawMirrorModeSelect(posing, new Vector2(button4XSize));

            // Set IK Button

            //using(ImRaii.Disabled(posing?.SkeletonPosing.PoseInfo.HasIKStacks is false))
            //{
            //    using(ImRaii.PushFont(UiBuilder.IconFont))
            //        if(ImGui.Button($"{FontAwesomeIcon.Lock.ToIconString()}###clear_ik", new Vector2(button4XSize)))
            //            posing?.SkeletonPosing.ResetIK();
            //    ImBrio.AttachToolTip($"Set IK Changes{(!posing?.SkeletonPosing.PoseInfo.HasIKStacks ?? false ? ". Enable IK & make a change with IK to 'Lock in' IK changes using this button." : "")}");

            //    var center = ImGui.GetItemRectMin() + (ImGui.GetItemRectSize() / 2);
            //    var radius = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.9f);
            //    var thickness = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.1f);

            //    ImGui.GetWindowDrawList().AddCircle(center, radius, ImGui.GetColorU32(ImGuiCol.Text) & 0x80FFFFFF, 32, thickness);

            //    if(posing?.SkeletonPosing.PoseInfo.HasIKStacks is false)
            //    {
            //        thickness += 0.2f;
            //        var offset = (radius - thickness) / MathF.Sqrt(2.0f);
            //        var lineStart = center + new Vector2(-offset, -offset);
            //        var lineEnd = center + new Vector2(offset, offset);
            //        ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, 0x400000FF, thickness);
            //    }
            //}

            //ImGui.SameLine();

            // Bone Filter Button

            using(ImRaii.Disabled(hasMultipleActorsSelected))
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Bone.ToIconString()}###toggle_filter_window", button2XSizeVector2))
                    ImGui.OpenPopup(_boneFilterPopupName);
            }
            ImBrio.AttachToolTip("Bone Filter");

            ImGui.SameLine();

            // Bone Search Button

            using(ImRaii.Disabled(hasMultipleActorsSelected))
            using(ImRaii.PushFont(UiBuilder.IconFont))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Search.ToIconString()}###bone_search", button2XSizeVector2))
                    ImGui.OpenPopup("overlay_bone_search_popup");
            }
            ImBrio.AttachToolTip("Bone Search");
        }

        //
        // ------------- State
        //

        var selectedVfx = worldTransform?.GameBgObject as StaticVfxObject;

        using(ImRaii.Disabled(hasMultipleActorsSelected || !((posing?.CanResetBone(bone) ?? false) || selectedVfx is not null)))
            if(ImBrio.SeparatorTextButton("State", FontAwesomeIcon.Retweet, selectedVfx is not null ? "Restart VFX" : "Reset Bone"))
            {
                if(posing is not null)
                    posing.ResetSelectedBone();
                else
                    selectedVfx?.Resume();
            }

        DrawFrezeButton(hasMultipleActorsSelected);

        ImGui.SameLine();

        // Undo Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!_entityManager.CanUndoSelected))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Reply.ToIconString()}###undo_pose", new Vector2(button4XSize)) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Undo) && _entityManager.CanUndoSelected))
                {
                    _entityManager.UndoSelected();
                }
            }
        }
        ImBrio.AttachToolTip("Undo");

        ImGui.SameLine();

        // Redo Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(!_entityManager.CanRedoSelected))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Share.ToIconString()}###redo_pose", new Vector2(button4XSize)) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Redo) && _entityManager.CanRedoSelected))
                {
                    _entityManager.RedoSelected();
                }
            }
        }
        ImBrio.AttachToolTip("Redo");

        ImGui.SameLine();

        // Reset Pose Button

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            using(ImRaii.Disabled(hasMultipleActorsSelected || !CanResetSelectedTransform(posing, lightTransform, worldTransform)))
            {
                if(ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}###reset_pose", new Vector2(button4XSize)))
                {
                    if(posing is not null)
                        ImGui.OpenPopup("overlay_reset_pose_popup");
                    else
                        ResetSelectedTransform(posing, lightTransform, worldTransform);
                }
            }
        }
        ImBrio.AttachToolTip($"Reset Transform {_entityManager.SelectedEntity?.FriendlyName}");

        //using(ImRaii.PushFont(UiBuilder.IconFont))
        //{
        //    using(ImRaii.Disabled(!posing?.HasOverride(posing.SkeletonPosing.FilterNonFaceBones) ?? true))
        //    {
        //        if(ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}###reset_body_pose", new Vector2(button3XSize)))
        //        {
        //            posing!.Snapshot(false, reconcile: false);
        //            posing!.ClearStacks(posing.SkeletonPosing.FilterNonFaceBones);
        //        }

        //        ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2, ImGui.GetColorU32(ImGuiCol.Text), FontAwesomeIcon.ChildReaching.ToIconString());

        //    }
        //}

        //ImBrio.AttachToolTip("Reset Body");

        //ImGui.SameLine();

        //using(ImRaii.PushFont(UiBuilder.IconFont))
        //using(ImRaii.Disabled(!posing?.HasOverride(posing.SkeletonPosing.FilterFaceBones) ?? false))
        //{
        //    if(ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}###reset_face_pose", new Vector2(button3XSize)))
        //    {
        //        posing?.ClearStacks(posing.SkeletonPosing.FilterFaceBones);
        //    }

        //    ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2, ImGui.GetColorU32(ImGuiCol.Text), FontAwesomeIcon.Smile.ToIconString());

        //}

        //ImBrio.AttachToolTip("Reset Face");

        //ImGui.SameLine();

        //
        // ------------- File
        //

        ImBrio.SeparatorText("File");

        // Load Pose Button

        using(ImRaii.Disabled(hasMultipleActorsSelected || posing is null))
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.FileDownload.ToIconString()}###import_pose", button2XSizeVector2))
                ImGui.OpenPopup("DrawImportPoseMenuPopup");
        }
        ImBrio.AttachToolTip("Import Pose");

        ImGui.SameLine();

        // Save Pose Button

        using(ImRaii.Disabled(hasMultipleActorsSelected || posing is null))
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Save.ToIconString()}###export_pose", button2XSizeVector2))
                ImGui.OpenPopup("DrawExportPoseMenuPopup");
        }
        ImBrio.AttachToolTip("Save Pose");

        ImGui.PopStyleColor();

        // popups
        //

        FileUIHelpers.DrawImportPoseMenuPopup("postingOverlay", posing, true);
        FileUIHelpers.DrawExportPoseMenuPopup(posing);

        using(var popup = ImRaii.Popup("overlay_reset_pose_popup", ImGuiWindowFlags.AlwaysAutoResize))
        {
            if(popup.Success && posing is not null)
            {
                DrawResetMenu(posing);
            }
        }

        using(var popup = ImRaii.Popup("overlay_bone_search_popup"))
        {
            if(popup.Success)
            {
                _boneSearchControl.Draw("overlay_bone_search", posing!);
            }
        }

        using(var popup = ImRaii.Popup("overlay_bone_ik"))
        {
            if(popup.Success)
            {
                if(posing?.Selected.Value is BonePoseInfoId id)
                {
                    var info = posing.SkeletonPosing.GetBonePose(id);
                    BoneIKEditor.Draw(info, posing);
                }
            }
        }
    }

    private static bool CanResetSelectedTransform(PosingCapability? posing, LightTransformCapability? lightTransform, WorldObjectTransformCapability? worldTransform)
    {
        if(posing is not null)
            return posing.HasOverride();

        if(lightTransform is not null)
            return lightTransform.HasOverride;

        if(worldTransform is not null)
            return worldTransform.TransformOverride;

        return false;
    }

    private static void DrawResetMenu(PosingCapability posing)
    {
        using(ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f)))
        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            var buttonSize = new Vector2(155 * ImGuiHelpers.GlobalScale, 0);

            if(ImBrio.IconButtonWithText(FontAwesomeIcon.Undo, "Reset Pose", buttonSize))
            {
                posing.Reset(false, false);
                ImGui.CloseCurrentPopup();
            }

            using(ImRaii.Disabled(!posing.HasOverride(posing.SkeletonPosing.FilterNonFaceBones)))
            {
                if(ImBrio.IconButtonWithText(FontAwesomeIcon.ChildReaching, "Reset Body", buttonSize))
                {
                    posing.Snapshot(false, reconcile: false);
                    posing.SkeletonPosing.PoseInfo.Clear(posing.SkeletonPosing.FilterNonFaceBones);
                    ImGui.CloseCurrentPopup();
                }
            }

            using(ImRaii.Disabled(!posing.HasOverride(posing.SkeletonPosing.FilterFaceBones)))
            {
                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Smile, "Reset Face", buttonSize))
                {
                    posing.SkeletonPosing.PoseInfo.Clear(posing.SkeletonPosing.FilterFaceBones);
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private static void ResetSelectedTransform(PosingCapability? posing, LightTransformCapability? lightTransform, WorldObjectTransformCapability? worldTransform)
    {
        if(posing is not null)
        {
            posing.Reset(false, false);
            return;
        }

        if(lightTransform is not null)
        {
            lightTransform.Reset();
            return;
        }

        worldTransform?.Reset();
    }

    private void DrawFrezeButton(bool hasMultipleActorsSelected)
    {
        // Freeze Button

        bool anyFreezable = false;
        bool allFrozen = true;
        foreach(var entityId in _entityManager.SelectedEntities)
        {
            if(!_entityManager.TryGetEntity(entityId, out var entity))
                continue;

            if(entity.TryGetCapability<ActionTimelineCapability>(out var cap))
            {
                anyFreezable = true;
                if(cap.SpeedMultiplierOverride != 0)
                    allFrozen = false;
            }
            else if(TryGetSelectedVfx(entity, out var vfx))
            {
                anyFreezable = true;
                if(vfx.Speed != 0)
                    allFrozen = false;
            }
        }

        using(ImRaii.Disabled(!anyFreezable))
        using(ImRaii.PushColor(ImGuiCol.Text, anyFreezable && allFrozen ? UIConstants.ToggleButtonActive : ThemeManager.CurrentTheme.Text.Text))
        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            if(ImGui.Button($"{FontAwesomeIcon.Snowflake.ToIconString()}###freeze_toggle", new Vector2(button4XSize)) || (InputManagerService.ActionKeysPressedLastFrame(InputAction.Posing_Freeze) && anyFreezable))
            {
                bool shouldFreeze = !allFrozen;
                foreach(var entityId in _entityManager.SelectedEntities)
                {
                    if(!_entityManager.TryGetEntity(entityId, out var entity))
                        continue;

                    if(entity.TryGetCapability<ActionTimelineCapability>(out var cap))
                    {
                        if(shouldFreeze)
                            cap.SetOverallSpeedOverride(0f);
                        else
                            cap.ResetOverallSpeedOverride();
                    }
                    else if(TryGetSelectedVfx(entity, out var vfx))
                    {
                        vfx.SetSpeed(shouldFreeze ? 0f : 1f);
                    }
                }
            }
        }

        ImBrio.AttachToolTip($"{(allFrozen ? "Un-" : "")}Freeze Selected");
    }

    private static bool TryGetSelectedVfx(Entity entity, [MaybeNullWhen(false)] out StaticVfxObject vfx)
    {
        if(entity.TryGetCapability<WorldObjectTransformCapability>(out var worldTransform) && worldTransform.GameBgObject is StaticVfxObject staticVfx)
        {
            vfx = staticVfx;
            return true;
        }

        vfx = null;
        return false;
    }

    private void DrawBoneFilterPopup()
    {
        if(_entityManager.SelectedEntity is ActorEntity actorEntity)
        {
            using var popup = ImRaii.Popup(_boneFilterPopupName);
            if(popup.Success)
            {
                PosingEditorCommon.DrawBoneFilterEditor(actorEntity.OverlayFilter, _posingService);
            }
        }
    }
}
