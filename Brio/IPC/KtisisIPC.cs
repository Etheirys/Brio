using Brio.Config;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System.Threading.Tasks;

namespace Brio.IPC;

public class KtisisIPC : BrioIPC
{
    public override string Name => "Ktisis";

    public override bool IsAvailable => GetAPIVersion() is not (0, 0);

    public override bool AllowIntegration => true;

    public override int APIMajor => 1;

    public override int APIMinor => 0;

    public override (int Major, int Minor) GetAPIVersion()
    => _ktisisApiVersion?.InvokeFunc() ?? (0, 0);

    public override IDalamudPluginInterface GetPluginInterface()
        => _pluginInterface;

    //

    private readonly ConfigurationService _configurationService;
    private readonly IDalamudPluginInterface _pluginInterface;

    private readonly ICallGateSubscriber<(int, int)>? _ktisisApiVersion;

    //private readonly ICallGateSubscriber<IGameObject, Task<string?>> _ktisisLoadPose;
    //private readonly ICallGateSubscriber<IGameObject, string, Task<bool>> _ktisisSavePose;

    private readonly ICallGateSubscriber<bool>? _ktisisRefreshActors;
    private readonly ICallGateSubscriber<bool>? _ktisisIsPosing;


    public KtisisIPC(IDalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _ktisisApiVersion = _pluginInterface.GetIpcSubscriber<(int, int)>("Ktisis.ApiVersion");
        _ktisisRefreshActors = _pluginInterface.GetIpcSubscriber<bool>("Ktisis.RefreshActors");
        _ktisisIsPosing = _pluginInterface.GetIpcSubscriber<bool>("Ktisis.IsPosing");
    }

    public bool IsPosing => _ktisisIsPosing?.InvokeFunc() ?? false;

    public void RefreshActors()
    {
        if (IsAvailable && !Disabled)
        {
            _ktisisRefreshActors?.InvokeFunc();
        }
    }

    public override void Dispose()
    {

    }
}
