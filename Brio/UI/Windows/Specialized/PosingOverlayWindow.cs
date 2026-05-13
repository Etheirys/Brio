using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Game.World;
using Brio.Input;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Brio.UI.Controls.Editors;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.UI.Windows.Specialized;

public unsafe class PosingOverlayWindow : MediatorWindow
{
    private const int GizmoId = 142857;
    private const string BoneSelectPopupName = "brio_bone_select_popup";

    private readonly EntityManager _entityManager;
    private readonly CameraService _cameraService;
    private readonly ConfigurationService _configurationService;
    private readonly PosingService _posingService;
    private readonly GPoseService _gPoseService;
    private readonly HistoryService _groupedUndoService;
    private readonly LightingService _lightingService;
    private readonly IGameGui _gameGui;

    private readonly PosingTransformEditor _posingTransformEditor = new();

    private readonly List<OverlayItem> _selectingFrom = [with(256)];
    private readonly List<OverlayItem> _clickables = [with(256)];

    private readonly List<int> _clickedIndices = [with(10)];
    private readonly List<int> _hoveredIndices = [with(10)];

    private Transform? _trackingTransform;
    private List<(EntityId id, PosingCapability capability, Transform transform)>? _groupedPendingSnapshot = null;

    public PosingOverlayWindow(EntityManager entityManager, IGameGui gameGui, Mediator mediator, CameraService cameraService, LightingService lightingService, HistoryService groupedUndoService, ConfigurationService configService, PosingService posingService, GPoseService gPoseService)
        : base(mediator, "##brio_posing_overlay_window", ImGuiWindowFlags.AlwaysAutoResize, true)
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
        _gameGui = gameGui;

