using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World;

namespace Brio.Capabilities.World;

public class EnvironmentEditorCapability : Capability
{
    public EnvironmentService Environment { get; }

    public EnvironmentEditorCapability(Entity parent, EnvironmentService weatherService) : base(parent)
    {
        Environment = weatherService;

        Widget = new EnvironmentEditorWidget(this);
    }

}
