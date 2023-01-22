using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Core;

public class ServiceManager : IDisposable
{
    public bool IsStarted { get; private set; } = false;

    private readonly List<IService> _services = new();

    public void Add<T>() where T : IService
    {
        var newType = typeof(T);

        var service = (IService?)Activator.CreateInstance(newType);
        if(service != null)
            _services.Add(service);
    }

    public void Start()
    {
        if(IsStarted)
            throw new Exception("Services already running");

        foreach(var service in _services)
            service.Start();

        IsStarted = true;
    }

    public void Tick()
    {
        if(!IsStarted)
            return;

        foreach(var service in _services)
            service.Tick();
    }

    public void Dispose()
    {
        var reversed = _services.ToList();
        reversed.Reverse();

        if(IsStarted)
            foreach(var service in reversed)
                service.Stop();

        IsStarted = false;

        foreach(var service in reversed)
            service.Dispose();

        _services.Clear();

    }
}
