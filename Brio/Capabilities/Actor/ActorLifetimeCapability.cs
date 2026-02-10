using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Capabilities.Posing;
using Brio.Game.Core;
using Brio.Game.World;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;

namespace Brio.Capabilities.Actor;

public class ActorLifetimeCapability : ActorCapability
{
    private readonly TargetService _targetService;

    private readonly ActorSpawnService _actorSpawnService;
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly EntityManager _entityManager;
    private readonly VirtualCameraManager _cameraManager;
    public ActorLifetimeCapability(ActorEntity parent, TargetService targetService, ActorAppearanceService actorAppearanceService, ActorSpawnService actorSpawnService, EntityManager entityManager, VirtualCameraManager cameraManager, LightingService lightingService) : base(parent)
    {
        _targetService = targetService;
        _actorSpawnService = actorSpawnService;
        _entityManager = entityManager;
        _actorAppearanceService = actorAppearanceService;
        _cameraManager = cameraManager;

        Widget = new ActorLifetimeWidget(this, actorSpawnService, cameraManager, lightingService);
    }

    public void MoveToCamera()
    {
        if(_cameraManager.CurrentCamera is null)
            return;

        var cam = _cameraManager.CurrentCamera;
        System.Numerics.Vector3 camPos;
        if(cam.IsFreeCamera)
        {
            camPos = cam.Position;
        }
        else
        {
            unsafe
            {
                camPos = cam.BrioCamera->Position;
            }
        }

        if(Actor.TryGetCapability<ModelPosingCapability>(out var modelPosing))
        {
            var t = modelPosing.Transform;
            t.Position = camPos;
            modelPosing.Transform = t;
        }
    }

    public void Target()
    {
        _targetService.GPoseTarget = GameObject;
    }

    public bool CanClone => Actor.Parent is ActorContainerEntity && GameObject is ICharacter;

    public void SpawnNewActor(bool selectInHierarchy, bool spawnCompanion, bool disableSpawnCompanion)
    {
        SpawnFlags flags = SpawnFlags.Default;
        if(spawnCompanion)
        {
            flags |= SpawnFlags.ReserveCompanionSlot;
        }

        if(_actorSpawnService.CreateCharacter(out ICharacter? chara, flags, disableSpawnCompanion))
        {
            if(selectInHierarchy)
            {
                _entityManager.SetSelectedEntity(chara);
            }
        }
    }

    public void SpawnNewProp(bool selectInHierarchy)
    {
        if(_actorSpawnService.SpawnNewProp(out ICharacter? character))
        {
            if(selectInHierarchy)
            {
                _entityManager.SetSelectedEntity(character!);
            }
        }
    }


    public void Clone(bool selectInHierarchy)
    {
        if(!CanClone)
            return;

        if(_actorSpawnService.CloneCharacter((ICharacter)GameObject, out var chara))
        {
            if(selectInHierarchy)
            {
                _entityManager.SetSelectedEntity(chara);
            }
        }
    }

    public bool CanDestroy =>
        Actor.Parent is ActorContainerEntity ||
        (Actor.Parent is ActorEntity parentEntity && parentEntity.GameObject is ICharacter character && character.HasSpawnedCompanion());

    public void Destroy()
    {
        if(!CanDestroy)
            return;

        if(Actor.Parent is ActorContainerEntity)
            _actorSpawnService.DestroyObject(GameObject);
        else if(Actor.Parent is ActorEntity actorEntity)
            _actorSpawnService.DestroyCompanion((ICharacter)((ActorEntity)Actor.Parent).GameObject);
    }

}
