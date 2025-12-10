using Brio.API.Interface;

namespace Brio.IPC.API;

public class StateAPI : IState
{
    public (int Breaking, int Feature) ApiVersion => (BrioAPIService.MajorVersion, BrioAPIService.MinorVersion);

    public bool IsAvailable => true;
}
