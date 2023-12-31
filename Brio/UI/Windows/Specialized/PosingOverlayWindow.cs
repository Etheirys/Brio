using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.UI.Controls.Editors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuizmoNET;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

internal class PosingOverlayWindow : Window, IDisposable
{
    public OPERATION Operation = OPERATION.ROTATE;

    private readonly EntityManager _entityManager;
    private readonly CameraService _cameraService;
    private readonly ConfigurationService _configurationService;
    private readonly PosingService _posingService;
    private readonly GPoseService _gPoseService;

    private List<ClickableItem> _selectingFrom = [];
    private Transform? _trackingTransform;
    private readonly PosingTransformEditor _posingTransformEditor = new();

    private const int _gizmoId = 142857;
    private const string _boneSelectPopupName = "brio_bone_select_popup";

    public PosingOverlayWindow(EntityManager entityManager, CameraService cameraService, ConfigurationService configService, PosingService posingService, GPoseService gPoseService) : base("##brio_posing_overlay_window", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration)
    {
        Namespace = "brio_posing_overlay_namespace";

        IsOpen = configService.Configuration.Posing.OverlayDefaultsOn;
        _entityManager = entityManager;
        _cameraService = cameraService;
        _configurationService = configService;
        _posingService = posingService;
        _gPoseService = gPoseService;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
    }

    public override void PreDraw()
    {
        base.PreDraw();
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

        Flags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration;

        ImGuizmo.SetID(_gizmoId);

        if (ImGuizmo.IsUsing() && _trackingTransform.HasValue)
            Flags &= ~ImGuiWindowFlags.NoInputs;
    }

