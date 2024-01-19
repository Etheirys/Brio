using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.UI.Widgets.Camera;
using Brio.UI.Windows.Specialized;
using System.Numerics;

namespace Brio.Capabilities.Camera;

internal class CameraCapability : Capability
{
    private readonly CameraService _cameraService;
    private readonly GPoseService _gPoseService;
    private readonly CameraWindow _cameraWindow;

    public unsafe BrioCamera* Camera => _cameraService.GetCurrentCamera();

    public bool DisableCollision { get; set; } = false;

    public unsafe bool DelimitCamera
    {
        get => _originalZoomLimits.HasValue;
        set
        {
            var camera = Camera;
            if(camera != null)
            {
                if(value)
                {

                    _originalZoomLimits = new Vector2(camera->Camera.MinDistance, camera->Camera.MaxDistance);
                    camera->Camera.MinDistance = 0f;
                    camera->Camera.MaxDistance = 500f;
                }
                else
                {
                    if(_originalZoomLimits.HasValue)
                    {

                        camera->Camera.MinDistance = _originalZoomLimits.Value.X;
                        camera->Camera.MaxDistance = _originalZoomLimits.Value.Y;

                        if(camera->Camera.Distance < camera->Camera.MinDistance)
                            camera->Camera.Distance = camera->Camera.MinDistance;

                        _originalZoomLimits = null;

                    }
                }
            }
        }
    }

    public bool IsOveridden => DisableCollision || DelimitCamera || PositionOffset != Vector3.Zero;

    public Vector3 PositionOffset { get; set; } = Vector3.Zero;

    public bool IsAllowed => _gPoseService.IsGPosing;

    private Vector2? _originalZoomLimits { get; set; } = null;

    public CameraCapability(Entity parent, CameraService cameraService, GPoseService gPoseService, CameraWindow cameraWindow) : base(parent)
    {
        _cameraService = cameraService;
        _gPoseService = gPoseService;
        _cameraWindow = cameraWindow;

        Widget = new CameraWidget(this);

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public unsafe void Reset()
    {
        DisableCollision = false;
        PositionOffset = Vector3.Zero;
        DelimitCamera = false;
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(!newState)
            Reset();
    }

    public override void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }

    public void ShowCameraWindow()
    {
        _cameraWindow.IsOpen = true;
    }
}
