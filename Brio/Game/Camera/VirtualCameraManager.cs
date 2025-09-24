using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Game.GPose;
using Brio.Game.Input;
using Brio.Input;
using Microsoft.Extensions.DependencyInjection;
using Swan;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Game.Camera;

public class VirtualCameraManager : IDisposable
{
    public const float DefaultMovementSpeed = 0.03f;
    public const float DefaultMouseSensitivity = 0.1f;

    public VirtualCamera? CurrentCamera { get; private set; }
    public FreeCamValues FreeCamValues => CurrentCamera?.FreeCamValues!;

    public int CamerasCount => _createdCameras.Count;

    private readonly IServiceProvider _serviceProvider;
    private readonly GPoseService _gPoseService;
    private readonly EntityManager _entityManager;

    public VirtualCameraManager(IServiceProvider serviceProvider, GPoseService gPoseService, EntityManager entityManager)
    {
        _serviceProvider = serviceProvider;
        _gPoseService = gPoseService;
        _entityManager = entityManager;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    private readonly Vector3 Up = new(0f, 1f, 0f);

    private int _nextCameraId = 1;
    private readonly Dictionary<int, CameraEntity> _createdCameras = [];

    private float _moveSpeed = DefaultMovementSpeed;

    public (bool, int) CreateCamera(CameraType cameraType, bool selectCamera = true, bool targetNewInHierarch = true, VirtualCamera? virtualCamera = null)
    {
        if(_entityManager.TryGetEntity("cameras", out var ent))
        {
            CurrentCamera?.DeactivateCamera();

            int cameraId = _nextCameraId + 1;

            var camEnt = ActivatorUtilities.CreateInstance<CameraEntity>(_serviceProvider, cameraId, cameraType);
            _entityManager.AttachEntity(camEnt, ent);

            if(virtualCamera is null)
            {
                switch(cameraType)
                {
                    case CameraType.Free:
                        camEnt.VirtualCamera.FreeCamValues.MovementSpeed = DefaultMovementSpeed;
                        camEnt.VirtualCamera.FreeCamValues.MouseSensitivity = DefaultMouseSensitivity;
                        camEnt.VirtualCamera.IsFreeCamera = true;
                        camEnt.VirtualCamera.ToFreeCam();
                        camEnt.VirtualCamera.ActivateCamera();
                        camEnt.VirtualCamera.DeactivateCamera();
                        _createdCameras.Add(cameraId, camEnt);
                        break;
                    case CameraType.Brio:
                        camEnt.VirtualCamera.IsFreeCamera = false;
                        camEnt.VirtualCamera.ActivateCamera();
                        camEnt.VirtualCamera.DeactivateCamera();
                        _createdCameras.Add(cameraId, camEnt);
                        break;
                    case CameraType.Cutscene:
                        camEnt.VirtualCamera.IsCutsceneCamera = true;
                        camEnt.VirtualCamera.ActivateCamera();
                        camEnt.VirtualCamera.DeactivateCamera();
                        _createdCameras.Add(cameraId, camEnt);
                        break;
                    default:
                        Brio.Log.Error($"Unknown camera type: {cameraType}");
                        break;
                }
            }
            else
            {
                var id = camEnt.SetVirtualCamera(virtualCamera);

                if(_createdCameras.TryAdd(id, camEnt) == false)
                {
                    _createdCameras[id] = camEnt;
                }
            }

            CurrentCamera?.ActivateCamera();


            if(targetNewInHierarch)
                _entityManager.SetSelectedEntity(camEnt);
            else if(selectCamera)
                SelectCamera(camEnt.VirtualCamera);

            _nextCameraId = cameraId;
            return (true, cameraId);
        }
        return (false, -1);
    }

    public (bool, int) CloneCamera(int cameraID)
    {
        Brio.Log.Verbose($"Cloning camera {cameraID}");

        if(_entityManager.TryGetEntity("cameras", out var ent))
        {
            if(_createdCameras.TryGetValue(cameraID, out CameraEntity? oldCamEnt))
            {
                CurrentCamera?.DeactivateCamera();

                int newCameraId = _nextCameraId + 1;

                var oldCam = oldCamEnt.VirtualCamera;
                var newCam = ActivatorUtilities.CreateInstance<CameraEntity>(_serviceProvider, newCameraId, oldCamEnt.CameraType);
                _entityManager.AttachEntity(newCam, ent);

                oldCam.CopyPropertiesTo(newCam.VirtualCamera);
                newCam.VirtualCamera.Rotation = oldCam.Rotation;

                if(oldCamEnt.CameraType == CameraType.Free)
                {
                    newCam.VirtualCamera.Position = oldCam.Position;
                    newCam.VirtualCamera.ToFreeCam();
                }
                else
                {
                    newCam.VirtualCamera.PositionOffset = oldCam.PositionOffset;
                    newCam.VirtualCamera.Angle = oldCam.Angle;
                    newCam.VirtualCamera.Pan = oldCam.Pan;
                }

                _createdCameras.Add(newCameraId, newCam);
                CurrentCamera?.ActivateCamera();
                _entityManager.SetSelectedEntity(newCam);

                if(oldCamEnt.CameraType == CameraType.Free)
                {
                    newCam.VirtualCamera.FreeCamValues.DelimitAngle = oldCam.FreeCamValues.DelimitAngle;
                    newCam.VirtualCamera.FreeCamValues.MovementSpeed = oldCam.FreeCamValues.MovementSpeed;
                    newCam.VirtualCamera.FreeCamValues.MouseSensitivity = oldCam.FreeCamValues.MouseSensitivity;
                }
                else
                {
                    newCam.VirtualCamera.DisableCollision = oldCam.DisableCollision;
                    newCam.VirtualCamera.DelimitCamera = oldCam.DelimitCamera;
                }

                _nextCameraId = newCameraId;
                return (true, newCameraId);
            }
            Brio.Log.Error($"Camera with ID {cameraID} not found");
            return (false, -1);
        }
        Brio.Log.Error("No camera container found");
        return (false, -1);
    }

    public bool DestroyCamera(int cameraID)
    {
        if(cameraID == 0)
            return false;

        Brio.Log.Verbose("Destroying Brio camera " + cameraID);

        if(_entityManager.TryGetEntity("cameras", out var ent))
        {
            if(_createdCameras.TryGetValue(cameraID, out CameraEntity? camEnt))
            {
                if(_entityManager.TryGetEntity(new Entities.Core.CameraId(0), out var defaultCamEnt) && defaultCamEnt is CameraEntity cameraEnt)
                {
                    SelectCamera(cameraEnt.VirtualCamera);
                    _entityManager.SetSelectedEntity(defaultCamEnt);
                }

                ent.RemoveChild(camEnt);

                // destroy the camera from the dictionary
                // prevents cameras using old camera positions/rotations
                _createdCameras.Remove(cameraID);

                return true;
            }
        }
        return false;
    }

    public void SelectCamera(VirtualCamera virtualCamera)
    {
        CurrentCamera?.DeactivateCamera();

        CurrentCamera = virtualCamera;

        CurrentCamera.ActivateCamera();
    }

    public void DestroyAll()
    {
        if(CurrentCamera?.CameraID != 0)
            CurrentCamera = null;
        foreach(var item in _createdCameras.Values)
        {
            DestroyCamera(item.CameraID);
        }
        _nextCameraId = 1;
    }

    public void SelectInHierarchy(CameraEntity selectedEntity)
    {
        if(selectedEntity is not null)
            _entityManager.SetSelectedEntity(new Entities.Core.CameraId(selectedEntity.CameraID));
    }

    public IEnumerable<CameraEntity> GetAllCameras()
    {
        foreach(var item in _createdCameras.Values)
        {
            yield return item;
        }
    }

    //
    // Free Cam
    //

    public Vector3 _forward;
    public Vector2 _lastMousePosition;

    public unsafe void Update(MouseFrame* mouseFrame)
    {
        if(mouseFrame is null || CurrentCamera is null)
        {
            return;
        }

        //
        // Handle mouse input
        //

        if(mouseFrame->IsKeyDown(MouseState.Right))
        {
            _lastMousePosition += mouseFrame->GetDeltaAsVector2();

            mouseFrame->HandleDelta();
        }

        //
        // Handle keyboard input
        //

        // This removes the purgatory function and prevent camera crazing out when typing with the camera enabled
        if(FreeCamValues.IsMovementEnabled == false)
        {
            return;
        }

        // Initialize forward and lateral movement variables
        var forwardBackward = 0;
        var leftRight = 0;
        var upDown = 0;

        // Check for forward and backward movement
        if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_Forward))
            forwardBackward -= 1;
        if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_Backward))
            forwardBackward += 1;

        // Check for lateral movement
        // Invert logic around the 90 degree pivot points
        // (Similar to XIV's Default Camera)
        if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_Left))
            if(CurrentCamera.IsFreeCamera)
                if(ConfigurationService.Instance.Configuration.InputManager.FlipKeyBindsPastNinety && (CurrentCamera.PivotRotation < BrioUtilities.DegreesToRadians(-90) || CurrentCamera.PivotRotation > BrioUtilities.DegreesToRadians(90)))
                    leftRight += 1;
                else
                    leftRight -= 1;
            else
                leftRight += 1;

        if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_Right))
            if(CurrentCamera.IsFreeCamera)
                if(ConfigurationService.Instance.Configuration.InputManager.FlipKeyBindsPastNinety && (CurrentCamera.PivotRotation < BrioUtilities.DegreesToRadians(-90) || CurrentCamera.PivotRotation > BrioUtilities.DegreesToRadians(90)))
                    leftRight -= 1;
                else
                    leftRight += 1;
            else
                leftRight -= 1;

        // Handle vertical movement (up and down)
        // Invert logic around the 90 degree pivot points (like lateral movement)
        if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_Up) || InputManagerService.ActionKeysPressed(InputAction.FreeCamera_UpAlt))
            if(CurrentCamera.IsFreeCamera)
                if(ConfigurationService.Instance.Configuration.InputManager.FlipKeyBindsPastNinety && (CurrentCamera.PivotRotation < BrioUtilities.DegreesToRadians(-90) || CurrentCamera.PivotRotation > BrioUtilities.DegreesToRadians(90)))
                    upDown -= 1;
                else
                    upDown += 1;
            else
                upDown -= 1;
        else if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_Down) || InputManagerService.ActionKeysPressed(InputAction.FreeCamera_DownAlt))
            if(CurrentCamera.IsFreeCamera)
                if(ConfigurationService.Instance.Configuration.InputManager.FlipKeyBindsPastNinety && (CurrentCamera.PivotRotation < BrioUtilities.DegreesToRadians(-90) || CurrentCamera.PivotRotation > BrioUtilities.DegreesToRadians(90)))
                    upDown += 1;
                else
                    upDown -= 1;
            else
                upDown += 1;

        // Handle movement speed
        if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_IncreaseCamMovement))
            _moveSpeed = CurrentCamera.FreeCamValues.MovementSpeed * 3;
        else if(InputManagerService.ActionKeysPressed(InputAction.FreeCamera_DecreaseCamMovement))
            _moveSpeed = CurrentCamera.FreeCamValues.MovementSpeed * 0.3f;

        _forward = Vector3.Transform(new Vector3(leftRight, upDown, forwardBackward),
            Quaternion.CreateFromYawPitchRoll(CurrentCamera!.Rotation.X, FreeCamValues.Move2D ? 0 : -CurrentCamera.Rotation.Y, CurrentCamera.Rotation.Z));
    }
    public Matrix4x4 UpdateMatrix()
    {
        if(CurrentCamera is null)
        {
            throw new NullReferenceException("CurrentCamera can not be null");
        }

        _lastMousePosition *= CurrentCamera.FreeCamValues.MouseSensitivity * MathHelpers.DegreesToRadians;
        CurrentCamera.Position += _forward * _moveSpeed;

        CurrentCamera.Rotation.X -= _lastMousePosition.X;
        CurrentCamera.Rotation.Y = FreeCamValues.DelimitAngle ? CurrentCamera.Rotation.Y + _lastMousePosition.Y : Math.Clamp(CurrentCamera.Rotation.Y + _lastMousePosition.Y, -1.5f, 1.5f);

        _moveSpeed = CurrentCamera.FreeCamValues.MovementSpeed;
        _lastMousePosition = Vector2.Zero;
        _forward = Vector3.Zero;

        var lookVector = new Vector3(
            MathF.Sin(CurrentCamera.Rotation.X) * MathF.Cos(CurrentCamera.Rotation.Y),
            MathF.Sin(CurrentCamera.Rotation.Y),
            MathF.Cos(CurrentCamera.Rotation.X) * MathF.Cos(CurrentCamera.Rotation.Y));

        // Normalize the look direction
        var lookDirection = Vector3.Normalize(lookVector);
        var rightVector = Vector3.Normalize(Vector3.Cross(Up, lookDirection));
        var upVector = Vector3.Cross(lookDirection, rightVector);

        // Create the view matrix
        var matrix = new Matrix4x4
        (
            rightVector.X, upVector.X, lookDirection.X, 0.0f,
            rightVector.Y, upVector.Y, lookDirection.Y, 0.0f,
            rightVector.Z, upVector.Z, lookDirection.Z, 0.0f,

            (-CurrentCamera.Position.X * rightVector.X) - (CurrentCamera.Position.Y * rightVector.Y) - (CurrentCamera.Position.Z * rightVector.Z),
            (-CurrentCamera.Position.X * upVector.X) - (CurrentCamera.Position.Y * upVector.Y) - (CurrentCamera.Position.Z * upVector.Z),
            (-CurrentCamera.Position.X * lookDirection.X) - (CurrentCamera.Position.Y * lookDirection.Y) - (CurrentCamera.Position.Z * lookDirection.Z),
            1f
        );

        // apply the Z axis rotation
        var viewMatrix = Matrix4x4.Transform(matrix, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, CurrentCamera.PivotRotation));
        return viewMatrix;
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(newState == false)
        {
            DestroyAll();
        }
        else
        {
            var defaultCam = _entityManager.GetEntity<CameraEntity>(new Entities.Core.CameraId(0));
            if(defaultCam is not null)
            {
                defaultCam.VirtualCamera.SaveCameraState();
                SelectCamera(defaultCam.VirtualCamera);
            }
        }
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