    public override void Draw()
    {

        if (!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

        var io = ImGui.GetIO();
        ImGui.SetWindowSize(io.DisplaySize);
        var windowPos = ImGui.GetWindowPos();
        ImGuizmo.SetRect(windowPos.X, windowPos.Y, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.AllowAxisFlip(_configurationService.Configuration.Posing.AllowGizmoAxisFlip);
        ImGuizmo.SetDrawlist();

        DrawContent(posing);
    }

    public override void PostDraw()
    {
        ImGuizmo.SetID(0);
        base.PostDraw();
    }

    private unsafe void DrawContent(PosingCapability posing)
    {
        var overlayConfig = _configurationService.Configuration.Posing;
        var uiState = new OverlayUIState(overlayConfig);
        var clickables = new List<ClickableItem>();

        CalculateClickables(posing, uiState, overlayConfig, ref clickables);

        HandleSkeletonInput(posing, uiState, clickables);
        DrawPopup(posing);
        DrawSkeletonLines(uiState, overlayConfig, clickables);
        DrawSkeletonDots(uiState, overlayConfig, clickables);
        DrawGizmo(posing, uiState);
    }

    private unsafe void CalculateClickables(PosingCapability posing, OverlayUIState uiState, PosingConfiguration config, ref List<ClickableItem> clickables)
    {
        var camera = _cameraService.GetCurrentCamera();
        if (camera == null)
            return;

        // Model Transform
        if (camera->WorldToScreen(posing.ModelPosing.Transform.Position, out var modelScreen))
        {
            clickables.Add(new ClickableItem
            {
                Item = PosingSelectionType.ModelTransform,
                ScreenPosition = modelScreen,
                Size = config.BoneCircleSize,
            });
        }

        // Bone Transforms
        foreach (var (skeleton, poseSlot) in posing.SkeletonPosing.Skeletons)
        {
            var charaBase = skeleton.CharacterBase;
            if (charaBase == null)
                continue;

            var modelMatrix = new Transform()
            {
                Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
            }.ToMatrix();


            foreach (var bone in skeleton.Bones)
            {
                if (!_posingService.OverlayFilter.IsBoneValid(bone, poseSlot))
                    continue;

                var boneWorldPosition = Vector3.Transform(bone.LastTransform.Position, modelMatrix);

                if (camera->WorldToScreen(boneWorldPosition, out var boneScreen))
                {
                    clickables.Add(new ClickableItem
                    {
                        Item = posing.SkeletonPosing.GetBonePose(bone).Id,
                        ScreenPosition = boneScreen,
                        Size = config.BoneCircleSize,
                    });

                    if (bone.Parent != null)
                    {
                        if (!_posingService.OverlayFilter.IsBoneValid(bone.Parent, poseSlot))
                            continue;

                        var parentWorldPosition = Vector3.Transform(bone.Parent.LastTransform.Position, modelMatrix);
                        if (camera->WorldToScreen(parentWorldPosition, out var parentScreen))
                        {
                            clickables.Last().ParentScreenPosition = parentScreen;
                        }

                    }
                }
            }
        }

        // Selection
        foreach (var clickable in clickables)
        {
            if (posing.Selected.Equals(clickable.Item))
                clickable.CurrentlySelected = true;
        }
    }

    private void HandleSkeletonInput(PosingCapability posing, OverlayUIState uiState, List<ClickableItem> clickables)
    {
        if (!uiState.SkeletonInputEnabled)
            return;

        var clicked = new List<ClickableItem>();
        var hovered = new List<ClickableItem>();

        foreach (var clickable in clickables)
        {
            var start = new Vector2(clickable.ScreenPosition.X - clickable.Size, clickable.ScreenPosition.Y - clickable.Size);
            var end = new Vector2(clickable.ScreenPosition.X + clickable.Size, clickable.ScreenPosition.Y + clickable.Size);
            if (ImGui.IsMouseHoveringRect(start, end))
            {
                hovered.Add(clickable);
                clickable.CurrentlyHovered = true;
                uiState.AnyClickableHovered = true;

                ImGui.SetNextFrameWantCaptureMouse(true);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    clicked.Add(clickable);
                    clickable.WasClicked = true;
                    uiState.AnyClickableClicked = true;
                }
            }
        }

        if (clicked.Any())
        {
            posing.Selected = clicked[0].Item;

            if (clicked.Count > 1)
            {
                _selectingFrom = clicked;
                ImGui.OpenPopup(_boneSelectPopupName);
            }
        }

        if (hovered.Any() && !clicked.Any())
        {
            if (ImGui.Begin("gizmo_bone_select_preview", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
            {
                ImGui.SetWindowPos(ImGui.GetMousePos() + new Vector2(1, 0));
                foreach (var hover in hovered)
                {
                    ImGui.BeginDisabled();

                    ImGui.Selectable($"{hover.Item.DisplayName}###selectable_{hover.GetHashCode()}", hover.CurrentlySelected);
                    ImGui.EndDisabled();
                }
            }

            ImGui.End();

            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                if (hovered.Count == 1)
                {
                    posing.Selected = hovered[0].Item;
                }
                else
                {
                    _selectingFrom = hovered;
                    ImGui.OpenPopup(_boneSelectPopupName);
                }
            }
        }
    }


    private void DrawPopup(PosingCapability posing)
    {
        using (var popup = ImRaii.Popup(_boneSelectPopupName))
        {
            if (popup.Success)
            {
                int selectedIndex = -1;
                foreach (var click in _selectingFrom)
                {
                    bool isSelected = posing.Selected == click.Item;
                    if (isSelected)
                        selectedIndex = _selectingFrom.IndexOf(click);

                    if (ImGui.Selectable($"{click.Item.DisplayName}###clickable_{click.GetHashCode()}", isSelected))
                    {
                        posing.Selected = click.Item;
                        _selectingFrom = [];
                        ImGui.CloseCurrentPopup();
                    }
                }

                var wheel = ImGui.GetIO().MouseWheel;
                if (wheel != 0)
                {
                    if (wheel < 0)
                    {
                        selectedIndex++;
                        if (selectedIndex >= _selectingFrom.Count)
                            selectedIndex = 0;
                    }
                    else
                    {
                        selectedIndex--;
                        if (selectedIndex < 0)
                            selectedIndex = _selectingFrom.Count - 1;
                    }

                    posing.Selected = _selectingFrom[selectedIndex].Item;
                }

            }
        }
    }

    private void DrawSkeletonLines(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables)
    {
        if (!uiState.DrawSkeletonLines)
            return;

        foreach (var clickable in clickables)
        {
            if (clickable.ParentScreenPosition.HasValue)
            {
                float thickness = config.SkeletonLineThickness;
                uint color = uiState.SkeletonLinesEnabled ? config.SkeletonLineActiveColor : config.SkeletonLineInactiveColor;
                ImGui.GetWindowDrawList().AddLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, color, thickness);
            }
        }
    }

    private void DrawSkeletonDots(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables)
    {
        if (!uiState.DrawSkeletonDots)
            return;

        foreach (var clickable in clickables)
        {
            bool isFilled = clickable.CurrentlySelected || clickable.CurrentlyHovered;

            var color = config.BoneCircleNormalColor;

            if (clickable.CurrentlyHovered)
                color = config.BoneCircleHoveredColor;

            if (clickable.CurrentlySelected)
                color = config.BoneCircleSelectedColor;

            if (!uiState.SkeletonDotsEnabled)
                color = config.BoneCircleInactiveColor;

            if (isFilled)
                ImGui.GetWindowDrawList().AddCircleFilled(clickable.ScreenPosition, clickable.Size, color);
            else
                ImGui.GetWindowDrawList().AddCircle(clickable.ScreenPosition, clickable.Size, color);
        }
    }

    private unsafe void DrawGizmo(PosingCapability posing, OverlayUIState uiState)
    {
        if (!uiState.DrawGizmo)
            return;

        if (posing.Selected.Value is None)
            return;

        var camera = _cameraService.GetCurrentCamera();
        if (camera == null)
            return;

        var selected = posing.Selected;

        Matrix4x4 projectionMatrix = camera->GetProjectionMatrix();
        Matrix4x4 viewMatrix = camera->GetViewMatrix();
        viewMatrix.M44 = 1;

        Transform currentTransform = Transform.Identity;
        Matrix4x4 worldViewMatrix = viewMatrix;

        var shouldDraw = selected.Match(
            boneSelect =>
            {
                var bone = posing.SkeletonPosing.GetBone(boneSelect);
                if (bone == null)
                    return false;

                if (!_posingService.OverlayFilter.IsBoneValid(bone, boneSelect.Slot))
                {
                    return false;
                }

                currentTransform = bone.LastTransform;

                var charaBase = bone.Skeleton.CharacterBase;
                if (charaBase == null)
                    return false;

                var modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
                }.ToMatrix();

                worldViewMatrix = modelMatrix * viewMatrix;

                return true;
            },
            _ =>
            {
                currentTransform = posing.ModelPosing.Transform;
                return true;
            },
            _ => false
        );

        if (!shouldDraw)
            return;


        var lastObserved = _trackingTransform ?? currentTransform;
        var matrix = lastObserved.ToMatrix();

        ImGuizmo.Enable(uiState.GizmoEnabled);

        Transform? newTransform = null;

        if (ImGuizmo.Manipulate(
            ref worldViewMatrix.M11,
            ref projectionMatrix.M11,
            Operation,
            _posingService.CoordinateMode.AsGizmoMode(),
            ref matrix.M11
        ))
        {
            newTransform = matrix.ToTransform();
            _trackingTransform = newTransform;
        }

        if (_trackingTransform.HasValue && !ImGuizmo.IsUsing())
        {
            posing.Snapshot();
            _trackingTransform = null;
        }

        ImGuizmo.Enable(true);

        if (newTransform != null)
        {
            selected.Switch(
                bone =>
                {
                    posing.SkeletonPosing.GetBonePose(bone).Apply(newTransform.Value, lastObserved);
                },
                _ =>
                {
                    posing.ModelPosing.Transform = newTransform.Value;
                },
                _ => { }
            );
        }


    }

