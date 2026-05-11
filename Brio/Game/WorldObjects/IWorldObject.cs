using Brio.Core;
using System;

namespace Brio.Game.WorldObjects;

public interface IWorldObject : ITransformable, IDisposable
{
    WorldObjectType ObjectType { get; }
    string FriendlyName { get; }

    nint Address { get; }
    bool IsValid { get; }
    bool IsVisible { get; set; }
    bool IsDirty { get; set; }

    int EntityIndex { get; }
    int Index { get; }

    string Path { get; }

    void SetIndex(int index);
    void SetEntityIndex(int index);
}

public abstract class WorldObjectBase : IWorldObject
{
    private int _index;
    private int _entityIndex;
    private Transform _currentTransform;

    public abstract WorldObjectType ObjectType { get; }
    public abstract string FriendlyName { get; }  
    public abstract nint Address { get; }
 
    public virtual bool IsValid => Address != nint.Zero;

    public virtual bool IsVisible { get; set; }
    public virtual bool IsDirty { get; set; }

    public virtual int Index => _index;
    public virtual int EntityIndex => _entityIndex;
    public virtual string Path { get; set; } = string.Empty;

    public virtual bool TransformOverride { get; }
    public virtual bool IsTransformFrozen { get; set; }
    public virtual Transform Transform { get => _currentTransform; set => _currentTransform = value; }
    public virtual Transform OriginalTransform { get; set; }

    public virtual void SetIndex(int index) => _index = index;
    public virtual void SetEntityIndex(int index) => _entityIndex = index;

    public abstract void Dispose();
    public abstract void Destroy();

    public abstract Transform GetTransform();
    public abstract void SetTransform(Transform transform);
    public virtual void Snapshot() { }
}
