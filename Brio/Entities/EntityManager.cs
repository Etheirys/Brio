using Brio.Capabilities.Core;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Entities.Debug;
using Brio.Entities.World;
using Dalamud.Game.ClientState.Objects.Types;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Brio.Entities;

public partial class EntityManager(IServiceProvider serviceProvider, ConfigurationService configurationService) : IDisposable
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Entity? RootEntity => _worldEntity;
    private WorldEntity? _worldEntity;

    public Entity? DebugEntity => _debugEntity;
    private DebugEntity? _debugEntity;

    public EntityManagerContainer EntityManagerContainer => _entityContainerEntity!;
    private EntityManagerContainer? _entityContainerEntity;

    public IReadOnlyList<EntityId> SelectedEntitys => _selectedEntities.AsReadOnly();
    private readonly List<EntityId> _selectedEntities = [];

    public EntityId? SelectedEntityById => _selectedEntities.Count > 0 ? _selectedEntities[0] : (EntityId?)null;
    public Entity? SelectedEntity
    {
        get
        {
            if(SelectedEntityById.HasValue)
            {
                if(TryGetEntity(SelectedEntityById.Value, out var entity))
                {
                    return entity;
                }
            }
            return null;
        }
    }

    public HashSet<TransformableEntity> TransformableEntities => _transformableEntities;

    private readonly HashSet<TransformableEntity> _transformableEntities = [];
    private readonly Dictionary<EntityId, Entity> _entityMap = [];

    private readonly ConfigurationService _configurationService = configurationService;

    public void SetupDefaultEntities()
    {
        _worldEntity = ActivatorUtilities.CreateInstance<WorldEntity>(_serviceProvider);
        _entityMap[_worldEntity.Id] = _worldEntity;

        RefreshDebugEntity();

        var environmentEntity = ActivatorUtilities.CreateInstance<EnvironmentContainerEntity>(_serviceProvider);
        AttachEntity(environmentEntity, _worldEntity);

        _entityContainerEntity = ActivatorUtilities.CreateInstance<EntityManagerContainer>(_serviceProvider);
        AttachEntity(_entityContainerEntity, _worldEntity);

        var defaultCameraEntity = ActivatorUtilities.CreateInstance<CameraEntity>(_serviceProvider, 0, CameraType.Default);
        defaultCameraEntity.VirtualCamera.SaveCameraState();
        AttachEntity(defaultCameraEntity, _entityContainerEntity);
    }

    public T CreateEntityOnEntityContainer<T>(params object[] args) where T : Entity
    {
        var entity = ActivatorUtilities.CreateInstance<T>(_serviceProvider, args);
        AttachEntity(entity, _entityContainerEntity);
        return entity;
    }

    public void RemoveEntityFromEntityContainer(Entity entity, bool dispose = false)
    {
        DetachEntity(entity, dispose);
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

        if(entity is TransformableEntity transformableEntity)
            _transformableEntities.Add(transformableEntity);

        parent?.AddChild(entity);
        entity.OnAttached();
    }

    public void DetachEntity(Entity entity, bool dispose)
    {
        entity.OnDetached();

        if(entity is TransformableEntity transformableEntity)
            _transformableEntities.Remove(transformableEntity);

        _entityMap.Remove(entity.Id);
        entity.Parent?.RemoveChild(entity);
        if(dispose)
        {
            foreach(var child in entity.Children)
                DetachEntity(child, true);

            entity.Dispose();
        }
    }

    public bool MoveEntity(Entity entity, Entity newParent)
    {
        if(entity == newParent || entity == RootEntity)
            return false;

        var ancestor = newParent;
        while(ancestor != null)
        {
            if(ancestor == entity)
                return false;
            ancestor = ancestor.Parent;
        }

        entity.Parent?.RemoveChild(entity);
        newParent.AddChild(entity);
        return true;
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
        if(id.HasValue == false)
            return;

        foreach(var eId in _selectedEntities)
        {
            if(TryGetEntity(eId, out var ent))
            {
                ent.OnDeselected();
            }
        }

        _selectedEntities.Clear();
        _selectedEntities.Add(id.Value);

        SelectedEntity?.OnSelected();
    }

    public void SetSelectedEntity(IGameObject go)
    {
        SetSelectedEntity(new EntityId(go));
    }

    public void AddSelectedEntity(EntityId id)
    {
        if(_selectedEntities.Contains(id))
            return;

        if(TryGetEntity(id, out var ent))
        {
            _selectedEntities.Add(id);
            ent.OnSelected();
        }
    }

    public void RemoveSelectedEntity(EntityId id)
    {
        if(_selectedEntities.Contains(id))
        {
            if(TryGetEntity(id, out var ent))
                ent.OnDeselected();

            _selectedEntities.Remove(id);
        }
    }

    public void ClearSelectedEntities()
    {
        foreach(var eId in _selectedEntities.ToArray())
        {
            if(TryGetEntity(eId, out var ent))
                ent.OnDeselected();
        }
        _selectedEntities.Clear();
    }

    public bool SelectedHasCapability<T>(bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        return TryGetCapabilitiesFromSelectedEntities<T>(out _, considerChildren, considerParents);
    }

    public List<(EntityId id, PosingCapability capability, Transform transform)> GetAllSelectedActors()
    {
        var result = new List<(EntityId, PosingCapability, Transform)>();
        foreach(var id in _selectedEntities)
        {
            if(!TryGetEntity(id, out var entity))
                continue;

            if(entity.TryGetCapability<PosingCapability>(out var posingCap, false, true))
            {
                result.Add((posingCap.Actor, posingCap, posingCap.ModelPosing.Transform));
            }
        }
        return result;
    }

    public List<(EntityId id, ITransformable target, Transform snapshot)> GetAllSelectedTransformables()
    {
        var result = new List<(EntityId, ITransformable, Transform)>();
        foreach(var id in _selectedEntities)
        {
            if(!TryGetEntity(id, out var entity))
                continue;

            if(entity is ITransformable transformable)
                result.Add((id, transformable, transformable.Transform));
        }
        return result;
    }

    public IEnumerable<TransformableEntity> TryGetAllTransformableActors()
    {
        foreach(var entity in _entityMap.Values)
        {
            if(entity is TransformableEntity transformableEntity)
                yield return transformableEntity;
        }
    }

    public IEnumerable<ActorEntity> TryGetAllActors()
    {
        foreach(var entity in _entityMap.Values)
        {
            if(entity is ActorEntity actor)
            {
                if(actor.IsProp == false)
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

    public bool TryGetCapabilitiesFromSelectedEntities<T>([MaybeNullWhen(false)] out IEnumerable<T> capabilities, bool considerChildren = false, bool considerParents = true) where T : Capability
    {
        var results = new List<T>();
        capabilities = results;

        foreach(var id in _selectedEntities)
        {
            if(!TryGetEntity(id, out var entity))
                continue;

            if(!entity.IsAttached)
                continue;

            if(entity.TryGetCapabilities<T>(out var caps, considerChildren, considerParents))
                results.AddRange(caps);
        }

        return results.Count != 0;
    }

    private void RefreshDebugEntity()
    {
        if(_configurationService.IsDebug)
        {
            _debugEntity = ActivatorUtilities.CreateInstance<DebugEntity>(_serviceProvider);
            _debugEntity.OnAttached();

            _entityMap[Debug.DebugEntity.FixedId] = _debugEntity;
        }
        else
        {
            if(TryGetEntity(Debug.DebugEntity.FixedId, out var entity))
            {
                _entityMap.Remove(Debug.DebugEntity.FixedId);
                _debugEntity = null;
            }
        }
    }

    public void Dispose()
    {
        foreach(var entity in _entityMap.Values)
            entity.Dispose();
    }
}
