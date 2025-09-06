using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;

namespace Brio.Capabilities.Actor;
public class ActorDynamicPoseCapability : ActorCharacterCapability
{
    private readonly ActorLookAtService _actorLookAtService;
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly GPoseService _gposeService;
    private readonly VirtualCameraManager _virtualCameraManager;

    public VirtualCamera? Camera => _virtualCameraManager.CurrentCamera;

    public ActorDynamicPoseCapability(ActorEntity parent, ActorLookAtService actorLookAtService, VirtualCameraManager virtualCameraManager, ActorAppearanceService actorAppearanceService, GPoseService gPoseService) : base(parent)
    {
        _actorLookAtService = actorLookAtService;
        _actorAppearanceService = actorAppearanceService;
        _gposeService = gPoseService;
        _virtualCameraManager = virtualCameraManager;

        Widget = new ActorDynamicPoseWidget(this);
    }

    public unsafe void StartLookAt()
    {
        _actorLookAtService.AddObjectToLook(GameObject);
    }

    public unsafe void StopLookAt()
    {
        _actorLookAtService.RemoveObjectFromLook(GameObject);
    }

    public unsafe void SetMode(LookAtTargetMode lookAtTargetMode)
    {
        _actorLookAtService.SetTargetMode(GameObject, lookAtTargetMode);
    }

    public void SetTargetLock(bool doLock, LookAtTargetType targetType, Vector3 target)
    {
        _actorLookAtService.SetTargetLock(GameObject, doLock, targetType, target);
    }

    public void SetTargetType(LookAtTargetType lookAtTarget)
    {
        _actorLookAtService.SetTargetType(GameObject, lookAtTarget);
    }

    public LookAtDataHolder? GetData()
    {
        return _actorLookAtService.GetTargetDataHolder(GameObject);
    }

    public static ActorDynamicPoseCapability? CreateIfEligible(IServiceProvider provider, ActorEntity entity)
    {
        if(entity.GameObject is IBattleChara)
            return ActivatorUtilities.CreateInstance<ActorDynamicPoseCapability>(provider, entity);

        return null;
    }

}
