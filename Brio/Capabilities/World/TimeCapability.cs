using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World;

namespace Brio.Capabilities.World;

public class TimeCapability : Capability
{
    public TimeService TimeService { get; }



    public TimeCapability(Entity parent, TimeService timeService) : base(parent)
    {
        TimeService = timeService;
        Widget = new TimeWidget(this);
    }
}
