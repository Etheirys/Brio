using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.World;
using Brio.UI.Widgets.World;
using Brio.UI.Windows.Specialized;

namespace Brio.Capabilities.World;

public class EnvironmentLifetimeCapability : LightCapability
{
    private readonly LightingService _lightingService;
    private readonly LightWindow _lightWindow;

    public EnvironmentLifetimeCapability(Entity parent, ActorSpawnService actorSpawnService, VirtualCameraManager cameraManager, LightingService lightingService, LightWindow lightWindow) : base(parent)
    {
        _lightingService = lightingService;
        _lightWindow = lightWindow;

        this.Widget = new EnvLifetimeWidget(this, actorSpawnService, cameraManager, lightingService);
    }

    public bool IsLightWindowOpen => _lightWindow.IsOpen;

    public void ToggleLightWindow()
    {
        _lightWindow.IsOpen = !_lightWindow.IsOpen;
    }
    public void OpenLightWindow()
    {
        _lightWindow.IsOpen = true;
    }

}
