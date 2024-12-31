using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Files;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Resources;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace Brio.IPC;
internal class BrioIPCService : IDisposable
{
    public static readonly (int, int) CurrentApiVersion = (2, 0);

    public bool IsIPCEnabled { get; private set; } = false;

    //

    public const string ApiVersion_IPCName = "Brio.ApiVersion";
    private ICallGateProvider<(int, int)>? API_Version_IPC;


    public const string Actor_Spawn_IPCName = "Brio.Actor.Spawn";
    private ICallGateProvider<IGameObject?>? Actor_Spawn_IPC;

    public const string Actor_SpawnAsync_IPCName = "Brio.Actor.SpawnAsync";
    private ICallGateProvider<Task<IGameObject?>>? Actor_SpawnAsync_IPC;

    public const string Actor_SpawnExAsync_IPCName = "Brio.Actor.SpawnExAsync";
    private ICallGateProvider<bool, bool, Task<IGameObject?>>? Actor_SpawnExAsync_IPC;


    public const string Actor_Despawn_IPCName = "Brio.Actor.Despawn";
    private ICallGateProvider<IGameObject, bool>? Actor_DespawnActor_Ipc;

    public const string Actor_DespawnAsync_IPCName = "Brio.Actor.DespawnAsync";
    private ICallGateProvider<IGameObject, Task<bool>>? Actor_DespawnActorAsync_Ipc;


    public const string Actor_SetModelTransform_IPCName = "Brio.Actor.SetModelTransform";
    private ICallGateProvider<IGameObject, Vector3, Quaternion, Vector3, bool>? Actor_SetModelTransform_IPC;

    public const string Actor_GetModelTransform_IPCName = "Brio.Actor.GetModelTransform";
    private ICallGateProvider<IGameObject, (Vector3?, Quaternion?, Vector3?)>? Actor_GetModelTransform_IPC;
   
    public const string Actor_ResetModelTransform_IPCName = "Brio.Actor.ResetModelTransform";
    private ICallGateProvider<IGameObject, bool>? Actor_ResetModelTransform_IPC;


    public const string Actor_PoseLoadFromFile_IPCName = "Brio.Actor.Pose.LoadFromFile";
    private ICallGateProvider<IGameObject, string, bool>? Actor_Pose_LoadFromFile_IPC;

    public const string Actor_PoseLoadFromJson_IPCName = "Brio.Actor.Pose.LoadFromJson";
    private ICallGateProvider<IGameObject, string, bool, bool>? Actor_Pose_LoadFromJson_IPC;

    public const string Actor_PoseGetAsJson_IPCName = "Brio.Actor.Pose.GetPoseAsJson";
    private ICallGateProvider<IGameObject, string?>? Actor_Pose_GetFromJson_IPC;

    //

    private readonly ActorSpawnService _actorSpawnService;
    private readonly ConfigurationService _configurationService;
    private readonly EntityManager _entityManager;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;

