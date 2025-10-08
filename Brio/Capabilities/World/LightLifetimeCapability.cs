using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World.Lights;
using Brio.UI.Windows.Specialized;

namespace Brio.Capabilities.World;

public class LightLifetimeCapability : LightCapability
{
    private readonly LightingService _lightingService;
    private readonly LightWindow _lightWindow;

    public LightLifetimeCapability(Entity parent, LightingService lightingService, LightWindow lightWindow) : base(parent)
    {
        _lightingService = lightingService;
        _lightWindow = lightWindow;

        this.Widget = new LightLifetimeWidget(this, lightingService);
    }

    public bool CanDestroy => true;
    public bool CanClone => true;

    public bool IsLightWindowOpen => _lightWindow.IsOpen;

    public void ToggleLightWindow()
    {
        _lightWindow.IsOpen = !_lightWindow.IsOpen;
    }
    public void OpenLightWindow()
    {
        _lightWindow.IsOpen = true;
    }

    public void Destroy()
    {
        if(!CanDestroy)
            return;

        _lightingService.Destroy(GameLight);
    }

    public void Clone()
    {
        _lightingService.Clone(GameLight);
    }
}
