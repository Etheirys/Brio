using Brio.Capabilities.Core;
using Brio.Config;
using Brio.Entities.Actor;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Entities.Debug;
using Brio.Entities.World;
using Dalamud.Game.ClientState.Objects.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Brio.Capabilities.Actor;
using FFXIVClientStructs.FFXIV.Client.Game.Object;


namespace Brio.Entities;

internal unsafe partial class EntityManager : IDisposable
{
    private readonly IServiceProvider _serviceProvider;

    public Entity? RootEntity => _worldEntity;

    public EntityId? SelectedEntityId { get; private set; }

    public Entity? SelectedEntity
    {
        get
        {
            if(SelectedEntityId.HasValue)
            {
                if(TryGetEntity(SelectedEntityId.Value, out var entity))
                {
                    return entity;
                }
            }
            return null;
        }
    }

    private WorldEntity? _worldEntity;
    private readonly Dictionary<EntityId, Entity> _entityMap = [];

    private readonly ConfigurationService _configurationService;

    public EntityManager(IServiceProvider serviceProvider, ConfigurationService configurationService)
    {
        _serviceProvider = serviceProvider;
        _configurationService = configurationService;
    }

    public void SetupDefaultEntities()
    {
        _worldEntity = ActivatorUtilities.CreateInstance<WorldEntity>(_serviceProvider);
        _entityMap[_worldEntity.Id] = _worldEntity;

        var environmentEntity = ActivatorUtilities.CreateInstance<EnvironmentEntity>(_serviceProvider);
        AttachEntity(environmentEntity, _worldEntity);

        var cameraEntity = ActivatorUtilities.CreateInstance<CameraEntity>(_serviceProvider);
        AttachEntity(cameraEntity, _worldEntity);

        RefreshDebugEntity();
    }

    public void AttachEntity(Entity entity, Entity? parent, bool autoDetach = false)
    {
        if(entity.Parent != null)
        {
            if(autoDetach)
            {
                DetachEntity(entity, false);
            }
            else
            {
                throw new InvalidOperationException("Entity is already attached to a parent.");
            }
        }

        parent ??= RootEntity;

        _entityMap[entity.Id] = entity;
        parent?.AddChild(entity);
        entity.OnAttached();
    }

    public void DetachEntity(Entity entity, bool dispose)
    {
        entity.OnDetached();
        _entityMap.Remove(entity.Id);
        entity.Parent?.RemoveChild(entity);
        if(dispose)
        {
            foreach(var child in entity.Children)
                DetachEntity(child, true);

            entity.Dispose();
        }
    }

    public bool TryGetEntity(EntityId id, [MaybeNullWhen(false)] out Entity entity)
    {
        return _entityMap.TryGetValue(id, out entity);
    }

    public bool TryGetEntity<T>(EntityId id, [MaybeNullWhen(false)] out T entity) where T : Entity
    {
        if(TryGetEntity(id, out var e))
        {
            if(e is T t)
            {
                entity = t;
                return true;
            }
        }
        entity = null;
        return false;
    }

    public Entity? GetEntity(EntityId id)
    {
        _entityMap.TryGetValue(id, out var entity);
        return entity;
    }
    
    public T? GetEntity<T>(EntityId id) where T : Entity
    {
        _entityMap.TryGetValue(id, out var entity);

        if(entity is T t)
        {
            return t;
        }
        return null;
    }

    public bool EntityExists(EntityId id)
    {
        return _entityMap.ContainsKey(id);
    }

    public void SetSelectedEntity(EntityId? id)
    {
        SelectedEntity?.OnDeselected();

        SelectedEntityId = id;

        SelectedEntity?.OnSelected();
    }

    public void SetSelectedEntity(IGameObject go)
    {
        SetSelectedEntity(new EntityId(go));
    }

    public bool SelectedHasCapability<T>(bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        return TryGetCapabilitiesFromSelectedEntity<T>(out _, considerChildren, considerParents);
    }

    public IEnumerable<ActorEntity> TryGetAllActors()
    {
        foreach(var entity in _entityMap.Values)
        {
            if(entity is ActorEntity actor)
            {
                yield return actor;
            }
        }
    }

    public IEnumerable<IGameObject> TryGetAllActorsAsGameObject()
    {
        foreach(var entity in _entityMap.Values)
        {
            if(entity is ActorEntity actor)
            {
                yield return actor.GameObject;
            }
        }
    }

    public bool TryGetCapabilityFromSelectedEntity<T>([MaybeNullWhen(false)] out T capability, bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        if(TryGetCapabilitiesFromSelectedEntity<T>(out var capabilities, considerChildren, considerParents))
        {
            capability = capabilities.First();
            return true;
        }
        capability = null;
        return false;
    }

    public bool TryGetCapabilitiesFromSelectedEntity<T>([MaybeNullWhen(false)] out IEnumerable<T> capabilities, bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        capabilities = null;

        var selected = SelectedEntity;
        if(selected != null)
        {
            if(!selected.IsAttached)
                return false;

            if(selected.TryGetCapabilities<T>(out capabilities, considerChildren, considerParents))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshDebugEntity()
    {
        if(_configurationService.IsDebug)
        {
            var debugEntity = ActivatorUtilities.CreateInstance<DebugEntity>(_serviceProvider);
            AttachEntity(debugEntity, _worldEntity);
        }
        else
        {
            if(TryGetEntity(DebugEntity.FixedId, out var entity))
            {
                DetachEntity(entity, true);
            }
        }
    }

    public void Dispose()
    {
        foreach(var entity in _entityMap.Values)
            entity.Dispose();
    }
}
