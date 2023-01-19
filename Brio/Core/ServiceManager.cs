using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Core;

public class ServiceManager : IDisposable
{
    public bool IsStarted { get; private set; } = false;

    private readonly List<Type> _toRegister = new();

    private readonly List<IService> _services = new();

    public void Add<T> () where T : IService
    {
        _toRegister.Add(typeof(T));
    }

    public void Start()
    {
        if (IsStarted)
            return;

        IsStarted = true;

        foreach(var newType in _toRegister)
        {
            var service = (IService?) Activator.CreateInstance(newType);
            if(service != null)
                _services.Add(service);
        }

        _toRegister.Clear();

        foreach (var service in _services)
        {
            service.Start();
        }
    }

    public void Tick()
    {
        if (!IsStarted)
            return;

        foreach(var service in _services)
            service.Tick();
    }

    public void Dispose()
    {
        if (!IsStarted)
            return;

        var reversed = _services.ToList();
        reversed.Reverse();

        foreach (var service in reversed)
            service.Stop();

        foreach (var service in reversed)
            service.Dispose();

        _services.Clear();
    }
}
