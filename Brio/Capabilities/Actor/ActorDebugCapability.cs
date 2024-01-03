using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities.Actor;
using Brio.UI.Widgets.Actor;
using System.Collections.Generic;

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

    public Dictionary<string, int> SkeletonStacks
    {
        get
        {
            if(Entity.TryGetCapability<SkeletonPosingCapability>(out var capability))
                return capability.PoseInfo.StackCounts;

            return [];
        }
    }
}
