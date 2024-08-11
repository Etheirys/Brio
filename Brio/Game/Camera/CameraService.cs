using Brio.Capabilities.Camera;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Game.Cutscene;
using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

using BrioRenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;
using BrioSceneCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera;

namespace Brio.Game.Camera;

internal unsafe class CameraService : IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly CutsceneManager _cutsceneManager;

    private delegate nint CameraCollisionDelegate(BrioCamera* a1, Vector3* a2, Vector3* a3, float a4, nint a5, float a6);
    private readonly Hook<CameraCollisionDelegate> _cameraCollisionHook = null!;

    private delegate nint CameraUpdateDelegate(BrioCamera* camera);
    private readonly Hook<CameraUpdateDelegate> _cameraUpdateHook = null!;

    private delegate nint CameraSceneUpdate(BrioSceneCamera* gsc);
    private readonly Hook<CameraSceneUpdate> _cameraSceneUpdateHook = null!;

    private delegate void CameraMatrixLoadDelegate(BrioRenderCamera* camera, nint a1);
    private readonly CameraMatrixLoadDelegate _cameraMatrixLoad;
   
    public CameraService(EntityManager entityManager, CutsceneManager cutsceneManager, GPoseService gPoseService, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _cutsceneManager = cutsceneManager;

        var cameraCollisionAddr = "E8 ?? ?? ?? ?? 4C 8D 45 ?? 89 83";
        _cameraCollisionHook = hooking.HookFromAddress<CameraCollisionDelegate>(scanner.ScanText(cameraCollisionAddr), CameraCollisionDetour);
        _cameraCollisionHook.Enable();

        var cameraUpdateAddr = "40 55 53 57 48 8D 6C 24 A0 48 81 EC ?? ?? ?? ?? 48 8B 1D";
        _cameraUpdateHook = hooking.HookFromAddress<CameraUpdateDelegate>(scanner.ScanText(cameraUpdateAddr), CameraUpdateDetour);
        _cameraUpdateHook.Enable();

        var cameraSceneUpdateAddr = "48 ?? ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? F6 81 EC ?? ?? ?? ?? 48 8B ?? 48 ?? ?? ??"; // old sig doesn't seem to get called anymore? // E8 ?? ?? ?? ?? 33 C0 48 89 83 ?? ?? ?? ?? 48 8B BC 24 ?? ?? ?? ??
        _cameraSceneUpdateHook = hooking.HookFromAddress<CameraSceneUpdate>(scanner.ScanText(cameraSceneUpdateAddr), CameraSceneUpdateDetour);
        _cameraSceneUpdateHook.Enable();

        var cameraMatrixLoadAddr = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B 93 90 02 ?? ?? 48 8D 4C 24 40");
        _cameraMatrixLoad = Marshal.GetDelegateForFunctionPointer<CameraMatrixLoadDelegate>(cameraMatrixLoadAddr);
    }

    private float _originalFOV;
    private bool _cutsceneEnded = false;
    private bool _firstCutsceneFrame = true;
    private nint CameraUpdateDetour(BrioCamera* camera)
    {
        var result = _cameraUpdateHook.Original(camera);

        if(_gPoseService.IsGPosing)
        {
            if(_cutsceneManager.VirtualCamera.IsActive)
            {
                if(_firstCutsceneFrame)
                {
                    _firstCutsceneFrame = false;
                    _cutsceneEnded = true;

                    _originalFOV = camera->FoV;
                }

                camera->FoV = _cutsceneManager.VirtualCamera.State.FoV;
            }
            else if(_cutsceneEnded)
            {
                camera->FoV = _originalFOV;

                _cutsceneEnded = false;
                _firstCutsceneFrame = true;
            }

            if(_entityManager.TryGetEntity<CameraEntity>("camera", out var cameraEntity))
            {
                if(cameraEntity.TryGetCapability<CameraCapability>(out var cameraCapability))
                {
                    if(camera == cameraCapability.Camera)
                    {
                        Vector3 currentPos = camera->Camera.CameraBase.SceneCamera.Object.Position;
                        var newPos = cameraCapability.PositionOffset + currentPos;
                        camera->Camera.CameraBase.SceneCamera.Object.Position = newPos;

                        Vector3 currentLookAt = camera->Camera.CameraBase.SceneCamera.LookAtVector;
                        camera->Camera.CameraBase.SceneCamera.LookAtVector = currentLookAt + (newPos - currentPos);
                    }
                }
            }
        }
        return result;
    }

    private nint CameraSceneUpdateDetour(BrioSceneCamera* gsc)
    {
        var exec = _cameraSceneUpdateHook.Original(gsc);
      
        if(_cutsceneManager.VirtualCamera.IsActive == false)
            return exec;

        var camMatrix = _cutsceneManager.UpdateCamera();

        if(camMatrix is null)
            return exec;

        gsc->ViewMatrix = _cutsceneManager.VirtualCamera.State.ViewMatrix;

        _cameraMatrixLoad(GetCurrentCamera()->Camera.CameraBase.SceneCamera.RenderCamera, (nint)(&gsc->ViewMatrix));

        return exec;
    }

    private nint CameraCollisionDetour(BrioCamera* camera, Vector3* a2, Vector3* a3, float a4, nint a5, float a6)
    {
        if(_gPoseService.IsGPosing)
        {
            if(_entityManager.TryGetEntity<CameraEntity>("camera", out var cameraEntity))
            {
                if(cameraEntity.TryGetCapability<CameraCapability>(out var cameraCapability))
                {
                    if(cameraCapability.DisableCollision)
                    {
                        if(camera == cameraCapability.Camera)
                        {
                            camera->Collide = new Vector2(camera->Camera.MaxDistance);
                            return 0;
                        }
                    }
                }
            }
        }

        return _cameraCollisionHook.Original(camera, a2, a3, a4, a5, a6);
    }
   
    public BrioCamera* GetCurrentCamera()
    {
        return (BrioCamera*)CameraManager.Instance()->GetActiveCamera();
    }

    public void Dispose()
    {
        _cameraCollisionHook.Dispose();
        _cameraUpdateHook.Dispose();
        _cameraSceneUpdateHook.Dispose();
    }
}
