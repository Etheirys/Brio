using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.World;
using Brio.UI.Widgets.World.Lights;
using Brio.UI.Windows.Specialized;
using System.Numerics;

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

        this.Widget = new LightLifetimeWidget(this);
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

    public unsafe void MoveToCamera()
    {
        if(_cameraManager.CurrentCamera is null)
            return;

        if(GameLight != null && GameLight.GameLight != null && Entity is TransformableEntity transformableEntity)
        {
            var cam = _cameraManager.CurrentCamera;

            Vector3 camPos = cam.IsFreeCamera ? cam.Position : cam.BrioCamera->Position;
            Quaternion camRot = cam.IsFreeCamera ? cam.FreeCameraRotationAsQuaternion : cam.BrioCamera->CalculateDirectionAsQuaternion();

            var transform = transformableEntity.Transform;
            transform.Position = camPos;
            transform.Rotation = camRot;

            transformableEntity.Transform = transform;
        }
    }
}
