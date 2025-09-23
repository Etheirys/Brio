using Brio.Capabilities.Core;
using Brio.Game.Actor;
using Dalamud.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Brio.Entities.Core;

public abstract class Entity : IDisposable
{
    public EntityId Id { get; private set; }

    public Entity? Parent { get; protected set; }

    public SpawnFlags SpawnFlag { get; protected set; }

    public IReadOnlyCollection<Entity> Children => _children.AsReadOnly();

    public IReadOnlyList<Capability> Capabilities => _capabilities.Values.ToList().AsReadOnly();

    string name = "";
    public virtual string FriendlyName
    {
        get
        {
            if(string.IsNullOrEmpty(name))
            {
                return Id.Unique;
            }

            return name;
        }
        set
        {
            name = value;
        }
    }


    public virtual FontAwesomeIcon Icon => FontAwesomeIcon.Question;
    public virtual bool IsVisible => true;
    public virtual bool IsAttached => Parent != null;
    public virtual EntityFlags Flags => EntityFlags.DefaultOpen;

    public virtual int ContextButtonCount => 0;

    public virtual bool IsLoading { get; set; } = false;
    public virtual bool IsLocked { get; set; } = false;

    public virtual string LoadingDescription { get; set; } = string.Empty;

    public virtual bool IsDisabled { get; set; } = false;

    protected List<Entity> _children = [];

    protected IServiceProvider _serviceProvider;

    private readonly Dictionary<Type, Capability> _capabilities = [];

    public Entity(EntityId id, IServiceProvider serviceProvider, IEnumerable<Entity>? children = null)
    {
        Id = id;

        _serviceProvider = serviceProvider;

        if(children != null)
            foreach(var child in children)
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
        if(_children.Contains(child))
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

    public virtual void OnSelected()
    {
        foreach(Capability capability in Capabilities)
        {
            capability.OnEntitySelected();
        }
    }

    public virtual void OnDeselected()
    {
        foreach(Capability capability in Capabilities)
        {
            capability.OnEntityDeselected();
        }
    }

    public virtual void OnDoubleClick()
    {

    }

    public virtual void DrawContextButton()
    {

    }

    public void AddCapability<T>(T? capability) where T : Capability
    {
        if(capability == null)
            return;

        _capabilities[capability.GetType()] = capability;
    }

    public void RemoveCapability<T>() where T : Capability
    {
        _capabilities.Remove(typeof(T));
    }

    public void ClearCapabilities()
    {
        foreach(var capability in _capabilities.Values)
            capability.Dispose();

        _capabilities.Clear();
    }

    public bool HasCapability<T>() where T : Capability
    {
        return _capabilities.ContainsKey(typeof(T));
    }

    public T GetCapability<T>() where T : Capability
    {
        if(TryGetCapability<T>(out var capability))
            return capability;

        throw new InvalidOperationException($"Entity {Id} does not have capability {typeof(T)}");
    }

    public bool TryGetCapability<T>([MaybeNullWhen(false)] out T capability, bool considerChildren = false, bool considerParents = false) where T : Capability
    {
        capability = null;

        if(TryGetCapabilities<T>(out var capabilities, considerChildren, considerParents))
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

        if(_capabilities.TryGetValue(typeof(T), out var cap))
        {
            results.Add((T)cap);
        }

        if(considerChildren)
        {
            foreach(var child in Children)
            {
                if(child.TryGetCapabilities<T>(out var childCaps, true, false))
                {
                    results.AddRange(childCaps);
                }
            }
        }

        if(considerParents && Parent != null)
        {
            if(Parent.TryGetCapabilities<T>(out var parentCaps, false, true))
            {
                results.AddRange(parentCaps);
            }
        }

        return results.Count != 0;
    }

    public bool SetSpawnFlags(SpawnFlags flags)
    {
        if(SpawnFlag is SpawnFlags.None)
        {
            SpawnFlag = flags;
            return true;
        }
        return false;
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
        if(obj is Entity ent)
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
public enum EntityFlags
{
    None,
    DefaultOpen = 1 << 0,
    HasContextButton = 1 << 1,
    AllowOutsideGpose = 1 << 2,
    AllowDoubleClick = 1 << 3,
    AllowMultiSelect = 1 << 4,
}
