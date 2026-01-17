using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.Game.World;
using Brio.UI.Widgets.World.Lights;
using Brio.UI.Windows.Specialized;

namespace Brio.Capabilities.World;

public class LightContainerCapability : LightCapability
{
    private readonly LightingService _lightingService;
    private readonly GPoseService _gPoseService;
    private readonly LightWindow _lightWindow;

    public bool IsAllowed => _gPoseService.IsGPosing;

    public LightingService LightingService => _lightingService;

    public LightContainerCapability(Entity parent, GPoseService gPoseService, LightWindow lightWindow, LightingService lightingService) : base(parent)
    {
        _lightingService = lightingService;
        _lightWindow = lightWindow;
        _gPoseService = gPoseService;

        Widget = new LightContainerWidget(this);
    }

    public void OpenLightWindow()
    {
        _lightWindow.IsOpen = true;
    }
}
