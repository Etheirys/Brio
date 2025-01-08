using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Brio.Game.Posing;
using Brio.Resources;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Swan.Formatters;

namespace Brio.Capabilities.Actor;

internal class ActorLifetimeCapability : ActorCapability
{
    private readonly TargetService _targetService;

    private readonly ActorSpawnService _actorSpawnService;
    private readonly EntityManager _entityManager;
    private readonly IFramework _framework;
    private readonly PosingService _posingService;
    public ActorLifetimeCapability(ActorEntity parent, PosingService posingService, TargetService targetService, ActorSpawnService actorSpawnService, EntityManager entityManager, IFramework framework) : base(parent)
    {
        _targetService = targetService;
        _actorSpawnService = actorSpawnService;
        _entityManager = entityManager;
        _framework = framework;
        _posingService = posingService;

        Widget = new ActorLifetimeWidget(this);
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

    public unsafe void SpawnNewProp(bool selectInHierarchy)
    {
        if(_actorSpawnService.CreateCharacter(out ICharacter? chara, SpawnFlags.AsProp | SpawnFlags.CopyPosition, true))
        {
            if(selectInHierarchy)
            {
                _entityManager.SetSelectedEntity(chara);
            }

            _framework.RunUntilSatisfied(
            () => chara.Native()->IsReadyToDraw(),
            (__) =>
            {
                var entity = _entityManager.GetEntity(chara.Native());
                if(entity is not null)
                {
                    entity.GetCapability<ActionTimelineCapability>().SetOverallSpeedOverride(0);
                    entity.GetCapability<PosingCapability>().ImportPose(JsonSerializer.Deserialize<PoseFile>(ResourceProvider.Instance.GetRawResourceString("Data.BrioPropPose.pose")), _posingService.SceneImporterOptions);
                    entity.GetCapability<ActorAppearanceCapability>().AttachWeapon();
                }
            },
                100,
                dontStartFor: 2
            );
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
