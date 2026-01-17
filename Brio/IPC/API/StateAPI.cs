using Brio.API.Interface;
using Brio.Game.GPose;

namespace Brio.IPC.API;

public class StateAPI(GPoseService gPoseService) : IState
{

    private readonly GPoseService _gPoseService = gPoseService;

    public (int Breaking, int Feature) ApiVersion => (Brio.MajorAPIVersion, Brio.MinorAPIVersion);

    public bool IsAvailable => true;

    public bool IsValidGPoseSession => _gPoseService.IsGPosing;
}
