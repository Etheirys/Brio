using Brio.Config;
using Brio.Core;
using Brio.Game.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Ipc;
using System.Threading.Tasks;

namespace Brio.IPC;

public class BrioIPCService : ServiceBase<BrioIPCService>
{
    public static readonly (int, int) CurrentApiVersion = (1, 0);

    public const string ApiVersionIpcName = "Brio.ApiVersion";
    private ICallGateProvider<(int, int)>? ApiVersionIpc;

    public const string SpawnActorIpcName = "Brio.SpawnActor";
    private ICallGateProvider<GameObject?>? SpawnActorIpc;

    public const string DespawnActorIpcName = "Brio.DespawnActor";
    private ICallGateProvider<GameObject, bool>? DespawnActorIpc;

    public const string SpawnActorAsyncIpcName = "Brio.SpawnActorAsync";
    private ICallGateProvider<Task<GameObject?>>? SpawnActorAsyncIpc;

    public const string DespawnActorAsyncIpcName = "Brio.DespawnActorAsync";
    private ICallGateProvider<GameObject, Task<bool>>? DespawnActorAsyncIpc;

    public bool IsIPCEnabled { get; private set; } = false;

    public BrioIPCService()
    {
        if(!ConfigService.Configuration.AllowBrioIPC)
            return;

        CreateIPC();
    }

    private (int, int) ApiVersionImpl() => CurrentApiVersion;

    private Task<GameObject?> SpawnActorAsyncImpl() => Dalamud.Framework.RunOnTick(SpawnActorImpl);

    private GameObject? SpawnActorImpl()
    {
        ushort? id = ActorSpawnService.Instance.Spawn(SpawnOptions.ApplyModelPosition);
        if(id != null)
            return Dalamud.ObjectTable[(int)id];

        return null;
    }

    private Task<bool> DespawnActorAsyncImpl(GameObject gameObject) => Dalamud.Framework.RunOnTick(() => DespawnActorImpl(gameObject));
    private bool DespawnActorImpl(GameObject gameObject) => ActorSpawnService.Instance.DestroyObject(gameObject);

    private void CreateIPC()
    {
        ApiVersionIpc = Dalamud.PluginInterface.GetIpcProvider<(int, int)>(ApiVersionIpcName);
        ApiVersionIpc.RegisterFunc(ApiVersionImpl);

        SpawnActorAsyncIpc = Dalamud.PluginInterface.GetIpcProvider<Task<GameObject?>>(SpawnActorAsyncIpcName);
        SpawnActorAsyncIpc.RegisterFunc(SpawnActorAsyncImpl);

        SpawnActorIpc = Dalamud.PluginInterface.GetIpcProvider<GameObject?>(SpawnActorIpcName);
        SpawnActorIpc.RegisterFunc(SpawnActorImpl);

        DespawnActorIpc = Dalamud.PluginInterface.GetIpcProvider<GameObject, bool>(DespawnActorIpcName);
        DespawnActorIpc.RegisterFunc(DespawnActorImpl);

        DespawnActorAsyncIpc = Dalamud.PluginInterface.GetIpcProvider<GameObject, Task<bool>>(DespawnActorAsyncIpcName);
        DespawnActorAsyncIpc.RegisterFunc(DespawnActorAsyncImpl);

        IsIPCEnabled = true;
    }

    private void DisposeIPC()
    {
        ApiVersionIpc?.UnregisterFunc();
        SpawnActorAsyncIpc?.UnregisterFunc();
        SpawnActorIpc?.UnregisterFunc();
        DespawnActorIpc?.UnregisterFunc();
        DespawnActorAsyncIpc?.UnregisterFunc();

        ApiVersionIpc = null;
        SpawnActorAsyncIpc = null;
        SpawnActorIpc = null;
        DespawnActorIpc = null;
        DespawnActorAsyncIpc = null;

        IsIPCEnabled = false;
    }

    public override void Tick()
    {
        if(IsIPCEnabled != ConfigService.Configuration.AllowBrioIPC)
        {
            if(ConfigService.Configuration.AllowBrioIPC)
            {
                CreateIPC();
            }
            else
            {
                DisposeIPC();
            }
        }
    }

    public override void Dispose()
    {
        DisposeIPC();
    }
}
