using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;

namespace Brio.Capabilities.Actor;

internal class ActorLifetimeCapability : ActorCapability
{
    private readonly TargetService _targetService;

    private readonly ActorSpawnService _actorSpawnService;
    private readonly EntityManager _entityManager;

    public ActorLifetimeCapability(ActorEntity parent, TargetService targetService, ActorSpawnService actorSpawnService, EntityManager entityManager) : base(parent)
    {
        _targetService = targetService;
        _actorSpawnService = actorSpawnService;
        _entityManager = entityManager;
        Widget = new ActorLifetimeWidget(this);
    }

    public void Target()
    {
        _targetService.GPoseTarget = GameObject;
    }

    public bool CanClone => Actor.Parent is ActorContainerEntity && GameObject is Character;

    public void SpawnNewActor(bool selectInHierarchy, bool spawnCompanion, bool disableSpawnCompanion)
    {
        SpawnFlags flags = SpawnFlags.Default;
        if(spawnCompanion)
        {
            flags |= SpawnFlags.ReserveCompanionSlot;
        }

        if(_actorSpawnService.CreateCharacter(out Character? chara, flags, disableSpawnCompanion))
        {
            if(selectInHierarchy)
            {
                _entityManager.SetSelectedEntity(chara);
            }
        }
    }

    public void Clone(bool selectInHierarchy)
    {
        if(!CanClone)
            return;

        if(_actorSpawnService.CloneCharacter((Character)GameObject, out var chara))
        {
            if(selectInHierarchy)
            {
                _entityManager.SetSelectedEntity(chara);
            }
        }
    }

    public bool CanDestroy =>
        Actor.Parent is ActorContainerEntity ||
        (Actor.Parent is ActorEntity parentEntity && parentEntity.GameObject is Character character && character.HasSpawnedCompanion());

    public void Destroy()
    {
        if(!CanDestroy)
            return;

        if(Actor.Parent is ActorContainerEntity)
            _actorSpawnService.DestroyObject(GameObject);
        else if(Actor.Parent is ActorEntity actorEntity)
            _actorSpawnService.DestroyCompanion((Character)((ActorEntity)Actor.Parent).GameObject);
    }

}
