using Brio.Capabilities.Posing;
using Brio.Capabilities.World;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Game.World;
using Brio.Input;
using Brio.UI.Controls.Editors;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
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
    private readonly HistoryService _groupedUndoService;

    private readonly LightingService _lightingService;

    private List<ClickableItem> _selectingFrom = [];
    private Transform? _trackingTransform;
    private readonly PosingTransformEditor _posingTransformEditor = new();
    private List<(EntityId id, PoseInfo info, Transform model)>? _groupedPendingSnapshot = null;

    private const int _gizmoId = 142857;
    private const string _boneSelectPopupName = "brio_bone_select_popup";

    public PosingOverlayWindow(EntityManager entityManager, CameraService cameraService, LightingService lightingService, HistoryService groupedUndoService, ConfigurationService configService, PosingService posingService, GPoseService gPoseService)
        : base("##brio_posing_overlay_window", ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        Namespace = "brio_posing_overlay_namespace";

        IsOpen = configService.Configuration.Posing.OverlayDefaultsOn;
        _entityManager = entityManager;
        _cameraService = cameraService;
        _configurationService = configService;
        _posingService = posingService;
        _gPoseService = gPoseService;
        _groupedUndoService = groupedUndoService;
        _lightingService = lightingService;

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
        var overlayConfig = _configurationService.Configuration.Posing;
        var uiState = new OverlayUIState(overlayConfig);

        for(int i = 0; i < _lightingService.SpawnedLightEntities.Count; i++)
        {
            var lightEntity = _lightingService.SpawnedLightEntities[i];
            if(lightEntity is not null)
            {
                if(lightEntity.TryGetCapability<LightTransformCapability>(out var lightCap))
                {
                    DrawLightContent(lightCap, overlayConfig, uiState);
                    DrawLightGizmo(lightCap, uiState);
                }
            }
        }

        if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out var posing))
        {
            return;
        }

        DrawActorContent(posing, uiState, overlayConfig);
    }

    public override void PostDraw()
    {
        ImGuizmo.SetID(0);
        base.PostDraw();
    }

    private unsafe void DrawLightContent(LightTransformCapability lightCapability, PosingConfiguration config, OverlayUIState uiState)
    {
        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
            return;

        var overlayConfig = _configurationService.Configuration.Posing;
        var light = lightCapability.GameLight;
        var clickables = new List<ClickableItem>();

        if(camera->WorldToScreen(light.Position, out var modelScreen))
        {
            var lightClickable = new ClickableItem
            {
                Name = lightCapability.Entity.FriendlyName,
                ScreenPosition = modelScreen,
                Size = overlayConfig.BoneCircleSize,
                CurrentlySelected = _lightingService.SelectedLightEntity?.GameLight.EntityIndex == light.EntityIndex
            };
            clickables.Add(lightClickable);
        }

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

                ImGui.SetNextFrameWantCaptureMouse(true);

                if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _lightingService.SelectedLightEntity = lightCapability.Entity as LightEntity;

                    clicked.Add(clickable);
                    clickable.WasClicked = true;
                    uiState.AnyClickableClicked = true;
                }
            }

            bool isFilled = clickable.CurrentlySelected || clickable.CurrentlyHovered;

            var color = config.LightCircleHoveredColor;

            if(clickable.CurrentlyHovered)
                color = config.LightCircleHoveredColor;

            if(clickable.CurrentlySelected)
                color = config.LightCircleSelectedColor;

            if(isFilled)
                ImGui.GetWindowDrawList().AddCircleFilled(clickable.ScreenPosition, clickable.Size + 3, color, 8);
            else
                ImGui.GetWindowDrawList().AddCircle(clickable.ScreenPosition, clickable.Size, color, 8, 2);
        }

        if(hovered.Count != 0 && clicked.Count == 0)
        {
            ImGui.SetNextWindowPos(ImGui.GetMousePos() + new Vector2(15, 10), ImGuiCond.Always);
            if(ImGui.Begin("gizmo_light_select_preview", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove))
            {
                foreach(var hover in hovered)
                {
                    ImGui.BeginDisabled();
                    ImGui.Selectable($"{hover.Name}###selectable_{hover.GetHashCode()}", hover.CurrentlySelected);
                    ImGui.EndDisabled();
                }

                ImGui.End();
            }
        }
    }

    private unsafe void DrawActorContent(PosingCapability posing, OverlayUIState uiState, PosingConfiguration overlayConfig)
    {
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
        if(camera == null)
            return;

        // Model Transform
        if(camera->WorldToScreen(posing.ModelPosing.Transform.Position, out var modelScreen))
        {
            var modelTransform = new ClickableItem
            {
                Item = PosingSelectionType.ModelTransform,
                ScreenPosition = modelScreen,
                Size = config.BoneCircleSize,
            };
            clickables.Add(modelTransform);
            modelTransform.CurrentlySelected = posing.Selected.Equals(modelTransform);
        }

        // Bone Transforms
        if(posing.Actor.IsProp == false)
        {
            BonePoseInfoId? selectedBoneId = null;
            if(posing.Selected.Value is BonePoseInfoId boneId)
                selectedBoneId = boneId;

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
                    bool isSelectedBone = selectedBoneId != null && selectedBoneId.Value.Equals(posing.SkeletonPosing.GetBonePose(bone).Id);

                    // Always show the selected bone, even if the overlay filter would hide it
                    if((!_posingService.OverlayFilter.IsBoneValid(bone, poseSlot) || bone.Name == "n_throw") && !isSelectedBone)
                        continue;

                    var boneWorldPosition = Vector3.Transform(bone.LastTransform.Position, modelMatrix);

                    if(camera->WorldToScreen(boneWorldPosition, out var boneScreen))
                    {
                        var clickItem = new ClickableItem
                        {
                            Item = posing.SkeletonPosing.GetBonePose(bone).Id,
                            ScreenPosition = boneScreen,
                            Size = config.BoneCircleSize,
                            CurrentlySelected = isSelectedBone
                        };
                        clickables.Add(clickItem);

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
        using var popup = ImRaii.Popup(_boneSelectPopupName);
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

                if(config.SkeletonLineToCircle)
                {
                    if(Vector2.DistanceSquared(clickable.ParentScreenPosition.Value, clickable.ScreenPosition) >= MathF.Pow(clickable.Size * 2, 2))
                    {
                        ImGui.GetWindowDrawList().AddLine(
                            PointAlongLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, clickable.Size - 1),
                            PointAlongLine(clickable.ScreenPosition, clickable.ParentScreenPosition.Value, clickable.Size - 1),
                            color, thickness
                        );
                    }
                }
                else
                {
                    ImGui.GetWindowDrawList().AddLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, color, thickness);
                }
            }
        }

        static Vector2 PointAlongLine(Vector2 start, Vector2 end, float distance)
            => start + (Vector2.Normalize(end - start) * distance);
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

    private Transform? _lightTrackingTransform;
    private unsafe void DrawLightGizmo(LightTransformCapability lightTransformCapability, OverlayUIState uiState)
    {
        if(!uiState.DrawGizmo || lightTransformCapability.GameLight.IsValid is false || lightTransformCapability.GameLight.IsVisible is false)
            return;

        if(_lightingService.SelectedLightEntity is not null && _lightingService.SelectedLightEntity == lightTransformCapability.Entity)
        {
            // Always draw if this is the selected light
        }
        else if(lightTransformCapability.IsGismoVisible is false)
            return;

        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
            return;

        Matrix4x4 projectionMatrix = camera->GetProjectionMatrix();
        Matrix4x4 worldViewMatrix = camera->GetViewMatrix();
        worldViewMatrix.M44 = 1;

        Transform currentTransform = lightTransformCapability.GameLight.GameLight->Transform;
        Matrix4x4 modelMatrix = worldViewMatrix;

        var lastObserved = _lightTrackingTransform ?? currentTransform;
        var lastMatrix = lastObserved.ToMatrix();

        ImGuizmo.SetID(_gizmoId + lightTransformCapability.GameLight.Index + 1);

        ImGuizmo.BeginFrame();
        var io = ImGui.GetIO();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.AllowAxisFlip(_configurationService.Configuration.Posing.AllowGizmoAxisFlip);
        ImGuizmo.SetDrawlist();
        ImGuizmo.Enable(uiState.GizmoEnabled);

        Transform? newTransform = null;

        if(ImGuizmoExtensions.MouseWheelManipulate(ref lastMatrix))
        {
            newTransform = lastMatrix.ToTransform();
            _lightTrackingTransform = newTransform;
        }

        if(ImGuizmo.Manipulate(
            ref worldViewMatrix,
            ref projectionMatrix,
            _lightingService.Operation.AsGizmoOperation(),
            _lightingService.CoordinateMode.AsGizmoMode(),
            ref lastMatrix
        ))
        {
            newTransform = lastMatrix.ToTransform();
            _lightTrackingTransform = newTransform;
        }

        if(_lightTrackingTransform.HasValue && !ImGuizmo.IsUsing())
        {
            _lightTrackingTransform = null;

            lightTransformCapability.Snapshot();
        }

        ImGuizmo.Enable(true);

        if(newTransform != null)
        {
            var delta = newTransform.Value.CalculateDiff(lastObserved);

            lightTransformCapability.Transform = lightTransformCapability.GameLight.GameLight->Transform += delta;

            lightTransformCapability.rotation = lightTransformCapability.Transform.Rotation.EulerAngles;
            lightTransformCapability.position = lightTransformCapability.Transform.Position;

            if(ImGuizmo.IsUsing() is false)
                lightTransformCapability.Snapshot();
        }

        ImGuizmo.SetID(_gizmoId);
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
        Matrix4x4 worldViewMatrix = camera->GetViewMatrix();
        worldViewMatrix.M44 = 1;

        Transform currentTransform = Transform.Identity;
        Matrix4x4 modelMatrix = worldViewMatrix;

        Game.Posing.Skeletons.Bone? selectedBone = null;

        var shouldDraw = selected.Match(
            boneSelect =>
            {
                var bone = posing.SkeletonPosing.GetBone(boneSelect);
                if(bone == null)
                    return false;

                if(!_posingService.OverlayFilter.IsBoneValid(bone, boneSelect.Slot) && _posingService.GizmoStaysWhenAllBonesAreDisabled is false)
                {
                    return false;
                }

                currentTransform = bone.LastTransform;

                var charaBase = bone.Skeleton.CharacterBase;
                if(charaBase == null)
                    return false;

                selectedBone = bone;
                modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = Vector3.Clamp((Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor, new Vector3(.5f), new Vector3(1.5f))
                }.ToMatrix();

                worldViewMatrix = Matrix4x4.Multiply(modelMatrix, worldViewMatrix);

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

        var lastMatrix = lastObserved.ToMatrix();

        ImGuizmo.BeginFrame();
        var io = ImGui.GetIO();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.AllowAxisFlip(_configurationService.Configuration.Posing.AllowGizmoAxisFlip);
        ImGuizmo.SetDrawlist();
        ImGuizmo.Enable(uiState.GizmoEnabled);

        Transform? newTransform = null;

        if(ImGuizmoExtensions.MouseWheelManipulate(ref lastMatrix))
        {
            if(!posing.ModelPosing.Freeze && !(selectedBone != null && selectedBone.Freeze))
            {
                newTransform = lastMatrix.ToTransform();
                _trackingTransform = newTransform;
            }
        }

        if(ImGuizmo.Manipulate(
            ref worldViewMatrix,
            ref projectionMatrix,
            _posingService.Operation.AsGizmoOperation(),
            _posingService.CoordinateMode.AsGizmoMode(),
            ref lastMatrix
        ))
        {
            if(!posing.ModelPosing.Freeze && !(selectedBone != null && selectedBone.Freeze))
            {
                newTransform = lastMatrix.ToTransform();
                _trackingTransform = newTransform;
            }
        }

        if(_trackingTransform.HasValue && !ImGuizmo.IsUsing())
        {
            if(_groupedPendingSnapshot != null && _groupedPendingSnapshot.Count > 0)
            {
                _groupedUndoService.Snapshot(_groupedPendingSnapshot);
                _groupedPendingSnapshot = null;
            }

            foreach(var eid in _entityManager.SelectedEntityIds)
            {
                if(!_entityManager.TryGetEntity(eid, out var ent))
                    continue;

                if(!ent.TryGetCapability<PosingCapability>(out var cap))
                    continue;

                cap.Snapshot(false, false);
            }

            _trackingTransform = null;
        }

        ImGuizmo.Enable(true);

        if(newTransform != null)
        {
            var delta = newTransform.Value.CalculateDiff(lastObserved);

            selected.Switch(
                bone =>
                {
                    posing.SkeletonPosing.GetBonePose(bone).Apply(newTransform.Value, lastObserved);
                },
                _ =>
                {
                    if(_groupedPendingSnapshot == null && ImGuizmo.IsUsing())
                    {
                        var list = new List<(EntityId, PoseInfo, Transform)>();
                        foreach(var id in _entityManager.SelectedEntityIds)
                        {
                            if(!_entityManager.TryGetEntity(id, out var ent))
                                continue;

                            if(!ent.TryGetCapability<PosingCapability>(out var cap))
                                continue;

                            list.Add((id, cap.SkeletonPosing.PoseInfo.Clone(), cap.ModelPosing.Transform));
                        }
                        _groupedPendingSnapshot = list;
                    }

                    foreach(var id in _entityManager.SelectedEntityIds)
                    {
                        if(!_entityManager.TryGetEntity(id, out var ent))
                            continue;

                        if(!ent.TryGetCapability<PosingCapability>(out var cap))
                            continue;

                        if(cap.ModelPosing.Freeze)
                            continue;

                        cap.ModelPosing.Transform += delta;
                    }
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

        GC.SuppressFinalize(this);
    }

    private class OverlayUIState(PosingConfiguration configuration)
    {
        public bool PopupOpen = ImGui.IsPopupOpen(_boneSelectPopupName);
        public bool UsingGizmo = ImGuizmo.IsUsing();
        public bool HoveringGizmo = ImGuizmo.IsOver();
        public bool AnyActive = ImGui.IsAnyItemActive();
        public bool AnyWindowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
        public bool UserDisablingSkeleton = InputManagerService.ActionKeysPressed(InputAction.Posing_DisableSkeleton);
        public bool UserDisablingGizmo = InputManagerService.ActionKeysPressed(InputAction.Posing_DisableGizmo);
        public bool UserHidingOverlay = InputManagerService.ActionKeysPressed(InputAction.Posing_HideOverlay);


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
        public string Name = string.Empty; // It's just easier this way

        public PosingSelectionType Item = null!;

        public Vector2 ScreenPosition;
        public Vector2? ParentScreenPosition = null;

        public float Size;
        public bool CurrentlySelected;
        public bool CurrentlyHovered;
        public bool WasClicked;
    }
}
