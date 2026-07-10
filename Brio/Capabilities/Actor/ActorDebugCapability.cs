using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities.Actor;
using Brio.Game.Core;
using Brio.Game.Posing;
using Brio.UI.Widgets.Actor;
using Dalamud.Plugin.Services;
using System.Collections.Generic;

namespace Brio.Capabilities.Actor;

public class ActorDebugCapability : ActorCharacterCapability
{

    public bool IsDebug => _configService.IsDebug;

    private readonly ConfigurationService _configService;
    private readonly VFXService _vfxService;
    private readonly SkeletonService _skeletonService;
    public readonly IObjectTable _gameObjects;

    public SkeletonService SkeletonService => _skeletonService;

    public ActorDebugCapability(ActorEntity parent, IObjectTable gameObjects, SkeletonService skeletonService, ConfigurationService configService, VFXService actorVFXService) : base(parent)
    {
        _configService = configService;
        _vfxService = actorVFXService;
        _skeletonService = skeletonService;
        _gameObjects = gameObjects;

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

    public VFXService VFXService => _vfxService;
}
