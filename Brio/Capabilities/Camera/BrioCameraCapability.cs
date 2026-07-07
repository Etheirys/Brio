using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.UI.Widgets.Camera;
using Brio.UI.Windows.Specialized;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Capabilities.Camera;

public class BrioCameraCapability : CameraCapability
{
    private readonly CameraWindow _cameraWindow;
    private readonly VirtualCameraManager _virtualCameraService;
    public readonly ConfigurationService _configurationService;
    public readonly EntityManager _entityManager;

    public bool CanUndo
        => _undoStack.Count is not 0 and not 1;
    public bool CanRedo
        => _redoStack.Count > 0;

    private Stack<CameraSnapshot> _undoStack = [];
    private Stack<CameraSnapshot> _redoStack = [];

    public BrioCameraCapability(CameraEntity parent, EntityManager entityManager, VirtualCameraManager virtualCameraService, GPoseService gPoseService, CameraWindow cameraWindow, ConfigurationService configService) : base(parent, gPoseService)
    {
        _virtualCameraService = virtualCameraService;
        _cameraWindow = cameraWindow;
        _entityManager = entityManager;

        _configurationService = configService;

        Widget = new BrioCameraWidget(this);
    }

    public override void OnEntitySelected()
    {
        _virtualCameraService.SelectCamera(VirtualCamera);
    }

    public void ShowCameraWindow()
    {
        _cameraWindow.IsOpen = true;
    }

    private bool _isTrackingEdit = false;
    public void TrackEdit(bool anyActive)
    {
        if(anyActive)
        {
            _isTrackingEdit = true;
        }
        else if(_isTrackingEdit)
        {
            _isTrackingEdit = false;
            Snapshot();
        }
    }

    public void Snapshot()
    {
        var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
        if(undoStackSize <= 0)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            return;
        }

        _redoStack.Clear();

        if(_undoStack.Count == 0)
            _undoStack.Push(CaptureBaseline());

        _undoStack.Push(CaptureCurrent());
        _undoStack = _undoStack.Trim(undoStackSize + 1);
    }

    public void Undo()
    {
        if(_undoStack.TryPop(out var undoStack))
            _redoStack.Push(undoStack);

        if(_undoStack.TryPeek(out var applicable))
            ApplyState(applicable);
    }
    public void Redo()
    {
        if(_redoStack.TryPop(out var redoStack))
        {
            _undoStack.Push(redoStack);
            ApplyState(redoStack);
        }
    }

    private CameraSnapshot CaptureBaseline() => new(
        VirtualCamera.SpawnPosition, Vector3.Zero,
        Vector3.Zero, Vector3.Zero, "Select an actor to track",
        0f, 2.5f, 0f, Vector2.Zero, Vector2.Zero,
        false, false, false,
        VirtualCamera.FreeCamValues.MovementSpeed, VirtualCamera.FreeCamValues.MouseSensitivity, VirtualCamera.FreeCamValues.DelimitAngle, VirtualCamera.FreeCamValues.IsMovementEnabled, VirtualCamera.FreeCamValues.Move2D);

    private CameraSnapshot CaptureCurrent() => new(
        VirtualCamera.Position, VirtualCamera.Rotation,
        VirtualCamera.PositionOffset, VirtualCamera.TargetOffset, VirtualCamera.SelectedActorName,
        VirtualCamera.PivotRotation, VirtualCamera.Zoom, VirtualCamera.FoV, VirtualCamera.Pan, VirtualCamera.Angle,
        VirtualCamera.DisableCollision, VirtualCamera.DelimitCamera, VirtualCamera.IsPortraitMode,
        VirtualCamera.FreeCamValues.MovementSpeed, VirtualCamera.FreeCamValues.MouseSensitivity, VirtualCamera.FreeCamValues.DelimitAngle, VirtualCamera.FreeCamValues.IsMovementEnabled, VirtualCamera.FreeCamValues.Move2D);

    private void ApplyState(CameraSnapshot state)
    {
        VirtualCamera.Position = state.Position;
        VirtualCamera.Rotation = state.Rotation;
        VirtualCamera.PositionOffset = state.PositionOffset;
        VirtualCamera.TargetOffset = state.TargetOffset;
        VirtualCamera.SelectedActorName = state.SelectedActorName;
        VirtualCamera.PivotRotation = state.PivotRotation;
        VirtualCamera.Zoom = state.Zoom;
        VirtualCamera.FoV = state.FoV;
        VirtualCamera.Pan = state.Pan;
        VirtualCamera.Angle = state.Angle;
        VirtualCamera.DisableCollision = state.DisableCollision;
        VirtualCamera.DelimitCamera = state.DelimitCamera;

        if(state.IsPortraitMode != VirtualCamera.IsPortraitMode)
            VirtualCamera.TogglePortraitMode();

        VirtualCamera.FreeCamValues.MovementSpeed = state.MovementSpeed;
        VirtualCamera.FreeCamValues.MouseSensitivity = state.MouseSensitivity;
        VirtualCamera.FreeCamValues.DelimitAngle = state.DelimitAngle;
        VirtualCamera.FreeCamValues.IsMovementEnabled = state.IsMovementEnabled;
        VirtualCamera.FreeCamValues.Move2D = state.Move2D;
    }
}

public record struct CameraSnapshot(Vector3 Position, Vector3 Rotation, Vector3 PositionOffset, Vector3 TargetOffset, string SelectedActorName,
    float PivotRotation, float Zoom, float FoV, Vector2 Pan, Vector2 Angle,
    bool DisableCollision, bool DelimitCamera, bool IsPortraitMode, float MovementSpeed, float MouseSensitivity, bool DelimitAngle, bool IsMovementEnabled, bool Move2D);
