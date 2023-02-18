using System;

namespace Brio.Core;

public interface IService : IDisposable
{
    void AssignInstance();
    void ClearInstance();
    void Start();
    void Tick(float delta);
    void Stop();
}
