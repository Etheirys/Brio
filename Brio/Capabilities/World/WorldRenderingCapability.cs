using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World;

namespace Brio.Capabilities.World;

internal class WorldRenderingCapability : Capability
{
    public WorldRenderingService WorldRenderingService { get; }

    public WorldRenderingCapability(Entity parent, WorldRenderingService service) : base(parent)
    {
        WorldRenderingService = service;
        Widget = new WorldRenderingWidget(this);
    }
}
