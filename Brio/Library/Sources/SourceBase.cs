using Dalamud.Interface.Internal;
using System;

namespace Brio.Library.Sources;

internal abstract class SourceBase : GroupEntryBase
{
    public SourceBase()
        : base(null)
    {
    }

    public abstract string Description { get; }
    public abstract void Scan();
}
