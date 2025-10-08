using Brio.Config;
using Brio.Entities.World;
using Brio.Game.World;
using Brio.IPC;
using Brio.UI.Widgets.World.Lights;

namespace Brio.Capabilities.World;

public class LightDebugCapability : LightCapability
{
    public bool IsDebug => _configService.IsDebug;

    public DynamisIPC DynamisIPC => _dynamisIPC;

    private readonly ConfigurationService _configService;
    private readonly DynamisIPC _dynamisIPC;
    private readonly LightingService _lightingService;

    public LightingService LightingService => _lightingService;

    public LightDebugCapability(LightEntity parent, DynamisIPC dynamisIPC, LightingService lightingService, ConfigurationService configService) : base(parent)
    {
        _configService = configService;
        _dynamisIPC = dynamisIPC;
        _lightingService = lightingService;

        Widget = new LightDebugWidget(this);
    }
}
