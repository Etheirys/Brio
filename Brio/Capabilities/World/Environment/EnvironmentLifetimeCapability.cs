using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.UI.Widgets.World;

namespace Brio.Capabilities.World;

public class EnvironmentLifetimeCapability : LightCapability
{
    public readonly GPoseService GPoseService;
    public EnvironmentLifetimeCapability(Entity parent, GPoseService gPoseService) : base(parent)
    {
        GPoseService = gPoseService;
        this.Widget = new EnvLifetimeWidget(this);
    }
}
