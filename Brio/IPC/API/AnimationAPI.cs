using Brio.API.Interface;
using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Objects.Types;

namespace Brio.IPC.API;

public unsafe class AnimationAPI(GPoseService gPoseService, EntityManager entityManager) : IAnimation
{
    private readonly GPoseService _gPoseService = gPoseService;
    private readonly EntityManager _entityManager = entityManager;

    public bool SetActorAnimation(IGameObject gameObject, string animationID, bool playOnLoad)
    {
        return false;
    }

    public bool FreezeActor(IGameObject gameObject)
    {
        if(_gPoseService.IsGPosing == false) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ActionTimelineCapability>(out var actionTimeline))
            {
                actionTimeline.StopSpeedAndResetTimeline();
                return true;
            }
        }

        return false;
    }

    public bool UnFreezeActor(IGameObject gameObject)
    {
        if(_gPoseService.IsGPosing == false) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ActionTimelineCapability>(out var actionTimeline))
            {
                actionTimeline.SetOverallSpeedOverride(1);
                return true;
            }
        }

        return false;
    }

    public float GetActorSpeed(IGameObject gameObject)
    {
        if(_gPoseService.IsGPosing == false) return 0;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ActionTimelineCapability>(out var actionTimeline))
            {
                return actionTimeline.SpeedMultiplier;
            }
        }

        return 0;
    }

    public bool SetActorSpeed(IGameObject gameObject, float speed)
    {
        if(_gPoseService.IsGPosing == false) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ActionTimelineCapability>(out var actionTimeline))
            {
                actionTimeline.SetOverallSpeedOverride(speed);
                return true;
            }
        }

        return false;
    }
}
