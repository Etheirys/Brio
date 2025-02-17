using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.Input;
using Brio.Game.Posing;
using Brio.Input;
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

public class PosingOverlayWindow : Window, IDisposable
{

    private readonly EntityManager _entityManager;
    private readonly CameraService _cameraService;
    private readonly ConfigurationService _configurationService;
    private readonly PosingService _posingService;
    private readonly GPoseService _gPoseService;
    private readonly GameInputService _gameInputService;

    private List<ClickableItem> _selectingFrom = [];
    private Transform? _trackingTransform;
    private readonly PosingTransformEditor _posingTransformEditor = new();

    private const int _gizmoId = 142857;
    private const string _boneSelectPopupName = "brio_bone_select_popup";

    public PosingOverlayWindow(EntityManager entityManager, CameraService cameraService, GameInputService gameInputService, ConfigurationService configService, PosingService posingService, GPoseService gPoseService)
        : base("##brio_posing_overlay_window", ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        Namespace = "brio_posing_overlay_namespace";

        IsOpen = configService.Configuration.Posing.OverlayDefaultsOn;
        _entityManager = entityManager;
        _cameraService = cameraService;
        _configurationService = configService;
        _posingService = posingService;
        _gPoseService = gPoseService;
        _gameInputService = gameInputService;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
    }

