using System;

namespace Brio.Core;

public interface IService : IDisposable
{
    void Start();
    void Tick();
    void Stop();
}
