using Brio.Config;
using Brio.Game.Posing;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Brio.IPC;

public class KtisisService : BrioIPC
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
	private readonly ICallGateSubscriber<bool, bool>? _ktisisPosingChanged;


    public KtisisService(IDalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _ktisisApiVersion = _pluginInterface.GetIpcSubscriber<(int, int)>("Ktisis.ApiVersion");
        _ktisisRefreshActors = _pluginInterface.GetIpcSubscriber<bool>("Ktisis.RefreshActors");
        _ktisisIsPosing = _pluginInterface.GetIpcSubscriber<bool>("Ktisis.IsPosing");

		_ktisisPosingChanged = _pluginInterface.GetIpcSubscriber<bool, bool>("Ktisis.PosingChanged");
		_ktisisPosingChanged.Subscribe(this.PosingChanged);
	}

    public bool IsPosing => ((_ktisisIsPosing?.HasFunction ?? false) && (_ktisisIsPosing?.InvokeFunc() ?? false));

    public void RefreshActors()
    {
        if(IsAvailable && !Disabled)
        {
            _ktisisRefreshActors?.InvokeFunc();
        }
    }

	private void PosingChanged(bool isPosing) {
		Brio.TryGetService<SkeletonService>(out var skeletonService);
		Brio.TryGetService<ModelTransformService>(out var modelTransformService);
		if (isPosing) {
			skeletonService._updateBonePhysicsHook.Disable();
			skeletonService._finalizeSkeletonsHook.Disable();
			modelTransformService._setPositionHook.Disable();
		} else {
			skeletonService._updateBonePhysicsHook.Enable();
			skeletonService._finalizeSkeletonsHook.Enable();
			modelTransformService._setPositionHook.Enable();
		}
	}

    public override void Dispose()
    {

    }
}
