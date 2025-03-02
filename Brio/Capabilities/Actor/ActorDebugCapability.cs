using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.UI.Widgets.Actor;
using System.Collections.Generic;

namespace Brio.Capabilities.Actor;

public class ActorDebugCapability : ActorCharacterCapability
{

    public bool IsDebug => _configService.IsDebug;

    private readonly ConfigurationService _configService;
    private readonly ActorVFXService _vfxService;

    public ActorDebugCapability(ActorEntity parent, ConfigurationService configService, ActorVFXService actorVFXService) : base(parent)
    {
        _configService = configService;
        _vfxService = actorVFXService;

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

    public ActorVFXService VFXService => _vfxService;
}
