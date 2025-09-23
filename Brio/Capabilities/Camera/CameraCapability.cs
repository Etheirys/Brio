using Brio.Capabilities.Core;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Game.Camera;
using Brio.Game.GPose;
using System;

namespace Brio.Capabilities.Camera;

public class CameraCapability : Capability, IDisposable
{
    private readonly GPoseService _gPoseService;
    public bool IsAllowed => _gPoseService.IsGPosing;

    public CameraCapability(Entity parent, GPoseService gPoseService) : base(parent)
    {
        _gPoseService = gPoseService;
        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public CameraEntity CameraEntity => (CameraEntity)Entity;

    public VirtualCamera VirtualCamera => CameraEntity.VirtualCamera;

    private void OnGPoseStateChange(bool newState)
    {
        if(newState is false && VirtualCamera.HasDelimitOverride)
        {
            VirtualCamera.DelimitCamera = false;
        }
    }

    public override void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
        base.Dispose();
    }
}
