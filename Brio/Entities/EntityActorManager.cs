using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Entities;

internal unsafe class EntityActorManager : IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ObjectMonitorService _monitorService;
    private readonly IObjectTable _objects;
    private readonly IFramework _framework;

    private readonly ActorContainerEntity _actorContainerEntity;

    public EntityActorManager(EntityManager entityManager, IServiceProvider serviceProvider, ObjectMonitorService monitorService, IObjectTable objects, IFramework framework)
    {
        _entityManager = entityManager;
        _serviceProvider = serviceProvider;
        _monitorService = monitorService;
        _objects = objects;
        _framework = framework;

        _monitorService.CharacterInitialized += OnCharacterInitialized;
        _monitorService.CharacterDestroyed += OnCharacterDestroyed;

        _actorContainerEntity = ActivatorUtilities.CreateInstance<ActorContainerEntity>(_serviceProvider);
    }

    public void AttachContainer()
    {
        _entityManager.AttachEntity(_actorContainerEntity, null);

        PopulateExistingActors();
    }

    private void PopulateExistingActors()
    {
        foreach(var go in _objects)
        {
            AttachActor(go, _actorContainerEntity);
        }
    }

    private void AttachActor(IGameObject go, Entity parent)
    {
        if(_entityManager.TryGetEntity(new EntityId(go), out var entity))
        {
            // Already attached to the correct parent
            if(parent.Equals(entity.Parent))
                return;
        }
        else
        {
            // Only characters
            if(!go.Native()->IsCharacter())
                return;

            // TODO: We should allow manipulation of overworld actors too
            if(!go.IsGPose())
                return;

            entity = ActivatorUtilities.CreateInstance<ActorEntity>(_serviceProvider, go);
        }

        _entityManager.AttachEntity(entity, parent, true);


        // This is ew, but we need to handle companions here for now.
        // This would be a stack overflow but the parenting check above prevents it.
        HandleCompanions(entity, true);
    }

    private void DetachActor(IGameObject actor)
    {
        if(_entityManager.TryGetEntity(new EntityId(actor), out var entity))
        {
            _entityManager.DetachEntity(entity, true);
        }
    }

    private void HandleCompanions(Entity entity, bool checkParent)
    {
        if(entity is ActorEntity actorEntity)
        {
            var currentActor = actorEntity.GameObject;

            if(currentActor is ICharacter character)
            {
                if(character.HasSpawnedCompanion())
                {
                    var companion = character.Native()->CompanionObject;
                    if(companion != null)
                    {
                        var companionObject = _objects.CreateObjectReference((nint)companion);
                        if(companionObject != null)
                        {
                            AttachActor(companionObject, entity);
                        }
                    }
                    return;
                }

                if(checkParent)
                {
                    var maybeParentId = currentActor.ObjectIndex - 1;
                    if(maybeParentId < 0)
                        return;

                    var maybeParent = _objects[maybeParentId];
                    if(maybeParent == null)
                        return;

                    _entityManager.TryGetEntity(new EntityId(maybeParent), out var maybeParentEntity);

                    if(maybeParentEntity == null)
                        return;

                    HandleCompanions(maybeParentEntity, false);
                }
            }
        }
    }

    private void OnCharacterDestroyed(NativeCharacter* chara)
    {
        var go = _objects.CreateObjectReference((nint)chara);
        if(go != null)
            DetachActor(go);
    }

    private void OnCharacterInitialized(NativeCharacter* chara)
    {
        // We wait for one frame on create to ensure that the actor is fully initialized
        _framework.RunOnTick(() =>
        {
            var go = _objects.CreateObjectReference((nint)chara);
            if(go != null)
                AttachActor(go, _actorContainerEntity);
        });
    }


    public void Dispose()
    {
        _monitorService.CharacterInitialized -= OnCharacterInitialized;
        _monitorService.CharacterDestroyed -= OnCharacterDestroyed;
    }
}
