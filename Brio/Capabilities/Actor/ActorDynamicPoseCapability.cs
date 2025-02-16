using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.GPose;
using Brio.UI.Widgets.Actor;

namespace Brio.Capabilities.Actor;
public class ActorDynamicPoseCapability : ActorCharacterCapability
{
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly GPoseService _gposeService;

    public ActorDynamicPoseCapability(ActorEntity parent, ActorAppearanceService actorAppearanceService, GPoseService gPoseService) : base(parent)
    {
        _actorAppearanceService = actorAppearanceService;
        _gposeService = gPoseService;

        Widget = new ActorDynamicPoseWidget(this);
    }

    public unsafe void TESTactorlook()
    {
        _actorAppearanceService.TESTactorlook(GameObject);
    }

    public unsafe void TESTactorlookClear()
    {
        _actorAppearanceService.TESTactorlookClear(GameObject);
    }

}
