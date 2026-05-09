using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Core;

public interface ITransformable
{
    public bool TransformOverride { get; }
    public bool IsTransformFrozen { get; set; }

    public Transform Transform { get; set; }
    public Transform OriginalTransform { get; set; }

    public void Snapshot();
}

public abstract class TransformableEntity(EntityId entityId, IServiceProvider provider) : Entity(entityId, provider), ITransformable
{
    public ITransformable? Transformable { get; set; }

    public bool TransformOverride => Transformable?.TransformOverride ?? false;
    public bool IsTransformFrozen
    {
        get => Transformable?.IsTransformFrozen ?? false;
        set => Transformable?.IsTransformFrozen = value;
    }
    public Transform Transform
    {
        get => Transformable?.Transform ?? default;
        set => Transformable?.Transform = value;
    }
    public Transform OriginalTransform
    {
        get => Transformable?.OriginalTransform ?? default;
        set => Transformable?.OriginalTransform = value;
    }

    public abstract void Snapshot();

    public void AddTransformable<T>() where T : Capability, ITransformable
    {
        var cap = ActivatorUtilities.CreateInstance<T>(_serviceProvider, this);
        Transformable = cap;
        AddCapability(cap);
    }
}
