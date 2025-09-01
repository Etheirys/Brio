using Brio.Entities.Actor;
using Brio.Game.Actor;
using Brio.Game.GPose;
using Brio.UI.Widgets.Actor;

namespace Brio.Capabilities.Actor;
public class ActorDynamicPoseCapability : ActorCharacterCapability
{
    private readonly ActorLookAtService _actorLookAtService;
    private readonly ActorAppearanceService _actorAppearanceService;
    private readonly GPoseService _gposeService;

    public ActorDynamicPoseCapability(ActorEntity parent, ActorLookAtService actorLookAtService, ActorAppearanceService actorAppearanceService, GPoseService gPoseService) : base(parent)
    {
        _actorLookAtService = actorLookAtService;
        _actorAppearanceService = actorAppearanceService;
        _gposeService = gPoseService;

        Widget = new ActorDynamicPoseWidget(this);
    }

    public unsafe void TESTactorlook()
    {
        _actorLookAtService.TESTactorlook(GameObject);
    }

    public unsafe void TESTactorlookClear()
    {
        _actorLookAtService.TESTactorlookClear(GameObject);
    }

}
