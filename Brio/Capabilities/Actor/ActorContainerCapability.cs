using Brio.Capabilities.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;

namespace Brio.Capabilities.Actor;

internal class ActorContainerCapability : Capability
{
    private readonly EntityManager _entityManager;
    private readonly ActorSpawnService _actorSpawnService;
    private readonly TargetService _targetService;
    private readonly GPoseService _gPoseService;

    public bool CanControlCharacters => _gPoseService.IsGPosing;

    public ActorContainerCapability(ActorContainerEntity parent, EntityManager entityManager, ActorSpawnService actorSpawnService, TargetService targetService, GPoseService gPoseService) : base(parent)
    {
        _entityManager = entityManager;
        _actorSpawnService = actorSpawnService;
        _targetService = targetService;
        _gPoseService = gPoseService;
        Widget = new ActorContainerWidget(this);
    }

    public void SelectActorInHierarchy(ActorEntity entity)
    {
        _entityManager.SetSelectedEntity(entity);
    }

    public void CreateCharacter(bool enableAttachments, bool targetNewInHierarchy, bool forceSpawnActorWithoutCompanion = false)
    {
        SpawnFlags flags = SpawnFlags.Default;
        if(enableAttachments)
            flags |= SpawnFlags.ReserveCompanionSlot;

        if(_actorSpawnService.CreateCharacter(out var chara, flags, disableSpawnCompanion: forceSpawnActorWithoutCompanion))
        {
            if(targetNewInHierarchy)
            {
                _entityManager.SetSelectedEntity(chara);
            }
        }
    }

    public void DestroyCharacter(ActorEntity entity)
    {
        _actorSpawnService.DestroyObject(entity.GameObject);
    }

    public void CloneActor(ActorEntity entity, bool targetNewInHierarchy)
    {
        if(entity.GameObject is Character character)
        {
            if(_actorSpawnService.CloneCharacter(character, out var chara))
            {
                if(targetNewInHierarchy)
                {
                    _entityManager.SetSelectedEntity(chara);
                }
            }
        }
    }

    public void DestroyAll()
    {
        _actorSpawnService.ClearAll();
    }

    public void Target(ActorEntity entity)
    {
        _targetService.GPoseTarget = entity.GameObject;
    }

    public void SelectInHierarchy(ActorEntity entity)
    {
        _entityManager.SetSelectedEntity(entity);
    }
}
