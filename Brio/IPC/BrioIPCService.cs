using Brio.Config;
using Brio.Game.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;
using System.Threading.Tasks;

namespace Brio.IPC;
internal class BrioIPCService : IDisposable
{
    public bool IsIPCEnabled { get; private set; } = false;

    public static readonly (int, int) CurrentApiVersion = (1, 1);

    public const string ApiVersionIpcName = "Brio.ApiVersion";
    private ICallGateProvider<(int, int)>? ApiVersionIpc;

    public const string SpawnActorIpcName = "Brio.SpawnActor";
    private ICallGateProvider<GameObject?>? SpawnActorIpc;
 
    public const string SpawnActorWithoutCompanionIpcName = "Brio.SpawnActorWithoutCompanion";
    private ICallGateProvider<GameObject?>? SpawnActorWithoutCompanionIpc;

    public const string DespawnActorIpcName = "Brio.DespawnActor";
    private ICallGateProvider<GameObject, bool>? DespawnActorIpc;

    public const string SpawnActorAsyncIpcName = "Brio.SpawnActorAsync";
    private ICallGateProvider<Task<GameObject?>>? SpawnActorAsyncIpc;

    public const string DespawnActorAsyncIpcName = "Brio.DespawnActorAsync";
    private ICallGateProvider<GameObject, Task<bool>>? DespawnActorAsyncIpc;

    private readonly ActorSpawnService _actorSpawnService;
    private readonly ConfigurationService _configurationService;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;

    public BrioIPCService(ActorSpawnService actorSpawnService, ConfigurationService configurationService, DalamudPluginInterface pluginInterface, IFramework framework)
    {
        _actorSpawnService = actorSpawnService;
        _configurationService = configurationService;
        _pluginInterface = pluginInterface;
        _framework = framework;

        if(_configurationService.Configuration.IPC.EnableBrioIPC)
            CreateIPC();

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;
    }

    private (int, int) ApiVersionImpl() => CurrentApiVersion;

    private Task<GameObject?> SpawnActorAsyncImpl() => _framework.RunOnTick(SpawnActorImpl);

    private GameObject? SpawnActorImpl()
    {
        if(_actorSpawnService.CreateCharacter(out var character))
            return character;

        return null;
    }

    private GameObject? SpawnActorWithoutCompanionImpl()
    {
        if(_actorSpawnService.CreateCharacter(out var character, disableSpawnCompanion: true))
            return character;

        return null;
    }

    private Task<bool> DespawnActorAsyncImpl(GameObject gameObject) => _framework.RunOnTick(() => DespawnActorImpl(gameObject));
    private bool DespawnActorImpl(GameObject gameObject) => _actorSpawnService.DestroyObject(gameObject);

    private void CreateIPC()
    {
        ApiVersionIpc = _pluginInterface.GetIpcProvider<(int, int)>(ApiVersionIpcName);
        ApiVersionIpc.RegisterFunc(ApiVersionImpl);

        SpawnActorAsyncIpc = _pluginInterface.GetIpcProvider<Task<GameObject?>>(SpawnActorAsyncIpcName);
        SpawnActorAsyncIpc.RegisterFunc(SpawnActorAsyncImpl);

        SpawnActorIpc = _pluginInterface.GetIpcProvider<GameObject?>(SpawnActorIpcName);
        SpawnActorIpc.RegisterFunc(SpawnActorImpl);

        SpawnActorWithoutCompanionIpc = _pluginInterface.GetIpcProvider<GameObject?>(SpawnActorWithoutCompanionIpcName);
        SpawnActorWithoutCompanionIpc.RegisterFunc(SpawnActorWithoutCompanionImpl);

        DespawnActorIpc = _pluginInterface.GetIpcProvider<GameObject, bool>(DespawnActorIpcName);
        DespawnActorIpc.RegisterFunc(DespawnActorImpl);

        DespawnActorAsyncIpc = _pluginInterface.GetIpcProvider<GameObject, Task<bool>>(DespawnActorAsyncIpcName);
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

    private void OnConfigurationChanged()
    {
        if(IsIPCEnabled != _configurationService.Configuration.IPC.EnableBrioIPC)
        {
            if(_configurationService.Configuration.IPC.EnableBrioIPC)
                CreateIPC();
            else
                DisposeIPC();
        }
    }

    public void Dispose()
    {
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;
        DisposeIPC();
    }
}
