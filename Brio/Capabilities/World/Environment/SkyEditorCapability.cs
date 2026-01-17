using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World;

namespace Brio.Capabilities.World;

public class SkyEditorCapability : Capability
{
    public EnvironmentService Environment => _environmentService;

    public readonly EnvironmentService _environmentService;

    public SkyEditorCapability(Entity parent, EnvironmentService weatherService) : base(parent)
    {
        _environmentService = weatherService;

        Widget = new SkyEditorWidget(this);
    }
}