    public override void PreDraw()
    {
        base.PreDraw();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0), ImGuiCond.Always);
        SizeCondition = ImGuiCond.Always;

        var io = ImGui.GetIO();
        Size = io.DisplaySize * ImGui.GetFontSize();

        Flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoCollapse;

        ImGuizmo.SetID(_gizmoId);

        //if(_trackingTransform.HasValue)
        //{
        //    Flags &= ~ImGuiWindowFlags.NoInputs;
        //}
    }

    public override void Draw()
    {
        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

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

        if(posing.Selected.Value is not null and BonePoseInfoId)
        {
            _gameInputService.AllowEscape = false;

            if(InputService.IsKeyBindDown(KeyBindEvents.Poseing_Esc))
            {
                posing.ClearSelection();
            }
        }
        else
        {
            _gameInputService.AllowEscape = true;
        }

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
        if(camera == null)
            return;

        // Model Transform
        if(camera->WorldToScreen(posing.ModelPosing.Transform.Position, out var modelScreen))
        {
            clickables.Add(new ClickableItem
            {
                Item = PosingSelectionType.ModelTransform,
                ScreenPosition = modelScreen,
                Size = config.BoneCircleSize,
            });
        }

        // Bone Transforms
        if(posing.Actor.IsProp == false)
        {
            foreach(var (skeleton, poseSlot) in posing.SkeletonPosing.Skeletons)
            {
                if(!skeleton.IsValid)
                    continue;

                var charaBase = skeleton.CharacterBase;
                if(charaBase == null)
                    continue;

                var modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
                }.ToMatrix();


                foreach(var bone in skeleton.Bones)
                {
                    if(!_posingService.OverlayFilter.IsBoneValid(bone, poseSlot) || bone.Name == "n_throw")
                        continue;

                    var boneWorldPosition = Vector3.Transform(bone.LastTransform.Position, modelMatrix);

                    if(camera->WorldToScreen(boneWorldPosition, out var boneScreen))
                    {
                        clickables.Add(new ClickableItem
                        {
                            Item = posing.SkeletonPosing.GetBonePose(bone).Id,
                            ScreenPosition = boneScreen,
                            Size = config.BoneCircleSize,
                        });

                        if(bone.Parent != null)
                        {
                            if(!_posingService.OverlayFilter.IsBoneValid(bone.Parent, poseSlot))
                                continue;

                            var parentWorldPosition = Vector3.Transform(bone.Parent.LastTransform.Position, modelMatrix);
                            if(camera->WorldToScreen(parentWorldPosition, out var parentScreen))
                            {
                                clickables.Last().ParentScreenPosition = parentScreen;
                            }

                        }
                    }
                }
            }
        }

        // Selection
        foreach(var clickable in clickables)
        {
            if(posing.Selected.Equals(clickable.Item))
                clickable.CurrentlySelected = true;
        }
    }

    private void HandleSkeletonInput(PosingCapability posing, OverlayUIState uiState, List<ClickableItem> clickables)
    {
        if(!uiState.SkeletonInputEnabled)
            return;

        var clicked = new List<ClickableItem>();
        var hovered = new List<ClickableItem>();

        foreach(var clickable in clickables)
        {
            var start = new Vector2(clickable.ScreenPosition.X - clickable.Size, clickable.ScreenPosition.Y - clickable.Size);
            var end = new Vector2(clickable.ScreenPosition.X + clickable.Size, clickable.ScreenPosition.Y + clickable.Size);
            if(ImGui.IsMouseHoveringRect(start, end))
            {
                hovered.Add(clickable);
                clickable.CurrentlyHovered = true;
                uiState.AnyClickableHovered = true;

                ImGui.SetNextFrameWantCaptureMouse(true);

                if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    clicked.Add(clickable);
                    clickable.WasClicked = true;
                    uiState.AnyClickableClicked = true;
                }
            }
        }

        if(clicked.Count != 0)
        {
            posing.Selected = clicked[0].Item;

            if(clicked.Count > 1)
            {
                _selectingFrom = clicked;
                ImGui.OpenPopup(_boneSelectPopupName);
            }
        }

        if(hovered.Count != 0 && clicked.Count == 0)
        {
            ImGui.SetNextWindowPos(ImGui.GetMousePos() + new Vector2(15, 10), ImGuiCond.Always);
            if(ImGui.Begin("gizmo_bone_select_preview", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove))
            {
                foreach(var hover in hovered)
                {
                    ImGui.BeginDisabled();
                    ImGui.Selectable($"{hover.Item.DisplayName}###selectable_{hover.GetHashCode()}", hover.CurrentlySelected);
                    ImGui.EndDisabled();
                }

                ImGui.End();
            }

            var wheel = ImGui.GetIO().MouseWheel;
            if(wheel != 0)
            {
                if(hovered.Count == 1)
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
        using(var popup = ImRaii.Popup(_boneSelectPopupName))
        {
            if(popup.Success)
            {
                int selectedIndex = -1;
                foreach(var click in _selectingFrom)
                {
                    bool isSelected = posing.Selected == click.Item;
                    if(isSelected)
                        selectedIndex = _selectingFrom.IndexOf(click);

                    if(ImGui.Selectable($"{click.Item.DisplayName}###clickable_{click.GetHashCode()}", isSelected))
                    {
                        posing.Selected = click.Item;
                        _selectingFrom = [];
                        ImGui.CloseCurrentPopup();
                    }
                }

                var wheel = ImGui.GetIO().MouseWheel;
                if(wheel != 0)
                {
                    if(wheel < 0)
                    {
                        selectedIndex++;
                        if(selectedIndex >= _selectingFrom.Count)
                            selectedIndex = 0;
                    }
                    else
                    {
                        selectedIndex--;
                        if(selectedIndex < 0)
                            selectedIndex = _selectingFrom.Count - 1;
                    }

                    posing.Selected = _selectingFrom[selectedIndex].Item;
                }
            }
        }
    }

    private static void DrawSkeletonLines(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables)
    {
        if(!uiState.DrawSkeletonLines)
            return;

        foreach(var clickable in clickables)
        {
            if(clickable.ParentScreenPosition.HasValue)
            {
                float thickness = config.SkeletonLineThickness;
                uint color = uiState.SkeletonLinesEnabled ? config.SkeletonLineActiveColor : config.SkeletonLineInactiveColor;
                ImGui.GetWindowDrawList().AddLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, color, thickness);
            }
        }
    }

    private void DrawSkeletonDots(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables)
    {
        if(!uiState.DrawSkeletonDots)
            return;

        foreach(var clickable in clickables)
        {
            bool isFilled = clickable.CurrentlySelected || clickable.CurrentlyHovered;

            var color = config.BoneCircleNormalColor;

            if(clickable.CurrentlyHovered)
                color = config.BoneCircleHoveredColor;

            if(clickable.CurrentlySelected)
                color = config.BoneCircleSelectedColor;

            if(!uiState.SkeletonDotsEnabled)
                color = config.BoneCircleInactiveColor;

            if(clickable.Item == PosingSelectionType.ModelTransform && _configurationService.Configuration.Posing.ModelTransformStandout)
            {
                ImGui.GetWindowDrawList().AddCircleFilled(clickable.ScreenPosition, clickable.Size + 3, config.ModelTransformCircleStandOutColor);
                continue;
            }

            if(isFilled)
                ImGui.GetWindowDrawList().AddCircleFilled(clickable.ScreenPosition, clickable.Size, color);
            else
                ImGui.GetWindowDrawList().AddCircle(clickable.ScreenPosition, clickable.Size, color);
        }
    }

    private unsafe void DrawGizmo(PosingCapability posing, OverlayUIState uiState)
    {
        if(!uiState.DrawGizmo)
            return;

        if(posing.Selected.Value is None)
            return;

        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
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
                if(bone == null)
                    return false;

                if(!_posingService.OverlayFilter.IsBoneValid(bone, boneSelect.Slot))
                {
                    return false;
                }

                currentTransform = bone.LastTransform;

                var charaBase = bone.Skeleton.CharacterBase;
                if(charaBase == null)
                    return false;

                var modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = Vector3.Clamp((Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor, new Vector3(.5f), new Vector3(1.5f))
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

        if(!shouldDraw)
            return;


        var lastObserved = _trackingTransform ?? currentTransform;
        var matrix = lastObserved.ToMatrix();

        ImGuizmo.BeginFrame();
        var io = ImGui.GetIO();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.AllowAxisFlip(_configurationService.Configuration.Posing.AllowGizmoAxisFlip);
        ImGuizmo.SetDrawlist();
        ImGuizmo.Enable(uiState.GizmoEnabled);

        Transform? newTransform = null;

        if(ImGuizmoExtensions.MouseWheelManipulate(ref matrix))
        {
            newTransform = matrix.ToTransform();
            _trackingTransform = newTransform;
        }

        if(ImGuizmo.Manipulate(
            ref worldViewMatrix.M11,
            ref projectionMatrix.M11,
            _posingService.Operation.AsGizmoOperation(),
            _posingService.CoordinateMode.AsGizmoMode(),
            ref matrix.M11
        ))
        {
            newTransform = matrix.ToTransform();
            _trackingTransform = newTransform;
        }

        if(_trackingTransform.HasValue && !ImGuizmo.IsUsing())
        {
            posing.Snapshot(false, false);
            _trackingTransform = null;
        }

        ImGuizmo.Enable(true);

        if(newTransform != null)
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
        if(newState)
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
        public bool PopupOpen => ImGui.IsPopupOpen(_boneSelectPopupName);
        public bool UsingGizmo => ImGuizmo.IsUsing();
        public bool HoveringGizmo => ImGuizmo.IsOver();
        public bool AnyActive => ImGui.IsAnyItemActive();
        public bool AnyWindowHovered => ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
        public bool UserDisablingSkeleton => InputService.IsKeyBindDown(KeyBindEvents.Posing_DisableSkeleton);
        public bool UserDisablingGizmo => InputService.IsKeyBindDown(KeyBindEvents.Posing_DisableGizmo);
        public bool UserHidingOverlay => InputService.IsKeyBindDown(KeyBindEvents.Posing_HideOverlay);


        public bool AnythingBusy => PopupOpen || UsingGizmo || AnyActive || AnyWindowHovered;

        public bool AnyClickableHovered = false;
        public bool AnyClickableClicked = false;

        public bool DrawSkeletonLines => !UserHidingOverlay && configuration.ShowSkeletonLines && (!UsingGizmo || !configuration.HideSkeletonWhenGizmoActive);
        public bool DrawSkeletonDots => !UserHidingOverlay && (!UsingGizmo || !configuration.HideSkeletonWhenGizmoActive);
        public bool SkeletonLinesEnabled => !PopupOpen && !UsingGizmo && !UserDisablingSkeleton;
        public bool SkeletonDotsEnabled => !PopupOpen && !UsingGizmo && !UserDisablingSkeleton;
        public bool SkeletonInputEnabled => !AnythingBusy && DrawSkeletonDots && SkeletonDotsEnabled;

        public bool DrawGizmo => !UserHidingOverlay && !(configuration.HideGizmoWhenAdvancedPosingOpen && UIManager.IsPosingGraphicalWindowOpen);
        public bool GizmoEnabled => !PopupOpen && !AnyClickableClicked && !AnyClickableHovered && !UserDisablingGizmo;
    }

    public class ClickableItem
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
