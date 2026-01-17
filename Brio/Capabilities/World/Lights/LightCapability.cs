using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Game.World;
using System;

namespace Brio.Capabilities.World;

public class LightCapability(Entity parent) : Capability(parent), IDisposable
{
    public LightEntity Light => (LightEntity)Entity;
    public IGameLight GameLight => Light.GameLight;
}
