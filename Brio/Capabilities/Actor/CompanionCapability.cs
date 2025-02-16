using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Types;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Capabilities.Actor;

public unsafe class CompanionCapability : ActorCharacterCapability
{
    public ModeType Mode { get; }

    private readonly ActorSpawnService _actorSpawnService;
    private readonly EntityManager _entityManager;

    public CompanionCapability(ActorEntity parent, ModeType mode, ActorSpawnService actorSpawnService, EntityManager entityManager) : base(parent)
    {
        _actorSpawnService = actorSpawnService;
        _entityManager = entityManager;

        Mode = mode;
        Widget = new CompanionWidget(this);
    }

    public void DestroyCompanion()
    {
        SetCompanion(CompanionContainer.None);
    }

    public void SetCompanion(CompanionContainer container)
    {
        _actorSpawnService.CreateCompanion(Character, container);
    }

    public unsafe Entities.Core.Entity? GetCompanionAsEntity()
    {
        if(Character.HasSpawnedCompanion())
        {
            if(_entityManager.TryGetEntity(&Character.Native()->CompanionObject->Character.GameObject, out Entities.Core.Entity? actor))
            {
                return actor;
            }
        }

        return null;
    }

    public static CompanionCapability? CreateIfEligible(IServiceProvider provider, ActorEntity entity)
    {
        if(entity.GameObject is ICharacter character && character.HasCompanionSlot())
            return ActivatorUtilities.CreateInstance<CompanionCapability>(provider, entity, ModeType.Owner);

        if(entity.Parent is ActorEntity parentEntity && parentEntity.GameObject is ICharacter parentCharacter && parentCharacter.HasCompanionSlot())
            return ActivatorUtilities.CreateInstance<CompanionCapability>(provider, parentEntity, ModeType.Companion);

        return null;
    }

    public enum ModeType
    {
        Owner,
        Companion
    }
}
