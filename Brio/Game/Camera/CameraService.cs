using Brio.Capabilities.Camera;
using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Numerics;

namespace Brio.Game.Camera;

internal unsafe class CameraService : IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;

    private delegate nint CameraCollisionDelegate(nint a1, nint a2, nint a3, nint a4, uint a5, float a6, nint a7);
    private readonly Hook<CameraCollisionDelegate> _cameraCollisionHook = null!;

    private delegate void CameraUpdateDelegate(BrioCamera* camera);
    private readonly Hook<CameraUpdateDelegate> _cameraUpdateHook = null!;

    public CameraService(EntityManager entityManager, GPoseService gPoseService, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;
        _gPoseService = gPoseService;

        var cameraCollisionAddr = "E8 ?? ?? ?? ?? 45 0F 57 FF";
        _cameraCollisionHook = hooking.HookFromAddress<CameraCollisionDelegate>(scanner.ScanText(cameraCollisionAddr), CameraCollisionDetour);
        _cameraCollisionHook.Enable();

        var cameraUpdateAddr = "40 55 53 48 8D 6C 24 B1 48 81 EC ?? ?? ?? ?? 48 8B D9"; // Camera.vf3
        _cameraUpdateHook = hooking.HookFromAddress<CameraUpdateDelegate>(scanner.ScanText(cameraUpdateAddr), CameraUpdateDetour);
        _cameraUpdateHook.Enable();
    }

    public BrioCamera* GetCurrentCamera()
    {
        return (BrioCamera*)CameraManager.Instance()->GetActiveCamera();
    }

    private void CameraUpdateDetour(BrioCamera* camera)
    {
        _cameraUpdateHook.Original(camera);

        if (_gPoseService.IsGPosing)
        {
            if (_entityManager.TryGetEntity<CameraEntity>("camera", out var cameraEntity))
            {
                if (cameraEntity.TryGetCapability<CameraCapability>(out var cameraCapability))
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

    private nint CameraCollisionDetour(nint a1, nint a2, nint a3, nint a4, uint a5, float a6, nint a7)
    {
        var result = _cameraCollisionHook.Original(a1, a2, a3, a4, a5, a6, a7);

        if (_gPoseService.IsGPosing)
        {
            if (_entityManager.TryGetEntity<CameraEntity>("camera", out var cameraEntity))
            {
                if (cameraEntity.TryGetCapability<CameraCapability>(out var cameraCapability))
                {
                    if (cameraCapability.DisableCollision)
                        return 0;
                }
            }
        }

        return result;
    }

    public void Dispose()
    {
        _cameraCollisionHook.Dispose();
        _cameraUpdateHook.Dispose();
    }
}