        mediator.Subscribe<GposeStateChangedMessage>(this, (state) => OnGPoseStateChanged(state.NewState));
    }

    public override void PreDraw()
    {
        base.PreDraw();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0), ImGuiCond.Always);
        SizeCondition = ImGuiCond.Always;

        var io = ImGui.GetIO();
        Size = io.DisplaySize * ImGui.GetFontSize();

        Flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoCollapse;

        ImGuizmo.SetID(GizmoId);

        if(_trackingTransform.HasValue)
        {
            Flags &= ~ImGuiWindowFlags.NoInputs;
        }
    }
    public override void PostDraw()
    {
        ImGuizmo.SetID(0);
        base.PostDraw();
    }

    public override void Draw()
    {
        var overlayConfig = _configurationService.Configuration.Posing;
        var uiState = new OverlayUIState(overlayConfig, _trackingTransform.HasValue);

        _clickables.Clear();

        GetAllOverlayItems(overlayConfig, _clickables);
        HandleInput(uiState, overlayConfig, _clickables);
        DrawPopup();
        DrawSkeletonDotsAndLines(uiState, overlayConfig, _clickables);
        DrawGizmo(uiState);
    }

    // Overlay items
    //

    private void GetAllOverlayItems(PosingConfiguration config, List<OverlayItem> clickables)
    {
        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
            return;

        var cameraViewMatrix = camera->GetViewMatrix();
        var viewportPos = ImGui.GetMainViewport().Pos;

        foreach(var transformableEntity in _entityManager.TransformableEntities)
        {
            var pos = transformableEntity.Transform.Position;
            var render = camera->WorldToScreen(pos, out var screen);

            var overlayItem = new OverlayItem()
            {
                Kind = OverlayItem.OverlayItemKind.Transformable,
                DisplayName = transformableEntity.FriendlyName,
                ScreenPosition = viewportPos + screen,
                Size = config.BoneCircleSize,
                CurrentlySelected = _entityManager.SelectedEntitys.Contains(transformableEntity.Id),
                NormalColor = config.ModelTransformCircleStandOutColor,
                HoveredColor = config.BoneCircleHoveredColor,
                SelectedColor = config.BoneCircleSelectedColor,
                Entity = transformableEntity,
                Transformable = transformableEntity,
            };

            if(transformableEntity is ActorEntity actorEntity)
            {
                if((actorEntity.IsOverlayVisible || _entityManager.SelectedEntitys.Contains(actorEntity.Id)) && actorEntity.TryGetCapability<PosingCapability>(out var posing))
                {
                    BonePoseInfoId? selectedBoneId = null;
                    if(posing.Selected.Value is BonePoseInfoId boneId)
                        selectedBoneId = boneId;

                    foreach(var (skeleton, poseSlot) in posing.SkeletonPosing.Skeletons)
                    {
                        if(!skeleton.IsValid) continue;

                        var charaBase = skeleton.CharacterBase;
                        if(charaBase == null) continue;

                        var modelMatrix = new Transform()
                        {
                            Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                            Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                            Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
                        }.ToMatrix();

                        foreach(var bone in skeleton.Bones)
                        {
                            var boneWorldPosition = Vector3.Transform(bone.LastTransform.Position, modelMatrix);

                            if(Vector3.Transform(boneWorldPosition, cameraViewMatrix).Z >= 0)
                                continue;

                            if(config.UseOverlayOffset && config.BoneOverlayOffsets.Count > 0
                                && config.BoneOverlayOffsets.TryGetValue(bone.Name, out var boneLocalOffset)
                                && boneLocalOffset != Vector3.Zero)
                                boneWorldPosition += Vector3.TransformNormal(
                                    Vector3.Transform(boneLocalOffset, bone.LastTransform.Rotation), modelMatrix);

                            var bonePoseId = posing.SkeletonPosing.GetBonePose(bone).Id;
                            bool isSelected = selectedBoneId != null && selectedBoneId.Value.Equals(bonePoseId);
                            bool isMultiSel = posing.IsBoneSelected(bonePoseId);

                            if(!_posingService.OverlayFilter.IsBoneValid(bone, poseSlot) && !isSelected && !isMultiSel)
                                continue;

                            uint? overrideLineColor = null;
                            if(config.UsePerCategoryLineColors)
                            {
                                var categoryId = _posingService.OverlayFilter.GetFilterCategoryId(bone, poseSlot);
                                if(config.BoneCategoryLineColors.TryGetValue(categoryId, out var catColor))
                                    overrideLineColor = catColor;
                            }

                            if(!camera->WorldToScreen(boneWorldPosition, out var boneScreen))
                                continue;

                            int addedIdx = clickables.Count;

                            clickables.Add(new OverlayItem
                            {
                                Kind = OverlayItem.OverlayItemKind.Bone,
                                DisplayName = bone.FriendlyName,
                                ScreenPosition = viewportPos + boneScreen,
                                Size = config.BoneCircleSize,
                                BoneOrModelItem = bonePoseId,
                                CurrentlySelected = isSelected || isMultiSel,
                                OverrideLineColor = overrideLineColor,
                                Entity = actorEntity,
                                NormalColor = config.BoneCircleNormalColor,
                                HoveredColor = config.BoneCircleHoveredColor,
                                SelectedColor = config.BoneCircleSelectedColor,
                                OnClick = (oi, isMulti) =>
                                {
                                    Brio.Log.Fatal($"Clicked bone overlay item: {oi.DisplayName}, multi: {isMulti}");

                                    if(!isMulti)
                                        ClearAllPosingSelections(oi.Entity);

                                    _entityManager.SetSelectedEntity(oi.Entity?.Id);

                                    posing.SetBoneSelection((BonePoseInfoId)oi.BoneOrModelItem!.Value, isMulti);
                                }
                            });

                            if(bone.Parent == null) continue;
                            if(!_posingService.OverlayFilter.IsBoneValid(bone.Parent, poseSlot)) continue;

                            var parentWorldPos = Vector3.Transform(bone.Parent.LastTransform.Position, modelMatrix);

                            if(config.UseOverlayOffset && config.BoneOverlayOffsets.Count > 0
                                && config.BoneOverlayOffsets.TryGetValue(bone.Parent.Name, out var parentLocalOffset)
                                && parentLocalOffset != Vector3.Zero)
                                parentWorldPos += Vector3.TransformNormal(
                                    Vector3.Transform(parentLocalOffset, bone.Parent.LastTransform.Rotation), modelMatrix);

                            if(camera->WorldToScreen(parentWorldPos, out var parentScreen))
                                CollectionsMarshal.AsSpan(clickables)[addedIdx].ParentScreenPosition = viewportPos + parentScreen;
                        }
                    }
                }
            }

            if(Vector3.Transform(pos, cameraViewMatrix).Z >= 0) continue;
            if(!render) continue;

            overlayItem.OnClick = (oi, isMulti) =>
            {
                if(oi.Entity is not null)
                {
                    if(isMulti)
                    {
                        if(_entityManager.SelectedEntitys.Contains(oi.Entity.Id))
                            _entityManager.RemoveSelectedEntity(oi.Entity.Id);
                        else
                            _entityManager.AddSelectedEntity(oi.Entity.Id);
                    }
                    else
                    {
                        ClearAllPosingSelections();

                        if(oi.Kind is OverlayItem.OverlayItemKind.Light)
                        {
                            _lightingService.SelectedLightEntity = oi.Entity as LightEntity;
                        }

                        _entityManager.SetSelectedEntity(oi.Entity.Id);
                    }
                }
            };

            clickables.Add(overlayItem);
        }

        void ClearAllPosingSelections(Entity? exclusion = null)
        {
            foreach(var entityId in _entityManager.SelectedEntitys)
            {
                if(exclusion is not null && entityId == exclusion.Id)
                    continue;

                if(_entityManager.TryGetEntity(entityId, out var entity)
                    && entity is ActorEntity actor
                    && actor.TryGetCapability<PosingCapability>(out var cap))
                {
                    cap.SelectedBones.Clear();
                    cap.Selected = PosingSelectionType.None;
                }
            }
        }
    }

    private void HandleInput(OverlayUIState uiState, PosingConfiguration config, List<OverlayItem> clickables)
    {
        if(!uiState.SkeletonInputEnabled)
            return;

        _clickedIndices.Clear();
        _hoveredIndices.Clear();

        bool isMultiSelectModifier = ImGui.GetIO().KeyShift;
        var span = CollectionsMarshal.AsSpan(clickables);

        for(int i = 0; i < span.Length; i++)
        {
            ref var clickable = ref span[i];
            var start = clickable.ScreenPosition - new Vector2(clickable.Size);
            var end = clickable.ScreenPosition + new Vector2(clickable.Size);

            if(!ImGui.IsMouseHoveringRect(start, end))
                continue;

            _hoveredIndices.Add(i);
            clickable.CurrentlyHovered = true;
            uiState.AnyClickableHovered = true;

            ImGui.SetNextFrameWantCaptureMouse(true);

            if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _clickedIndices.Add(i);
                clickable.WasClicked = true;
                uiState.AnyClickableClicked = true;
            }
        }

        if(_clickedIndices.Count != 0)
        {
            ref readonly var firstClicked = ref span[_clickedIndices[0]];

            firstClicked.OnClick?.Invoke(firstClicked, isMultiSelectModifier);

            if(_clickedIndices.Count > 1)
            {
                _selectingFrom.Clear();
                foreach(var idx in _clickedIndices)
                    _selectingFrom.Add(span[idx]);
                ImGui.OpenPopup(BoneSelectPopupName);
            }
        }

        if(_hoveredIndices.Count != 0)
        {
            ImGui.SetNextWindowPos(ImGui.GetMousePos() + new Vector2(15, 10), ImGuiCond.Always);
            if(ImGui.Begin("gizmo_bone_select_preview", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove))
            {
                foreach(var idx in _hoveredIndices)
                {
                    ref readonly var hover = ref span[idx];
                    ImGui.BeginDisabled();
                    ImGui.Selectable($"{hover.DisplayName}###selectable_{hover.GetHashCode()}", hover.CurrentlySelected);
                    ImGui.EndDisabled();
                }

                ImGui.End();
            }

            var wheel = ImGui.GetIO().MouseWheel;
            if(wheel != 0)
            {
                ref readonly var first = ref span[_hoveredIndices[0]];

                if(_hoveredIndices.Count == 1)
                {

                }
                else
                {
                    _selectingFrom.Clear();
                    foreach(var idx in _hoveredIndices)
                        _selectingFrom.Add(span[idx]);
                    ImGui.OpenPopup(BoneSelectPopupName);
                }

                first.OnClick?.Invoke(first, isMultiSelectModifier);
            }
        }
    }

    private void DrawPopup()
    {
        using var popup = ImRaii.Popup(BoneSelectPopupName);
        if(!popup.Success)
            return;

        int selectedIndex = -1;
        bool isMultiSelectModifier = ImGui.GetIO().KeyCtrl || ImGui.GetIO().KeyShift;

        for(int i = 0; i < _selectingFrom.Count; i++)
        {
            var click = _selectingFrom[i];

            if(click.CurrentlySelected)
                selectedIndex = i;

            if(ImGui.Selectable($"{click.DisplayName}###clickable_{click.GetHashCode()}", click.CurrentlySelected))
            {
                click.OnClick?.Invoke(click, isMultiSelectModifier);
                _selectingFrom.Clear();
                ImGui.CloseCurrentPopup();
            }
        }

        var wheel = ImGui.GetIO().MouseWheel;
        if(wheel != 0 && _selectingFrom.Count > 0)
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

            if(selectedIndex >= 0 && selectedIndex < _selectingFrom.Count)
            {
                var selected = _selectingFrom[selectedIndex];
                selected.OnClick?.Invoke(selected, isMultiSelectModifier);
            }
        }
    }

    private void DrawSkeletonDotsAndLines(OverlayUIState uiState, PosingConfiguration config, List<OverlayItem> clickables)
    {
        bool drawLines = uiState.DrawSkeletonLines;
        bool drawDots = uiState.DrawSkeletonDots;

        if(!drawLines && !drawDots)
            return;

        var drawList = ImGui.GetWindowDrawList();
        bool linesEnabled = uiState.SkeletonLinesEnabled;
        bool dotsEnabled = uiState.SkeletonDotsEnabled;
        bool lineToCircle = config.SkeletonLineToCircle;
        float lineThickness = config.SkeletonLineThickness;

        var span = CollectionsMarshal.AsSpan(clickables);

        for(int i = 0; i < span.Length; i++)
        {
            ref readonly var clickable = ref span[i];

            if(clickable.Kind == OverlayItem.OverlayItemKind.Transformable && _configurationService.Configuration.Posing.ModelTransformStandout)
            {
                bool filled = clickable.CurrentlySelected || clickable.CurrentlyHovered;

                if(filled)
                    drawList.AddCircleFilled(clickable.ScreenPosition, clickable.Size + 3, config.ModelTransformCircleStandOutColor);
                else
                    drawList.AddCircle(clickable.ScreenPosition, clickable.Size, config.ModelTransformCircleStandOutColor, 10, 3);

                continue;
            }

            var lineColor = linesEnabled
              ? (clickable.OverrideLineColor ?? config.SkeletonLineActiveColor)
              : config.SkeletonLineInactiveColor;

            if(drawLines && clickable.ParentScreenPosition.HasValue)
            {
                if(lineToCircle)
                {
                    float d = clickable.Size * 2f;
                    if(Vector2.DistanceSquared(clickable.ParentScreenPosition.Value, clickable.ScreenPosition) >= d * d)
                    {
                        drawList.AddLine(
                            PointAlongLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, clickable.Size - 1),
                            PointAlongLine(clickable.ScreenPosition, clickable.ParentScreenPosition.Value, clickable.Size - 1),
                            lineColor, lineThickness
                        );
                    }
                }
                else
                {
                    drawList.AddLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, lineColor, lineThickness);
                }
            }

            if(!drawDots)
                continue;

            uint color;
            if(!dotsEnabled)
                color = config.BoneCircleInactiveColor;
            else if(clickable.CurrentlySelected)
                color = config.BoneCircleSelectedColor;
            else if(clickable.CurrentlyHovered)
                color = config.BoneCircleHoveredColor;
            else if(clickable.OverrideLineColor is not null)
                color = lineColor;
            else
                color = config.BoneCircleNormalColor;

            bool isFilled = clickable.CurrentlySelected || clickable.CurrentlyHovered;
            if(isFilled)
                drawList.AddCircleFilled(clickable.ScreenPosition, clickable.Size, color);
            else
                drawList.AddCircle(clickable.ScreenPosition, clickable.Size, color);
        }

        static Vector2 PointAlongLine(Vector2 start, Vector2 end, float distance)
            => start + (Vector2.Normalize(end - start) * distance);
    }

    // Gizmo 
    //

    private void DrawGizmo(OverlayUIState uiState)
    {
        if(!uiState.DrawGizmo)
            return;

        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
            return;

        OverlayItem? primaryItem = null;
        foreach(var item in _clickables)
        {
            if(item.CurrentlySelected)
            {
                primaryItem = item;
                break;
            }
        }

        if(primaryItem == null)
            return;

        PosingCapability? posing = null;
        if(primaryItem.Kind is OverlayItem.OverlayItemKind.Bone
                            or OverlayItem.OverlayItemKind.ModelTransform)
        {
            if(!_entityManager.TryGetCapabilityFromSelectedEntity<PosingCapability>(out posing))
                return;
        }

        var allTransformables = _entityManager.GetAllSelectedTransformables();
        bool isMultiEntity = allTransformables.Count > 1;
       
        Vector3 centroid = isMultiEntity
            ? TransformHelper.GetCentroidForGivenTransforms(allTransformables.Select(x => x.target.Transform))
            : Vector3.Zero;

        Matrix4x4 projectionMatrix = camera->GetProjectionMatrix();
        Matrix4x4 worldViewMatrix = camera->GetViewMatrix();
        worldViewMatrix.M44 = 1;

        Transform currentTransform = Transform.Identity;
        Matrix4x4 modelMatrix = worldViewMatrix;

        Game.Posing.Skeletons.Bone? selectedBone = null;

        bool shouldDraw = primaryItem.Kind switch
        {
            OverlayItem.OverlayItemKind.Bone when posing != null
                => BuildBoneGizmo(posing, primaryItem, isMultiEntity, ref worldViewMatrix, ref currentTransform, ref modelMatrix, out selectedBone),

            OverlayItem.OverlayItemKind.Actor
            or OverlayItem.OverlayItemKind.Light
            or OverlayItem.OverlayItemKind.ModelTransform
            or OverlayItem.OverlayItemKind.Transformable
                => BuildEntityGizmo(primaryItem, isMultiEntity, centroid, ref currentTransform),

            _ => false
        };

        if(!shouldDraw)
            return;

        var primaryTransform = _trackingTransform ?? currentTransform;
        var originalTransform = primaryTransform;

        Vector3 gizmoLocalOffset = Vector3.Zero;
        if(selectedBone != null)
        {
            var offsets = _configurationService.Configuration.Posing.BoneOverlayOffsets;
            if(_configurationService.Configuration.Posing.UseOverlayOffset
                && offsets.Count > 0
                && offsets.TryGetValue(selectedBone.Name, out var boneOffset)
                && boneOffset != Vector3.Zero)
            {
                gizmoLocalOffset = Vector3.Transform(boneOffset, primaryTransform.Rotation);
            }
        }

        var displayTransform = primaryTransform;
        if(gizmoLocalOffset != Vector3.Zero)
            displayTransform.Position += gizmoLocalOffset;

        var lastMatrix = displayTransform.ToMatrix();

        Vector3 gizmoWorldCenter = selectedBone != null
            ? Vector3.Transform(displayTransform.Position, modelMatrix)
            : isMultiEntity
                ? centroid
                : primaryTransform.Position;

        //if(camera->WorldToScreen(gizmoWorldCenter, out var gizmoCenterScreen))
            //DrawGizmoImage(ImGui.GetMainViewport().Pos + gizmoCenterScreen);

        ImGuizmo.BeginFrame();
        var io = ImGui.GetIO();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.AllowAxisFlip(_configurationService.Configuration.Posing.AllowGizmoAxisFlip);
        ImGuizmo.SetDrawlist();
        ImGuizmo.Enable(uiState.GizmoEnabled);

        Transform? newTransform = null;

        if(ImGuizmoExtensions.MouseWheelManipulate(ref lastMatrix) && !uiState.PopupOpen)
        {
            TryGetNewTransform(ref lastMatrix);
        }

        if(ImGuizmo.Manipulate(ref worldViewMatrix, ref projectionMatrix, _posingService.Operation.AsGizmoOperation(), _posingService.CoordinateMode.AsGizmoMode(), ref lastMatrix))
        {
            TryGetNewTransform(ref lastMatrix);
        }

        if(_trackingTransform.HasValue && (!ImGuizmo.IsUsing() || !ImGui.IsMouseDown(ImGuiMouseButton.Left)))
        {
            if(_groupedPendingSnapshot?.Count > 0)
            {
                _groupedUndoService.Snapshot(_groupedPendingSnapshot);
                _groupedPendingSnapshot = null;
            }

            TransformHelper.SnapshotAll(_entityManager.GetAllSelectedTransformables().Select(x => x.target));
            _trackingTransform = null;
        }

        ImGuizmo.Enable(true);

        if(newTransform == null)
            return;

        var delta = newTransform.Value.CalculateDiff(originalTransform);

        if(isMultiEntity && primaryItem.Kind is not OverlayItem.OverlayItemKind.Bone)
        {
            if(_groupedPendingSnapshot == null && ImGuizmo.IsUsing())
                _groupedPendingSnapshot = _entityManager.GetAllSelectedActors();

            TransformHelper.ApplyDeltaToMultiple(allTransformables, delta, centroid, true);

            if(delta.Position != Vector3.Zero || delta.Rotation != Quaternion.Identity)
            {
                centroid = TransformHelper.GetCentroidForGivenTransforms(allTransformables.Select(x => x.target.Transform));
            }
        }
        else if(primaryItem.Kind == OverlayItem.OverlayItemKind.Bone && posing != null)
        {
            if(posing.IsMultiSelecting)
            {
                foreach(var boneId in posing.SelectedBones)
                {
                    var targetBone = posing.SkeletonPosing.GetBone(boneId);
                    if(targetBone != null && !targetBone.Freeze)
                    {
                        var boneTransform = targetBone.LastTransform;
                        posing.SkeletonPosing.GetBonePose(boneId).Apply(boneTransform + delta, boneTransform);
                    }
                }
            }
            else if(primaryItem.BoneOrModelItem?.Value is BonePoseInfoId bonePoseId)
            {
                posing.SkeletonPosing.GetBonePose(bonePoseId).Apply(newTransform.Value, originalTransform);
            }
        }
        else
        {
            if(_groupedPendingSnapshot == null && ImGuizmo.IsUsing())
                _groupedPendingSnapshot = _entityManager.GetAllSelectedActors();

            foreach(var (_, target, _) in _entityManager.GetAllSelectedTransformables())
                TransformHelper.ApplyDelta(target, delta);
        }

        bool TryGetNewTransform(ref Matrix4x4 matrix)
        {
            bool canEdit = primaryItem.Kind is OverlayItem.OverlayItemKind.Bone
                ? posing != null && !posing.ModelPosing.IsTransformFrozen && !(selectedBone != null && selectedBone.Freeze)
                : isMultiEntity
                    ? allTransformables.Any(x => !x.target.IsTransformFrozen)
                    : primaryItem.Transformable is { IsTransformFrozen: false };

            if(!canEdit)
                return false;

            var transform = matrix.ToTransform();
            if(gizmoLocalOffset != Vector3.Zero)
                transform.Position -= gizmoLocalOffset;

            newTransform = transform;
            _trackingTransform = newTransform;

            return true;
        }
    }

    private bool BuildBoneGizmo(PosingCapability posing, OverlayItem item, bool isMultiEntity, ref Matrix4x4 worldViewMatrix, ref Transform currentTransform, ref Matrix4x4 modelMatrix, out Game.Posing.Skeletons.Bone? selectedBone)
    {
        selectedBone = null;

        if(item.BoneOrModelItem?.Value is not BonePoseInfoId boneSelect)
            return false;

        var bone = posing.SkeletonPosing.GetBone(boneSelect);
        if(bone is null)
            return false;

        if(!_posingService.OverlayFilter.IsBoneValid(bone, boneSelect.Slot) && !_posingService.GizmoStaysWhenAllBonesAreDisabled)
            return false;

        var charaBase = bone.Skeleton.CharacterBase;
        if(charaBase == null)
            return false;

        selectedBone = bone;
        currentTransform = bone.LastTransform;
        modelMatrix = new Transform()
        {
            Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
            Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
            Scale = Vector3.Clamp((Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor,
                new Vector3(0.5f), new Vector3(1.5f))
        }.ToMatrix();

        worldViewMatrix = Matrix4x4.Multiply(modelMatrix, worldViewMatrix);

        return true;
    }
    private bool BuildEntityGizmo(OverlayItem item, bool isMultiEntity, Vector3 multiCentroid, ref Transform currentTransform)
    {
        if(isMultiEntity)
        {
            currentTransform = new Transform { Position = multiCentroid, Rotation = Quaternion.Identity, Scale = Vector3.One };

            return true;
        }

        currentTransform = item.Transformable?.Transform ?? new Transform();

        return true;
    }

    // Other
    //

    private void OnGPoseStateChanged(bool newState)
    {
        if(newState)
        {
            IsOpen = _configurationService.Configuration.Posing.OverlayDefaultsOn;
        }
        else
        {
            IsOpen = false;
            _trackingTransform = null;
            _groupedPendingSnapshot = null;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        GC.SuppressFinalize(this);
    }

    // UI State
    //

    private class OverlayUIState(PosingConfiguration configuration, bool isTrackingGizmo = false)
    {
        public bool PopupOpen = ImGui.IsPopupOpen(BoneSelectPopupName);
        public bool UsingGizmo = ImGuizmo.IsUsing() && isTrackingGizmo;
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

    public class OverlayItem
    {
        public enum OverlayItemKind
        {
            Actor, Light, Bone, ModelTransform, Transformable,

            IsEntity = Actor | Light | Transformable | ModelTransform,
        }

        public OverlayItemKind Kind;
        public string DisplayName = string.Empty;

        public Vector2 ScreenPosition;
        public Vector2? ParentScreenPosition;

        public float Size;
        public bool CurrentlySelected;
        public bool CurrentlyHovered;
        public bool WasClicked;

        public uint NormalColor;
        public uint HoveredColor;
        public uint SelectedColor;
        public uint? OverrideLineColor;

        public ITransformable? Transformable;

        public PosingSelectionType? BoneOrModelItem;
        public Entity? Entity;
        public Action<OverlayItem, bool>? OnClick;
    }

    // Gizmo Image
    //

    //public IDalamudTextureWrap? GizmoImage { get; set; }
    //public float GizmoImageSize { get; set; } = 130f;

    //private void DrawGizmoImage(Vector2 screenCenter)
    //{
    //    return;

    //    if(GizmoImage == null || GizmoImage.Handle == 0)
    //        return;

    //    var half = new Vector2(GizmoImageSize * ImGuiHelpers.GlobalScale * 0.5f);
    //    ImGui.GetForegroundDrawList().AddImageRounded(
    //        GizmoImage.Handle,
    //        screenCenter - half,
    //        screenCenter + half,
    //        Vector2.Zero,
    //        Vector2.One,
    //        ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)),
    //        half.X);
    //}
}
