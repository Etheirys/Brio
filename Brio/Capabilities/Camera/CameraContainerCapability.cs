using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.UI.Widgets.Camera;
using Brio.UI.Windows.Specialized;

namespace Brio.Capabilities.Camera;

public class CameraContainerCapability : Capability
{
    private readonly VirtualCameraManager _virtualCameraService;
    private readonly GPoseService _gPoseService;
    private readonly CameraWindow _cameraWindow;
    public bool IsAllowed => _gPoseService.IsGPosing;

    public VirtualCameraManager VirtualCameraManager => _virtualCameraService;
    public VirtualCamera CurrentCamera => VirtualCameraManager.CurrentCamera!;

    public CameraContainerCapability(Entity parent, CameraWindow cameraWindow, GPoseService gPoseService, VirtualCameraManager virtualCameraService) : base(parent)
    {
        _gPoseService = gPoseService;
        _virtualCameraService = virtualCameraService;
        _cameraWindow = cameraWindow;

        Widget = new CameraContainerWidget(this);
    }

    public void OpenCameraWindow()
    {
        _cameraWindow.IsOpen = true;
    }

}
