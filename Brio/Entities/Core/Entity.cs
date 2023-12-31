using Dalamud.Interface;
using Brio.Capabilities.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Brio.Entities.Core;

internal abstract class Entity : IDisposable
{
    public EntityId Id { get; private set; }

    public Entity? Parent { get; protected set; }

    public IReadOnlyCollection<Entity> Children => _children.AsReadOnly();

    public IReadOnlyList<Capability> Capabilities => _capabilities.Values.ToList().AsReadOnly();

    public virtual string FriendlyName => Id.Unique;

    public virtual FontAwesomeIcon Icon => FontAwesomeIcon.Question;
    public virtual bool IsVisible => true;
    public virtual bool IsAttached => Parent != null;
    public virtual EntityFlags Flags => EntityFlags.DefaultOpen;


    protected List<Entity> _children = [];

    protected IServiceProvider _serviceProvider;

    private readonly Dictionary<Type, Capability> _capabilities = [];

    public Entity(EntityId id, IServiceProvider serviceProvider, IEnumerable<Entity>? children = null)
    {
        Id = id;
        _serviceProvider = serviceProvider;

        if (children != null)
            foreach (var child in children)
                AddChild(child);
    }

    public void AddChild(Entity child)
    {
        child.Parent = this;
        _children.Add(child);
        OnChildAttached();
    }

    public void RemoveChild(Entity child)
    {
        OnChildDetached();
        child.Parent = null;
        _children.Remove(child);
    }

    public virtual void OnAttached()
    {

    }

    public virtual void OnDetached()
    {
        ClearCapabilities();
    }

    public virtual void OnChildAttached()
    {

    }

    public virtual void OnChildDetached()
    {

    }

    public void AddCapability<T>(T? capability) where T : Capability
    {
        if (capability == null)
            return;

        _capabilities[capability.GetType()] = capability;
    }

    public void RemoveCapability<T>() where T : Capability
    {
        _capabilities.Remove(typeof(T));
    }

    public void ClearCapabilities()
    {
        foreach (var capability in _capabilities.Values)
            capability.Dispose();

        _capabilities.Clear();
    }

    public bool HasCapability<T>() where T : Capability
    {
        return _capabilities.ContainsKey(typeof(T));
    }

    public T GetCapability<T>() where T : Capability
    {
        if (TryGetCapability<T>(out var capability))
            return capability;

        throw new InvalidOperationException($"Entity {Id} does not have capability {typeof(T)}");
    }

    public bool TryGetCapability<T>([MaybeNullWhen(false)] out T capability, bool considerChildren = false, bool considerParents = false) where T : Capability
    {
        capability = null;

        if (TryGetCapabilities<T>(out var capabilities, considerChildren, considerParents))
        {
            capability = capabilities.First();
            return true;
        }

        return false;
    }

    public bool TryGetCapabilities<T>([MaybeNullWhen(false)] out IEnumerable<T> capabilities, bool considerChildren = false, bool considerParents = false) where T : Capability
    {
        var results = new List<T>();
        capabilities = results;

        if (_capabilities.TryGetValue(typeof(T), out var cap))
        {
            results.Add((T)cap);
        }

        if (considerChildren)
        {
            foreach (var child in Children)
            {
                if (child.TryGetCapabilities<T>(out var childCaps, true, false))
                {
                    results.AddRange(childCaps);
                }
            }
        }

        if (considerParents && Parent != null)
        {
            if (Parent.TryGetCapabilities<T>(out var parentCaps, false, true))
            {
                results.AddRange(parentCaps);
            }
        }

        return results.Any();
    }

    public virtual void Dispose()
    {
        ClearCapabilities();
    }

    public override string ToString()
    {
        return $"{Id}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is Entity ent)
            return Id == ent.Id;

        return false;
    }

    public static bool operator ==(Entity? a, Entity? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Entity? a, Entity? b) => !(a == b);

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

[Flags]
internal enum EntityFlags
{
    None,
    DefaultOpen = 1 << 0,
}