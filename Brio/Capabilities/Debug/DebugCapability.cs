using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.UI.Widgets.Debug;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace Brio.Capabilities.Debug;

internal unsafe class DebugCapability : Capability
{
    private readonly GPoseService _gPoseService;

    public DebugCapability(Entity parent, GPoseService gPoseService) : base(parent)
    {
        _gPoseService = gPoseService;
        Widget = new DebugWidget(this);
    }

    public void EnterGPose()
    {
        Framework.Instance()->GetUiModule()->EnterGPose();
    }

    public void ExitGPose()
    {
        Framework.Instance()->GetUiModule()->ExitGPose();
    }

    public bool FakeGPose
    {
        get => _gPoseService.FakeGPose;
        set => _gPoseService.FakeGPose = value;
    }

    public IReadOnlyDictionary<string, nint> GetInterestingAddresses()
    {
        var addresses = new Dictionary<string, nint>
        {
            ["GameMain"] = ((nint)GameMain.Instance()),
            ["Framework"] = ((nint)Framework.Instance()),
            ["EnvManager"] = ((nint)EnvManager.Instance()),
            ["LayoutWorld"] = ((nint)LayoutWorld.Instance()),
            ["CameraManager"] = ((nint)CameraManager.Instance()),
            ["ActiveCamera"] = ((nint)CameraManager.Instance()->GetActiveCamera()),
            ["EventFramework"] = ((nint)EventFramework.Instance()),
        };

        return addresses.AsReadOnly();
    }
}
