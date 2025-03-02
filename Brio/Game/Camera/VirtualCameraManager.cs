﻿using Brio.Core;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Game.GPose;
using Brio.Game.Input;
using Dalamud.Game.ClientState.Keys;
using Microsoft.Extensions.DependencyInjection;
using Swan;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Game.Camera;

public class VirtualCameraManager : IDisposable
{
    public const float DefaultMovementSpeed = 0.04f;
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

    int _cameraId = 1;
    private readonly Dictionary<int, CameraEntity> _createdCameras = [];

    private float _moveSpeed = DefaultMovementSpeed;
    //private float _mouseSensitivity = DefaultMouseSensitivity;

    public (bool, int) CreateCamera(CameraType cameraType, bool selectCamera = true, bool targetNewInHierarch = true, VirtualCamera? virtualCamera = null)
    {
        if(_entityManager.TryGetEntity("cameras", out var ent))
        {
            CurrentCamera?.DeactivateCamera();

            _cameraId++;

            var camEnt = ActivatorUtilities.CreateInstance<CameraEntity>(_serviceProvider, _cameraId, cameraType);
            _entityManager.AttachEntity(camEnt, ent);

            if(virtualCamera is null)
            {
                switch(cameraType)
                {
                    case CameraType.Free:
                        camEnt.VirtualCamera.FreeCamValues.MovementSpeed = DefaultMovementSpeed;
                        camEnt.VirtualCamera.FreeCamValues.MouseSensitivity = DefaultMouseSensitivity;
                        camEnt.VirtualCamera.IsFreeCamera = true;
                        camEnt.VirtualCamera.ActivateCamera();
                        camEnt.VirtualCamera.ToFreeCam();
                        camEnt.VirtualCamera.DeactivateCamera();
                        _createdCameras.Add(_cameraId, camEnt);
                        break;
                    case CameraType.Brio:
                        camEnt.VirtualCamera.IsFreeCamera = false;
                        camEnt.VirtualCamera.ActivateCamera();
                        camEnt.VirtualCamera.DeactivateCamera();
                        _createdCameras.Add(_cameraId, camEnt);
                        break;
                    //case CameraType.Cutscene:
                    //    unimplemented
                    //    break;
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

            return (true, _cameraId);
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

                _cameraId++;

                var oldCam = oldCamEnt.VirtualCamera;
                var newCam = ActivatorUtilities.CreateInstance<CameraEntity>(_serviceProvider, _cameraId, oldCamEnt.CameraType);
                _entityManager.AttachEntity(newCam, ent);

                oldCam.CopyPropertiesTo(newCam.VirtualCamera);
                newCam.VirtualCamera.Rotation = oldCam.Rotation;

                if(oldCamEnt.CameraType == CameraType.Free)
                {
                    newCam.VirtualCamera.Position = oldCam.Position;
                    newCam.VirtualCamera.IsFreeCamera = true;
                }
                else
                {
                    newCam.VirtualCamera.PositionOffset = oldCam.PositionOffset;
                    newCam.VirtualCamera.Angle = oldCam.Angle;
                    newCam.VirtualCamera.Pan = oldCam.Pan;
                }

                _createdCameras.Add(_cameraId, newCam);
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

                return (true, _cameraId);
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

        Brio.Log.Verbose("Destroying Brio camera " + _cameraId);

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
        _cameraId = 0;
        CurrentCamera = null;
        foreach(var item in _createdCameras.Values)
        {
            DestroyCamera(item.CameraID);
        }
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

    public unsafe void Update(MouseFrame* mouseFrame, KeyboardFrame* keyboardFrame)
    {
        if((mouseFrame is null && keyboardFrame is null) || CurrentCamera is null)
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

        if(FreeCamValues.IsMovementEnabled == false)
        {
            return;
        }

        // Initialize forward and lateral movement variables
        var forwardBackward = 0;
        var leftRight = 0;
        var upDown = 0;

        // Check for forward and backward movement
        if(keyboardFrame->IsKeyDown(VirtualKey.W, true))
            forwardBackward -= 1;
        if(keyboardFrame->IsKeyDown(VirtualKey.S, true))
            forwardBackward += 1;

        // Check for lateral movement
        if(keyboardFrame->IsKeyDown(VirtualKey.A, true))
            leftRight -= 1;
        if(keyboardFrame->IsKeyDown(VirtualKey.D, true))
            leftRight += 1;

        // Handle vertical movement (up and down)
        if(keyboardFrame->IsKeyDown(VirtualKey.E, true) || keyboardFrame->IsKeyDown(VirtualKey.SPACE, true))
            upDown += 1;
        else if(keyboardFrame->IsKeyDown(VirtualKey.Q, true) || keyboardFrame->IsKeyDown(VirtualKey.CONTROL, true))
            upDown += -1;

        // Handle movement speed
        if(keyboardFrame->IsKeyDown(VirtualKey.SHIFT, true))
            _moveSpeed = CurrentCamera.FreeCamValues.MovementSpeed * 3;
        else if(keyboardFrame->IsKeyDown(VirtualKey.MENU, true))
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
        return new Matrix4x4
        (
            rightVector.X, upVector.X, lookDirection.X, 0.0f,
            rightVector.Y, upVector.Y, lookDirection.Y, 0.0f,
            rightVector.Z, upVector.Z, lookDirection.Z, 0.0f,

            (-CurrentCamera.Position.X * rightVector.X) - (CurrentCamera.Position.Y * rightVector.Y) - (CurrentCamera.Position.Z * rightVector.Z),
            (-CurrentCamera.Position.X * upVector.X) - (CurrentCamera.Position.Y * upVector.Y) - (CurrentCamera.Position.Z * upVector.Z),
            (-CurrentCamera.Position.X * lookDirection.X) - (CurrentCamera.Position.Y * lookDirection.Y) - (CurrentCamera.Position.Z * lookDirection.Z),
            1f
        );
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
            if(defaultCam != null)
                SelectCamera(defaultCam.VirtualCamera);
        }
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
