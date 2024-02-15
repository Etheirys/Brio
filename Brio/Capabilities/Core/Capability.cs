using Brio.Entities.Core;
using Brio.UI.Widgets.Core;
using System;

namespace Brio.Capabilities.Core;

internal abstract class Capability(Entity parent) : IDisposable
{
    public Entity Entity { get; } = parent;

    public IWidget? Widget { get; protected set; }

    public virtual void OnEntitySelected()
    {
    }

    public virtual void OnEntityDeselected()
    {
    }

    public virtual void Dispose()
    {
        OnEntityDeselected();
    }
}
