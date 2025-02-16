using Brio.Entities.Camera;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.UI.Widgets.Camera;

namespace Brio.Capabilities.Camera;

public class CameraLifetimeCapability : CameraCapability
{
    private readonly VirtualCameraManager _virtualCameraManager;

    public VirtualCameraManager VirtualCameraManager => _virtualCameraManager;

    public CameraLifetimeCapability(CameraEntity parent, GPoseService gPoseService, VirtualCameraManager virtualCameraManager) : base(parent, gPoseService)
    {
        _virtualCameraManager = virtualCameraManager;

        Widget = new CameraLifetimeWidget(this);
    }

    public bool CanDestroy => CameraEntity.CameraID != 0;
    public bool CanClone => CameraEntity.CameraID != 0;
}
