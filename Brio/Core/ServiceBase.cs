using System;

namespace Brio.Core;

public abstract class ServiceBase<T> : IService
    where T : ServiceBase<T>
{
    private static T? _instance;

    public static T Instance
    {
        get
        {
            if(_instance == null)
                throw new Exception($"No service found: {typeof(T)}");

            return _instance;
        }
    }

    public void AssignInstance() => _instance = (T?)this;
    public void ClearInstance() => _instance = null;

    public virtual void Start()
    {
        _instance = (T)this;
    }

    public virtual void Tick(float delta) { }

    public virtual void Stop() { }

    public virtual void Dispose() { }
}
