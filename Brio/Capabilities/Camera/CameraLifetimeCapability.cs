using Brio.Entities.Camera;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.World;
using Brio.UI.Widgets.Camera;

namespace Brio.Capabilities.Camera;

public class CameraLifetimeCapability : CameraCapability
{
    private readonly VirtualCameraManager _virtualCameraManager;

    public VirtualCameraManager VirtualCameraManager => _virtualCameraManager;

    public CameraLifetimeCapability(CameraEntity parent, GPoseService gPoseService, VirtualCameraManager virtualCameraManager, ActorSpawnService actorSpawnService, LightingService lightingService) : base(parent, gPoseService)
    {
        _virtualCameraManager = virtualCameraManager;

        Widget = new CameraLifetimeWidget(this, actorSpawnService, lightingService);
    }

    public bool CanDestroy => CameraEntity.CameraID != 0;
}
