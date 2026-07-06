using Brio.Entities;
using Brio.Entities.Core;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.UI.Widgets.Core;

namespace Brio.Capabilities.Core;

public class EntitManagerCapability : Capability
{
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly ObjectMonitorService _objectMonitorService;

    public bool CanControlCharacters => _gPoseService.IsGPosing;

    public EntitManagerCapability(Entity parent, EntityManager entityManager, GPoseService gPoseService, ObjectMonitorService objectMonitorService) : base(parent)
    {
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _objectMonitorService = objectMonitorService;

        Widget = new EntityManagerWidget(this);
    }
}
