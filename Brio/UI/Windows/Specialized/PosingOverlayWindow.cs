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
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

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

    private readonly Stopwatch _debugTraceStopwatch = Stopwatch.StartNew();
    private long _lastDebugTraceTime = 0;

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
                ScreenPosition = ImGui.GetMainViewport().Pos + modelScreen,
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
        var currentTime = _debugTraceStopwatch.ElapsedMilliseconds;
        var shouldTrace = (currentTime - _lastDebugTraceTime) >= 1000;

        long startTime = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        StringBuilder? traceLog = shouldTrace ? new StringBuilder() : null;

        if(shouldTrace)
        {
            traceLog!.AppendLine($" --- START [DrawActorContent] Selected: {posing.Selected.Value.GetType().Name}, " +
                          $"UIState - PopupOpen: {uiState.PopupOpen}, UsingGizmo: {uiState.UsingGizmo}, " +
                          $"GizmoEnabled: {uiState.GizmoEnabled}, SkeletonInputEnabled: {uiState.SkeletonInputEnabled} START --- ");
        }

        var clickables = new List<ClickableItem>();

        long calcClickablesStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        CalculateClickables(posing, uiState, overlayConfig, ref clickables, shouldTrace, traceLog);
        long calcClickablesEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(shouldTrace)
        {
            var deltaMs = (calcClickablesEnd - calcClickablesStart) / (double)Stopwatch.Frequency * 1000.0;
            traceLog!.AppendLine($"[DrawActorContent] Clickables calculated: {clickables.Count}, DeltaTime: {deltaMs:F3}ms");
        }

        long handleInputStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        HandleSkeletonInput(posing, uiState, clickables, shouldTrace, traceLog);
        long handleInputEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        long drawPopupStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        DrawPopup(posing, shouldTrace, traceLog);
        long drawPopupEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        long drawLinesStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        DrawSkeletonLines(uiState, overlayConfig, clickables, shouldTrace, traceLog);
        long drawLinesEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        long drawDotsStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        DrawSkeletonDots(uiState, overlayConfig, clickables, shouldTrace, traceLog);
        long drawDotsEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        long drawGizmoStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        DrawGizmo(posing, uiState, shouldTrace, traceLog);
        long drawGizmoEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(shouldTrace)
        {
            var handleInputDelta = (handleInputEnd - handleInputStart) / (double)Stopwatch.Frequency * 1000.0;
            var drawPopupDelta = (drawPopupEnd - drawPopupStart) / (double)Stopwatch.Frequency * 1000.0;
            var drawLinesDelta = (drawLinesEnd - drawLinesStart) / (double)Stopwatch.Frequency * 1000.0;
            var drawDotsDelta = (drawDotsEnd - drawDotsStart) / (double)Stopwatch.Frequency * 1000.0;
            var drawGizmoDelta = (drawGizmoEnd - drawGizmoStart) / (double)Stopwatch.Frequency * 1000.0;
            var totalDelta = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;

            traceLog!.AppendLine($"[DrawActorContent] Method Times - HandleInput: {handleInputDelta:F3}ms, Popup: {drawPopupDelta:F3}ms, " +
                          $"Lines: {drawLinesDelta:F3}ms, Dots: {drawDotsDelta:F3}ms, Gizmo: {drawGizmoDelta:F3}ms");
            traceLog.AppendLine($"--- END [DrawActorContent] TrackingTransform: {(_trackingTransform.HasValue ? "Active" : "None")}, TotalDeltaTime: {totalDelta:F3}ms END ---");

            Brio.Log.Debug(traceLog.ToString());
            _lastDebugTraceTime = currentTime;
        }
    }

    private unsafe void CalculateClickables(PosingCapability posing, OverlayUIState uiState, PosingConfiguration config, ref List<ClickableItem> clickables, bool shouldTrace, StringBuilder? traceLog)
    {
        long startTime = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(shouldTrace)
        {
            traceLog!.AppendLine($"[CalculateClickables] START - Actor.IsProp: {posing.Actor.IsProp}");
        }

        long cameraGetStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        var camera = _cameraService.GetCurrentCamera();
        long cameraGetEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(camera == null)
        {
            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[CalculateClickables] Camera is null, returning early, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        // Model Transform
        long modelTransformStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
        if(camera->WorldToScreen(posing.ModelPosing.Transform.Position, out var modelScreen))
        {
            var modelTransform = new ClickableItem
            {
                Item = PosingSelectionType.ModelTransform,
                ScreenPosition = ImGui.GetMainViewport().Pos + modelScreen,
                Size = config.BoneCircleSize,
            };
            clickables.Add(modelTransform);
            modelTransform.CurrentlySelected = posing.Selected.Equals(modelTransform);

            if(shouldTrace)
            {
                traceLog!.AppendLine($"[CalculateClickables] Added model transform clickable");
            }
        }
        long modelTransformEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        // Bone Transforms
        if(posing.Actor.IsProp == false)
        {
            BonePoseInfoId? selectedBoneId = null;
            if(posing.Selected.Value is BonePoseInfoId boneId)
                selectedBoneId = boneId;

            int skeletonCount = 0;
            int totalBoneCount = 0;
            int validBoneCount = 0;
            int worldToScreenCalls = 0;
            int worldToScreenSuccesses = 0;

            long boneProcessStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
            long skeletonIterationTime = 0;
            long matrixCreationTime = 0;
            long boneIterationTime = 0;
            long worldToScreenTime = 0;
            long clickableCreationTime = 0;
            long parentProcessingTime = 0;

            foreach(var (skeleton, poseSlot) in posing.SkeletonPosing.Skeletons)
            {
                long skeletonIterStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

                skeletonCount++;

                if(!skeleton.IsValid)
                {
                    if(shouldTrace)
                        skeletonIterationTime += _debugTraceStopwatch.ElapsedTicks - skeletonIterStart;
                    continue;
                }

                var charaBase = skeleton.CharacterBase;
                if(charaBase == null)
                {
                    if(shouldTrace)
                        skeletonIterationTime += _debugTraceStopwatch.ElapsedTicks - skeletonIterStart;
                    continue;
                }

                long matrixStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                var modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = (Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor
                }.ToMatrix();
                long matrixEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                if(shouldTrace)
                    matrixCreationTime += matrixEnd - matrixStart;

                long boneIterStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

                foreach(var bone in skeleton.Bones)
                {
                    totalBoneCount++;

                    bool isSelectedBone = selectedBoneId != null && selectedBoneId.Value.Equals(posing.SkeletonPosing.GetBonePose(bone).Id);

                    // Always show the selected bone, even if the overlay filter would hide it
                    //if((!_posingService.OverlayFilter.IsBoneValid(bone, poseSlot) || bone.Name == "n_throw") && !isSelectedBone)
                    //    continue;

                    long worldToScreenStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                    var boneWorldPosition = Vector3.Transform(bone.LastTransform.Position, modelMatrix);
                    worldToScreenCalls++;

                    if(camera->WorldToScreen(boneWorldPosition, out var boneScreen))
                    {
                        long worldToScreenEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                        if(shouldTrace)
                            worldToScreenTime += worldToScreenEnd - worldToScreenStart;

                        worldToScreenSuccesses++;

                        long clickableStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                        var clickItem = new ClickableItem
                        {
                            Item = posing.SkeletonPosing.GetBonePose(bone).Id,
                            ScreenPosition = boneScreen,
                            Size = config.BoneCircleSize,
                            CurrentlySelected = isSelectedBone
                        };
                        clickables.Add(clickItem);
                        validBoneCount++;
                        long clickableEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                        if(shouldTrace)
                            clickableCreationTime += clickableEnd - clickableStart;

                        if(bone.Parent != null)
                        {
                            long parentStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

                            //if(!_posingService.OverlayFilter.IsBoneValid(bone.Parent, poseSlot))
                            //{
                            //    if(shouldTrace)
                            //        parentProcessingTime += _debugTraceStopwatch.ElapsedTicks - parentStart;
                            //    continue;
                            //}

                            var parentWorldPosition = Vector3.Transform(bone.Parent.LastTransform.Position, modelMatrix);
                            worldToScreenCalls++;
                            if(camera->WorldToScreen(parentWorldPosition, out var parentScreen))
                            {
                                worldToScreenSuccesses++;
                                clickables.Last().ParentScreenPosition = ImGui.GetMainViewport().Pos + parentScreen;
                            }

                            long parentEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                            if(shouldTrace)
                                parentProcessingTime += parentEnd - parentStart;
                        }
                    }
                    else
                    {
                        long worldToScreenEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                        if(shouldTrace)
                            worldToScreenTime += worldToScreenEnd - worldToScreenStart;
                    }
                }

                long boneIterEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;
                if(shouldTrace)
                {
                    boneIterationTime += boneIterEnd - boneIterStart;
                    skeletonIterationTime += _debugTraceStopwatch.ElapsedTicks - skeletonIterStart;
                }
            }

            long boneProcessEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

            if(shouldTrace)
            {
                var boneProcessDelta = (boneProcessEnd - boneProcessStart) / (double)Stopwatch.Frequency * 1000.0;
                var skeletonIterDelta = skeletonIterationTime / (double)Stopwatch.Frequency * 1000.0;
                var matrixCreationDelta = matrixCreationTime / (double)Stopwatch.Frequency * 1000.0;
                var boneIterDelta = boneIterationTime / (double)Stopwatch.Frequency * 1000.0;
                var worldToScreenDelta = worldToScreenTime / (double)Stopwatch.Frequency * 1000.0;
                var clickableCreationDelta = clickableCreationTime / (double)Stopwatch.Frequency * 1000.0;
                var parentProcessingDelta = parentProcessingTime / (double)Stopwatch.Frequency * 1000.0;

                traceLog!.AppendLine($"[CalculateClickables] Processed {skeletonCount} skeletons, {totalBoneCount} total bones, {validBoneCount} visible bones");
                traceLog.AppendLine($"[CalculateClickables] WorldToScreen: {worldToScreenCalls} calls, {worldToScreenSuccesses} successes ({(worldToScreenCalls > 0 ? (worldToScreenSuccesses * 100.0 / worldToScreenCalls) : 0):F1}%)");
                traceLog.AppendLine($"[CalculateClickables] Bone Timing Breakdown:");
                traceLog.AppendLine($"  - Skeleton Iteration: {skeletonIterDelta:F3}ms");
                traceLog.AppendLine($"  - Matrix Creation: {matrixCreationDelta:F3}ms ({(boneProcessDelta > 0 ? (matrixCreationDelta / boneProcessDelta * 100) : 0):F1}%)");
                traceLog.AppendLine($"  - Bone Iteration: {boneIterDelta:F3}ms ({(boneProcessDelta > 0 ? (boneIterDelta / boneProcessDelta * 100) : 0):F1}%)");
                traceLog.AppendLine($"  - WorldToScreen: {worldToScreenDelta:F3}ms ({(boneProcessDelta > 0 ? (worldToScreenDelta / boneProcessDelta * 100) : 0):F1}%)");
                traceLog.AppendLine($"  - Clickable Creation: {clickableCreationDelta:F3}ms ({(boneProcessDelta > 0 ? (clickableCreationDelta / boneProcessDelta * 100) : 0):F1}%)");
                traceLog.AppendLine($"  - Parent Processing: {parentProcessingDelta:F3}ms ({(boneProcessDelta > 0 ? (parentProcessingDelta / boneProcessDelta * 100) : 0):F1}%)");
                traceLog.AppendLine($"  - Total Bone Process: {boneProcessDelta:F3}ms");
            }
        }

        if(shouldTrace)
        {
            var cameraGetDelta = (cameraGetEnd - cameraGetStart) / (double)Stopwatch.Frequency * 1000.0;
            var modelTransformDelta = (modelTransformEnd - modelTransformStart) / (double)Stopwatch.Frequency * 1000.0;
            var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;

            traceLog!.AppendLine($"[CalculateClickables] Overall Timing:");
            traceLog.AppendLine($"  - Camera Get: {cameraGetDelta:F3}ms");
            traceLog.AppendLine($"  - Model Transform: {modelTransformDelta:F3}ms");
            traceLog.AppendLine($"[CalculateClickables] END - Total clickables: {clickables.Count}, TotalDeltaTime: {deltaMs:F3}ms");
        }
    }

    private void HandleSkeletonInput(PosingCapability posing, OverlayUIState uiState, List<ClickableItem> clickables, bool shouldTrace, StringBuilder? traceLog)
    {
        long startTime = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(!uiState.SkeletonInputEnabled)
        {
            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[HandleSkeletonInput] Skeleton input disabled, skipping, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        if(shouldTrace)
        {
            traceLog!.AppendLine($"[HandleSkeletonInput] START - Processing {clickables.Count} clickables");
        }

        var clicked = new List<ClickableItem>();
        var hovered = new List<ClickableItem>();

        long hitTestStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

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

        long hitTestEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(clicked.Count != 0)
        {
            if(shouldTrace)
            {
                traceLog!.AppendLine($"[HandleSkeletonInput] {clicked.Count} clickables clicked, selecting: {clicked[0].Item.DisplayName}");
            }

            posing.Selected = clicked[0].Item;

            if(clicked.Count > 1)
            {
                _selectingFrom = clicked;
                ImGui.OpenPopup(_boneSelectPopupName);

                if(shouldTrace)
                {
                    traceLog!.AppendLine($"[HandleSkeletonInput] Multiple clicks detected, opening selection popup");
                }
            }
        }

        if(hovered.Count != 0 && clicked.Count == 0)
        {
            if(shouldTrace)
            {
                traceLog!.AppendLine($"[HandleSkeletonInput] {hovered.Count} clickables hovered");
            }

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
                if(shouldTrace)
                {
                    traceLog!.AppendLine($"[HandleSkeletonInput] Mouse wheel detected: {wheel}");
                }

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

        if(shouldTrace)
        {
            var hitTestDelta = (hitTestEnd - hitTestStart) / (double)Stopwatch.Frequency * 1000.0;
            var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
            traceLog!.AppendLine($"[HandleSkeletonInput] Timing Breakdown:");
            traceLog.AppendLine($"  - Hit Test: {hitTestDelta:F3}ms ({(deltaMs > 0 ? (hitTestDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"[HandleSkeletonInput] END - Hovered: {hovered.Count}, Clicked: {clicked.Count}, TotalDeltaTime: {deltaMs:F3}ms");
        }
    }

    private void DrawPopup(PosingCapability posing, bool shouldTrace, StringBuilder? traceLog)
    {
        long startTime = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        using var popup = ImRaii.Popup(_boneSelectPopupName);
        if(popup.Success)
        {
            if(shouldTrace)
            {
                traceLog!.AppendLine($"[DrawPopup] Popup is open with {_selectingFrom.Count} items");
            }

            int selectedIndex = -1;
            foreach(var click in _selectingFrom)
            {
                bool isSelected = posing.Selected == click.Item;
                if(isSelected)
                    selectedIndex = _selectingFrom.IndexOf(click);

                if(ImGui.Selectable($"{click.Item.DisplayName}###clickable_{click.GetHashCode()}", isSelected))
                {
                    if(shouldTrace)
                    {
                        traceLog!.AppendLine($"[DrawPopup] Item selected: {click.Item.DisplayName}");
                    }

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

                if(shouldTrace)
                {
                    traceLog!.AppendLine($"[DrawPopup] Wheel scroll in popup, new index: {selectedIndex}");
                }

                posing.Selected = _selectingFrom[selectedIndex].Item;
            }

            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[DrawPopup] END - DeltaTime: {deltaMs:F3}ms");
            }
        }
        else if(shouldTrace)
        {
            var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
            traceLog!.AppendLine($"[DrawPopup] Popup not open, DeltaTime: {deltaMs:F3}ms");
        }
    }

    private static void DrawSkeletonLines(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables, bool shouldTrace, StringBuilder? traceLog)
    {
        long startTime = shouldTrace ? Stopwatch.GetTimestamp() : 0;

        if(!uiState.DrawSkeletonLines)
        {
            if(shouldTrace)
            {
                var deltaMs = (Stopwatch.GetTimestamp() - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[DrawSkeletonLines] Skeleton lines disabled, skipping, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        if(shouldTrace)
        {
            traceLog!.AppendLine($"[DrawSkeletonLines] START - Drawing lines for {clickables.Count} clickables, Enabled: {uiState.SkeletonLinesEnabled}");
        }

        int linesDrawn = 0;
        int linesSkipped = 0;
        long drawStart = shouldTrace ? Stopwatch.GetTimestamp() : 0;

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
                        linesDrawn++;
                    }
                    else
                    {
                        linesSkipped++;
                    }
                }
                else
                {
                    ImGui.GetWindowDrawList().AddLine(clickable.ParentScreenPosition.Value, clickable.ScreenPosition, color, thickness);
                    linesDrawn++;
                }
            }
        }

        if(shouldTrace)
        {
            var drawDelta = (Stopwatch.GetTimestamp() - drawStart) / (double)Stopwatch.Frequency * 1000.0;
            var deltaMs = (Stopwatch.GetTimestamp() - startTime) / (double)Stopwatch.Frequency * 1000.0;
            traceLog!.AppendLine($"[DrawSkeletonLines] Timing Breakdown:");
            traceLog.AppendLine($"  - Draw: {drawDelta:F3}ms ({(deltaMs > 0 ? (drawDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"[DrawSkeletonLines] END - Lines Drawn: {linesDrawn}, Skipped: {linesSkipped}, TotalDeltaTime: {deltaMs:F3}ms");
        }

        static Vector2 PointAlongLine(Vector2 start, Vector2 end, float distance)
            => start + (Vector2.Normalize(end - start) * distance);
    }

    private void DrawSkeletonDots(OverlayUIState uiState, PosingConfiguration config, List<ClickableItem> clickables, bool shouldTrace, StringBuilder? traceLog)
    {
        long startTime = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(!uiState.DrawSkeletonDots)
        {
            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[DrawSkeletonDots] Skeleton dots disabled, skipping, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        if(shouldTrace)
        {
            traceLog!.AppendLine($"[DrawSkeletonDots] START - Drawing dots for {clickables.Count} clickables, Enabled: {uiState.SkeletonDotsEnabled}");
        }

        int dotsDrawn = 0;
        int selectedCount = 0;
        int hoveredCount = 0;
        int filledCount = 0;
        int outlineCount = 0;

        long drawStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        foreach(var clickable in clickables)
        {
            bool isFilled = clickable.CurrentlySelected || clickable.CurrentlyHovered;

            if(clickable.CurrentlySelected) selectedCount++;
            if(clickable.CurrentlyHovered) hoveredCount++;

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
                dotsDrawn++;
                filledCount++;
                continue;
            }

            if(isFilled)
            {
                ImGui.GetWindowDrawList().AddCircleFilled(clickable.ScreenPosition, clickable.Size, color);
                filledCount++;
            }
            else
            {
                ImGui.GetWindowDrawList().AddCircle(clickable.ScreenPosition, clickable.Size, color);
                outlineCount++;
            }

            dotsDrawn++;
        }

        if(shouldTrace)
        {
            var drawDelta = (_debugTraceStopwatch.ElapsedTicks - drawStart) / (double)Stopwatch.Frequency * 1000.0;
            var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
            traceLog!.AppendLine($"[DrawSkeletonDots] Timing Breakdown:");
            traceLog.AppendLine($"  - Draw: {drawDelta:F3}ms ({(deltaMs > 0 ? (drawDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"[DrawSkeletonDots] END - Total: {dotsDrawn}, Filled: {filledCount}, Outline: {outlineCount}, Selected: {selectedCount}, Hovered: {hoveredCount}, TotalDeltaTime: {deltaMs:F3}ms");
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

    private unsafe void DrawGizmo(PosingCapability posing, OverlayUIState uiState, bool shouldTrace, StringBuilder? traceLog)
    {
        long startTime = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(!uiState.DrawGizmo)
        {
            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[DrawGizmo] Gizmo drawing disabled, skipping, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        if(posing.Selected.Value is None)
        {
            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[DrawGizmo] No selection (None), skipping, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        if(shouldTrace)
        {
            traceLog!.AppendLine($"[DrawGizmo] START - GizmoEnabled: {uiState.GizmoEnabled}, Operation: {_posingService.Operation}, Mode: {_posingService.CoordinateMode}");
        }

        var camera = _cameraService.GetCurrentCamera();
        if(camera == null)
        {
            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[DrawGizmo] Camera is null, returning early, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        var selected = posing.Selected;

        Matrix4x4 projectionMatrix = camera->GetProjectionMatrix();
        Matrix4x4 worldViewMatrix = camera->GetViewMatrix();
        worldViewMatrix.M44 = 1;

        Transform currentTransform = Transform.Identity;
        Matrix4x4 modelMatrix = worldViewMatrix;

        Game.Posing.Skeletons.Bone? selectedBone = null;
        string selectionType = "Unknown";

        long matchStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        var shouldDraw = selected.Match(
            boneSelect =>
            {
                selectionType = "Bone";
                var bone = posing.SkeletonPosing.GetBone(boneSelect);
                if(bone == null)
                {
                    if(shouldTrace)
                    {
                        traceLog!.AppendLine($"[DrawGizmo] Bone is null");
                    }
                    return false;
                }

                if(!_posingService.OverlayFilter.IsBoneValid(bone, boneSelect.Slot) && _posingService.GizmoStaysWhenAllBonesAreDisabled is false)
                {
                    if(shouldTrace)
                    {
                        traceLog!.AppendLine($"[DrawGizmo] Bone '{bone.Name}' is not valid and gizmo should not stay");
                    }
                    return false;
                }

                currentTransform = bone.LastTransform;

                var charaBase = bone.Skeleton.CharacterBase;
                if(charaBase == null)
                {
                    if(shouldTrace)
                    {
                        traceLog!.AppendLine($"[DrawGizmo] CharacterBase is null");
                    }
                    return false;
                }

                selectedBone = bone;
                modelMatrix = new Transform()
                {
                    Position = (Vector3)charaBase->CharacterBase.DrawObject.Object.Position,
                    Rotation = (Quaternion)charaBase->CharacterBase.DrawObject.Object.Rotation,
                    Scale = Vector3.Clamp((Vector3)charaBase->CharacterBase.DrawObject.Object.Scale * charaBase->ScaleFactor, new Vector3(.5f), new Vector3(1.5f))
                }.ToMatrix();

                worldViewMatrix = Matrix4x4.Multiply(modelMatrix, worldViewMatrix);

                if(shouldTrace)
                {
                    traceLog!.AppendLine($"[DrawGizmo] Bone selected: {bone.Name}, Freeze: {bone.Freeze}");
                }

                return true;
            },
            _ =>
            {
                selectionType = "ModelTransform";
                currentTransform = posing.ModelPosing.Transform;

                if(shouldTrace)
                {
                    traceLog!.AppendLine($"[DrawGizmo] Model transform selected, Freeze: {posing.ModelPosing.Freeze}");
                }

                return true;
            },
            _ =>
            {
                selectionType = "Other";
                return false;
            }
        );

        long matchEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(!shouldDraw)
        {
            if(shouldTrace)
            {
                var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;
                traceLog!.AppendLine($"[DrawGizmo] Should not draw, returning early, DeltaTime: {deltaMs:F3}ms");
            }
            return;
        }

        var lastObserved = _trackingTransform ?? currentTransform;

        var lastMatrix = lastObserved.ToMatrix();

        long gizmoSetupStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        ImGuizmo.BeginFrame();
        var io = ImGui.GetIO();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.AllowAxisFlip(_configurationService.Configuration.Posing.AllowGizmoAxisFlip);
        ImGuizmo.SetDrawlist();
        ImGuizmo.Enable(uiState.GizmoEnabled);

        long gizmoSetupEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        Transform? newTransform = null;
        bool wasMouseWheelManipulated = false;
        bool wasGizmoManipulated = false;

        long manipulateStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(ImGuizmoExtensions.MouseWheelManipulate(ref lastMatrix))
        {
            if(!posing.ModelPosing.Freeze && !(selectedBone != null && selectedBone.Freeze))
            {
                newTransform = lastMatrix.ToTransform();
                _trackingTransform = newTransform;
                wasMouseWheelManipulated = true;
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
                wasGizmoManipulated = true;
            }
        }

        long manipulateEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        long snapshotTime = 0;
        if(_trackingTransform.HasValue && !ImGuizmo.IsUsing())
        {
            if(shouldTrace)
            {
                traceLog!.AppendLine($"[DrawGizmo] Transform tracking ended, saving snapshot");
            }

            long snapshotStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

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

            if(shouldTrace)
            {
                snapshotTime = _debugTraceStopwatch.ElapsedTicks - snapshotStart;
            }
        }

        ImGuizmo.Enable(true);

        long applyStart = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(newTransform != null)
        {
            var delta = newTransform.Value.CalculateDiff(lastObserved);

            if(shouldTrace)
            {
                traceLog!.AppendLine($"[DrawGizmo] Transform changed - MouseWheel: {wasMouseWheelManipulated}, Gizmo: {wasGizmoManipulated}, SelectionType: {selectionType}");
            }

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

                        if(shouldTrace)
                        {
                            traceLog!.AppendLine($"[DrawGizmo] Created grouped snapshot with {list.Count} entities");
                        }
                    }

                    int transformedCount = 0;
                    foreach(var id in _entityManager.SelectedEntityIds)
                    {
                        if(!_entityManager.TryGetEntity(id, out var ent))
                            continue;

                        if(!ent.TryGetCapability<PosingCapability>(out var cap))
                            continue;

                        if(cap.ModelPosing.Freeze)
                            continue;

                        cap.ModelPosing.Transform += delta;
                        transformedCount++;
                    }

                    if(shouldTrace)
                    {
                        traceLog!.AppendLine($"[DrawGizmo] Applied transform to {transformedCount} entities");
                    }
                },
                _ => { }
            );
        }

        long applyEnd = shouldTrace ? _debugTraceStopwatch.ElapsedTicks : 0;

        if(shouldTrace)
        {
            var matchDelta = (matchEnd - matchStart) / (double)Stopwatch.Frequency * 1000.0;
            var gizmoSetupDelta = (gizmoSetupEnd - gizmoSetupStart) / (double)Stopwatch.Frequency * 1000.0;
            var manipulateDelta = (manipulateEnd - manipulateStart) / (double)Stopwatch.Frequency * 1000.0;
            var snapshotDelta = snapshotTime / (double)Stopwatch.Frequency * 1000.0;
            var applyDelta = (applyEnd - applyStart) / (double)Stopwatch.Frequency * 1000.0;
            var deltaMs = (_debugTraceStopwatch.ElapsedTicks - startTime) / (double)Stopwatch.Frequency * 1000.0;

            traceLog!.AppendLine($"[DrawGizmo] Timing Breakdown:");
            traceLog.AppendLine($"  - Match: {matchDelta:F3}ms ({(deltaMs > 0 ? (matchDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"  - Setup: {gizmoSetupDelta:F3}ms ({(deltaMs > 0 ? (gizmoSetupDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"  - Manipulate: {manipulateDelta:F3}ms ({(deltaMs > 0 ? (manipulateDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"  - Snapshot: {snapshotDelta:F3}ms ({(deltaMs > 0 ? (snapshotDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"  - Apply: {applyDelta:F3}ms ({(deltaMs > 0 ? (applyDelta / deltaMs * 100) : 0):F1}%)");
            traceLog.AppendLine($"[DrawGizmo] END - IsUsing: {ImGuizmo.IsUsing()}, IsOver: {ImGuizmo.IsOver()}, TrackingTransform: {_trackingTransform.HasValue}, TotalDeltaTime: {deltaMs:F3}ms");
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
