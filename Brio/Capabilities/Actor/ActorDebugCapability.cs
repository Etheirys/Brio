using Brio.Config;
using Brio.Entities.Actor;
using Brio.UI.Widgets.Actor;

namespace Brio.Capabilities.Actor;

internal class ActorDebugCapability : ActorCharacterCapability
{

    public bool IsDebug => _configService.IsDebug;

    private readonly ConfigurationService _configService;

    public ActorDebugCapability(ActorEntity parent, ConfigurationService configService) : base(parent)
    {
        _configService = configService;

        Widget = new ActorDebugWidget(this);
    }
}
