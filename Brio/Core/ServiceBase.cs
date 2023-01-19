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

    public virtual void Start()
    {
        _instance = (T)this;
    }

    public virtual void Tick() { }

    public virtual void Stop() { }

    public virtual void Dispose() { }
}
