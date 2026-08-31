using Brio.Entities.Camera;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.World;
using Brio.UI.Widgets.Camera;
using Brio.UI.Windows.Specialized;

namespace Brio.Capabilities.Camera;

public class CameraLifetimeCapability : CameraCapability
{
    private readonly VirtualCameraManager _virtualCameraManager;
    private readonly CameraWindow _cameraWindow;

    public VirtualCameraManager VirtualCameraManager => _virtualCameraManager;

    public CameraLifetimeCapability(CameraEntity parent, GPoseService gPoseService, VirtualCameraManager virtualCameraManager, ActorSpawnService actorSpawnService, LightingService lightingService, CameraWindow cameraWindow) : base(parent, gPoseService)
    {
        _virtualCameraManager = virtualCameraManager;
        _cameraWindow = cameraWindow;

        Widget = new CameraLifetimeWidget(this);
    }

    public bool CanDestroy => CameraEntity.CameraID != 0;

    public void OpenCameraWindow()
    {
        _cameraWindow.IsOpen = true;
    }
}