    public BrioIPCService(ActorSpawnService actorSpawnService, ConfigurationService configurationService, EntityManager entityManager, IDalamudPluginInterface pluginInterface, IFramework framework)
    {
        _actorSpawnService = actorSpawnService;
        _configurationService = configurationService;
        _entityManager = entityManager;
        _pluginInterface = pluginInterface;
        _framework = framework;

        if(_configurationService.Configuration.IPC.EnableBrioIPC)
            CreateIPC();

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;
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

    private void CreateIPC()
    {
        API_Version_IPC = _pluginInterface.GetIpcProvider<(int, int)>(ApiVersion_IPCName);
        API_Version_IPC.RegisterFunc(ApiVersion_Impl);


        Actor_Spawn_IPC = _pluginInterface.GetIpcProvider<IGameObject?>(Actor_Spawn_IPCName);
        Actor_Spawn_IPC.RegisterFunc(SpawnActor);

        Actor_SpawnAsync_IPC = _pluginInterface.GetIpcProvider<Task<IGameObject?>> (Actor_SpawnAsync_IPCName);
        Actor_SpawnAsync_IPC.RegisterFunc(SpawnActorAsync_Impl);

        Actor_SpawnExAsync_IPC = _pluginInterface.GetIpcProvider<bool, bool, Task<IGameObject?>>(Actor_SpawnExAsync_IPCName);
        Actor_SpawnExAsync_IPC.RegisterFunc(SpawnExAsync_Impl);

        Actor_DespawnActor_Ipc = _pluginInterface.GetIpcProvider<IGameObject, bool>(Actor_Despawn_IPCName);
        Actor_DespawnActor_Ipc.RegisterFunc(DespawnActor);

        Actor_DespawnActorAsync_Ipc = _pluginInterface.GetIpcProvider<IGameObject, Task<bool>>(Actor_DespawnAsync_IPCName);
        Actor_DespawnActorAsync_Ipc.RegisterFunc(DespawnActorAsync_Impl);

        Actor_SetModelTransform_IPC = _pluginInterface.GetIpcProvider<IGameObject, Vector3, Quaternion, Vector3, bool>(Actor_SetModelTransform_IPCName);
        Actor_SetModelTransform_IPC.RegisterFunc(ActorSetModelTransform_Impl);

        Actor_GetModelTransform_IPC = _pluginInterface.GetIpcProvider<IGameObject, (Vector3?, Quaternion?, Vector3?)>(Actor_GetModelTransform_IPCName);
        Actor_GetModelTransform_IPC.RegisterFunc(ActorGetModelTransform_Impl);

        Actor_ResetModelTransform_IPC = _pluginInterface.GetIpcProvider<IGameObject, bool>(Actor_ResetModelTransform_IPCName);
        Actor_ResetModelTransform_IPC.RegisterFunc(ActorResetModelTransform_Impl);

        Actor_Pose_LoadFromFile_IPC = _pluginInterface.GetIpcProvider<IGameObject, string, bool>(Actor_PoseLoadFromFile_IPCName);
        Actor_Pose_LoadFromFile_IPC.RegisterFunc(LoadFromFile_Impl);

        Actor_Pose_LoadFromJson_IPC = _pluginInterface.GetIpcProvider<IGameObject, string, bool, bool>(Actor_PoseLoadFromJson_IPCName);
        Actor_Pose_LoadFromJson_IPC.RegisterFunc(LoadFromJson_Impl);

        Actor_Pose_GetFromJson_IPC = _pluginInterface.GetIpcProvider<IGameObject, string?>(Actor_PoseGetAsJson_IPCName);
        Actor_Pose_GetFromJson_IPC.RegisterFunc(GetPoseAsJson_Impl);

        IsIPCEnabled = true;
    }
    private void DisposeIPC()
    {
        API_Version_IPC?.UnregisterFunc();

        Actor_Spawn_IPC?.UnregisterFunc();
        Actor_SpawnAsync_IPC?.UnregisterFunc();
        Actor_SpawnExAsync_IPC?.UnregisterFunc();

        Actor_DespawnActor_Ipc?.UnregisterFunc();
        Actor_DespawnActorAsync_Ipc?.UnregisterFunc();

        Actor_SetModelTransform_IPC?.UnregisterFunc();
        Actor_GetModelTransform_IPC?.UnregisterFunc();

        Actor_Pose_LoadFromFile_IPC?.UnregisterFunc();
        Actor_Pose_LoadFromJson_IPC?.UnregisterFunc();
        Actor_Pose_GetFromJson_IPC?.UnregisterFunc();

        API_Version_IPC = null;

        Actor_Spawn_IPC = null;
        Actor_SpawnAsync_IPC = null;
        Actor_SpawnExAsync_IPC = null;

        Actor_DespawnActor_Ipc = null;
        Actor_DespawnActorAsync_Ipc = null;

        Actor_SetModelTransform_IPC = null;
        Actor_GetModelTransform_IPC = null;

        Actor_Pose_LoadFromFile_IPC = null;
        Actor_Pose_LoadFromJson_IPC = null;
        Actor_Pose_GetFromJson_IPC = null;

        IsIPCEnabled = false;
    }

    //

    private (int, int) ApiVersion_Impl() => CurrentApiVersion;

    private Task<IGameObject?> SpawnActorAsync_Impl() => _framework.RunOnTick(SpawnActor);
    private IGameObject? SpawnActor()
    {
        if(_actorSpawnService.CreateCharacter(out var character))
            return character;

        return null;
    }

    private async Task<IGameObject?> SpawnExAsync_Impl(bool spawnCompanion, bool selectInHierarchy) => await _framework.RunOnTick(() => SpawnEx(spawnCompanion, selectInHierarchy));
    private IGameObject? SpawnEx(bool spawnCompanion, bool selectInHierarchy)
    {
        SpawnFlags flags = SpawnFlags.Default;
        if(spawnCompanion)
        {
            flags |= SpawnFlags.ReserveCompanionSlot;
        }

        if(_actorSpawnService.CreateCharacter(out var character, flags, disableSpawnCompanion: false))
        {
            if(selectInHierarchy)
            {
                _entityManager.SetSelectedEntity(character);
            }

            return character;

        }

        return null;
    }

    private Task<bool> DespawnActorAsync_Impl(IGameObject gameObject) => _framework.RunOnTick(() => DespawnActor(gameObject));
    private bool DespawnActor(IGameObject gameObject) => _actorSpawnService.DestroyObject(gameObject);

    private unsafe bool ActorSetModelTransform_Impl(IGameObject gameObject, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ModelPosingCapability>(out var transformCapability))
            {
                transformCapability.Transform = new Core.Transform { Position = position, Rotation = rotation, Scale = scale };
                return true;
            }
        }
        return false;
    }
    private unsafe (Vector3?, Quaternion?, Vector3?) ActorGetModelTransform_Impl(IGameObject gameObject)
    {
        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ModelPosingCapability>(out var transformCapability))
            {
                var transform = transformCapability.Transform;

                return (transform.Position, transform.Rotation, transform.Scale);
            }
        }
        return (null, null, null);
    }
    private unsafe bool ActorResetModelTransform_Impl(IGameObject gameObject)
    {
        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ModelPosingCapability>(out var transformCapability))
            {
                transformCapability.ResetTransform();
                return true;
            }
        }
        return false;
    }

    private unsafe bool LoadFromFile_Impl(IGameObject gameObject, string fileURI)
    {
        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<PosingCapability>(out var posingCapability))
            {
                posingCapability.ImportPose(fileURI);
                return true;
            }
        }
        return false;
    }
    private unsafe bool LoadFromJson_Impl(IGameObject gameObject, string json, bool isLegacyCMToolPose)
    {
        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<PosingCapability>(out var posingCapability))
            {
                try
                {
                    if(isLegacyCMToolPose)
                    {
                        posingCapability.ImportPose(JsonSerializer.Deserialize<CMToolPoseFile>(json), null);
                    }
                    else
                    {
                        posingCapability.ImportPose(JsonSerializer.Deserialize<PoseFile>(json), null);
                    }

                    return true;
                }
                catch
                {
                    Brio.NotifyError("Invalid pose file loaded from IPC.");

                    return false;
                }
            }
        }

        return false;
    }
    private unsafe string? GetPoseAsJson_Impl(IGameObject gameObject)
    {
        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<PosingCapability>(out var posingCapability))
            {
                var pose = posingCapability.ExportPose();

                return JsonSerializer.Serialize(pose);
            }
        }
        return null;
    }

    public void Dispose()
    {
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;
        DisposeIPC();
    }
}
