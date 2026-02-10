using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Camera;
using System.Numerics;
using Brio.Game.World;
using Brio.UI.Widgets.World.Lights;
using Brio.UI.Windows.Specialized;

namespace Brio.Capabilities.World;

public class LightLifetimeCapability : LightCapability
{
    private readonly LightingService _lightingService;
    private readonly LightWindow _lightWindow;
    private readonly VirtualCameraManager _cameraManager;

    public LightLifetimeCapability(Entity parent, ActorSpawnService actorSpawnService, VirtualCameraManager cameraManager, LightingService lightingService, LightWindow lightWindow) : base(parent)
    {
        _lightingService = lightingService;
        _lightWindow = lightWindow;
        _cameraManager = cameraManager;

        this.Widget = new LightLifetimeWidget(this, actorSpawnService, cameraManager, lightingService);
    }

    public bool CanDestroy => true;
    public bool CanClone => true;

    public bool IsLightWindowOpen => _lightWindow.IsOpen;

    public void ToggleLightWindow()
    {
        _lightWindow.IsOpen = !_lightWindow.IsOpen;
    }
    public void OpenLightWindow()
    {
        _lightWindow.IsOpen = true;
    }

    public void Destroy()
    {
        if(!CanDestroy)
            return;

        _lightingService.Destroy(GameLight);
    }

    public void Clone()
    {
        _lightingService.Clone(GameLight);
    }

    public void MoveToCamera()
    {
        if(_cameraManager.CurrentCamera is null)
            return;

        var cam = _cameraManager.CurrentCamera;
        System.Numerics.Vector3 camPos;
        if(cam.IsFreeCamera)
        {
            camPos = cam.Position;
        }
        else
        {
            unsafe
            {
                camPos = cam.BrioCamera->Position;
            }
        }

        unsafe
        {
            if(GameLight != null && GameLight.GameLight != null)
            {
                GameLight.GameLight->Transform.Position = camPos;

                if(cam.IsFreeCamera)
                    GameLight.GameLight->Transform.Rotation = cam.FreeCameraRotationAsQuaternion;
                else
                    GameLight.GameLight->Transform.Rotation = cam.BrioCamera->CalculateDirectionAsQuaternion();
            }
        }
    }
}