    private void OnGPoseStateChanged(bool newState)
    {
        if (newState)
            IsOpen = _configurationService.Configuration.Posing.OverlayDefaultsOn;
        else
            IsOpen = false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;
    }

    private class OverlayUIState(PosingConfiguration configuration)
    {
        public bool PopupOpen = ImGui.IsPopupOpen(_boneSelectPopupName);
        public bool UsingGizmo = ImGuizmo.IsUsing();
        public bool HoveringGizmo = ImGuizmo.IsOver();
        public bool AnyActive = ImGui.IsAnyItemActive();
        public bool AnyWindowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
        public bool UserDisablingSkeleton = ImGui.IsKeyDown(configuration.DisableSkeletonHotkey);
        public bool UserDisablingGizmo = ImGui.IsKeyDown(configuration.DisableGizmoHotkey);
        public bool UserHidingOverlay = ImGui.IsKeyDown(configuration.HideOverlayHotkey);


        public bool AnythingBusy => PopupOpen || UsingGizmo || AnyActive || AnyWindowHovered;

        public bool AnyClickableHovered = false;
        public bool AnyClickableClicked = false;

        public bool DrawSkeletonLines => !UserHidingOverlay && configuration.ShowSkeletonLines;
        public bool DrawSkeletonDots => !UserHidingOverlay;
        public bool SkeletonLinesEnabled => !PopupOpen && !UsingGizmo && !UserDisablingSkeleton;
        public bool SkeletonDotsEnabled => !PopupOpen && !UsingGizmo && !UserDisablingSkeleton;
        public bool SkeletonInputEnabled => !AnythingBusy && DrawSkeletonDots && SkeletonDotsEnabled;

        public bool DrawGizmo => !UserHidingOverlay;
        public bool GizmoEnabled => !PopupOpen && !AnyClickableClicked && !AnyClickableHovered && !UserDisablingGizmo;
    }

    internal class ClickableItem
    {
        public PosingSelectionType Item = null!;

        public Vector2 ScreenPosition;
        public Vector2? ParentScreenPosition = null;

        public float Size;
        public bool CurrentlySelected;
        public bool CurrentlyHovered;
        public bool WasClicked;
    }
}
