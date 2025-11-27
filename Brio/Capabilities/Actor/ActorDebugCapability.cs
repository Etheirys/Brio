using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.Posing;
using Brio.UI.Widgets.Actor;
using System.Collections.Generic;

namespace Brio.Capabilities.Actor;

public class ActorDebugCapability : ActorCharacterCapability
{

    public bool IsDebug => _configService.IsDebug;

    private readonly ConfigurationService _configService;
    private readonly ActorVFXService _vfxService;
    private readonly SkeletonService _skeletonService;

    public SkeletonService SkeletonService => _skeletonService;

    public ActorDebugCapability(ActorEntity parent, SkeletonService skeletonService, ConfigurationService configService, ActorVFXService actorVFXService) : base(parent)
    {
        _configService = configService;
        _vfxService = actorVFXService;
        _skeletonService = skeletonService;

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
