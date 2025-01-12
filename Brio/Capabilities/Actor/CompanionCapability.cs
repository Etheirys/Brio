using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Types;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Capabilities.Actor;

internal unsafe class CompanionCapability : ActorCharacterCapability
{
    public ModeType Mode { get; }

    private readonly ActorSpawnService _actorSpawnService;


    public CompanionCapability(ActorEntity parent, ModeType mode, ActorSpawnService actorSpawnService) : base(parent)
    {
        Mode = mode;
        _actorSpawnService = actorSpawnService;

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
