using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.UI.Widgets.Camera;

namespace Brio.Capabilities.Camera;

public class CameraContainerCapability : Capability
{
    private readonly VirtualCameraManager _virtualCameraService;
    private readonly GPoseService _gPoseService;
    public bool IsAllowed => _gPoseService.IsGPosing;

    public VirtualCameraManager VirtualCameraManager => _virtualCameraService;
    public VirtualCamera CurrentCamera => VirtualCameraManager.CurrentCamera!;

    public CameraContainerCapability(Entity parent, GPoseService gPoseService, VirtualCameraManager virtualCameraService) : base(parent)
    {
        _gPoseService = gPoseService;
        _virtualCameraService = virtualCameraService;

        Widget = new CameraContainerWidget(this);
    }
}
