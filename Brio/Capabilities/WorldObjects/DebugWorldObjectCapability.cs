using Brio.Config;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.Game.WorldObjects;
using Brio.IPC;
using Brio.UI.Widgets.WorldObjects;

namespace Brio.Capabilities.WorldObjects;

public class DebugWorldObjectCapability : WorldObjectCapability
{
    public bool IsDebug => _configService.IsDebug;

    public readonly ConfigurationService _configService;
    public readonly DynamisService _dynamisIPC;
    public readonly GPoseService _gPoseService;
    public readonly WorldObjectService _worldObjectService;

    public DebugWorldObjectCapability(Entity parent, GPoseService gPoseService, WorldObjectService worldObjectService, DynamisService dynamisIPC, ConfigurationService configService) : base(parent)
    {
        _configService = configService;
        _dynamisIPC = dynamisIPC;
        _gPoseService = gPoseService;
        _worldObjectService = worldObjectService;

        this.Widget = new DebugWorldObjectWidget(this);
    }
}
