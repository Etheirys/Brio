using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System.Numerics;

namespace Brio;

public static class BrioAPI
{
    public static readonly (int major, int minor) APIVersion = (2, 0);

    private static bool hasInit = false;

    //

    private static ICallGateSubscriber<(int, int)> API_Version_IPC;

    private static ICallGateSubscriber<IGameObject?> Actor_Spawn_IPC;
    private static ICallGateSubscriber<Task<IGameObject?>> Actor_SpawnAsync_IPC;
    private static ICallGateSubscriber<bool, bool, Task<IGameObject?>> Actor_SpawnExAsync_IPC;

    private static ICallGateSubscriber<IGameObject, bool> Actor_DespawnActor_Ipc;
    private static ICallGateSubscriber<IGameObject, Task<bool>> Actor_DespawnActorAsync_Ipc;

    private static ICallGateSubscriber<IGameObject, Vector3?, Quaternion?, Vector3?, bool, bool> Actor_SetModelTransform_IPC;
    private static ICallGateSubscriber<IGameObject, (Vector3?, Quaternion?, Vector3?)> Actor_GetModelTransform_IPC;
    private static ICallGateSubscriber<IGameObject, bool> Actor_ResetModelTransform_IPC;

    private static ICallGateSubscriber<IGameObject, string, bool> Actor_Pose_LoadFromFile_IPC;
    private static ICallGateSubscriber<IGameObject, string, bool, bool> Actor_Pose_LoadFromJson_IPC;
    private static ICallGateSubscriber<IGameObject, string> Actor_Pose_GetFromJson_IPC;

    private static ICallGateSubscriber<IGameObject, bool, bool> Actor_Pose_Reset_IPC;

    private static ICallGateSubscriber<IGameObject, bool> Actor_Exists_IPC;
    private static ICallGateSubscriber<IGameObject[]?> Actor_GetAll_IPC;

    //
    //

    /// <summary>
    /// Initializes the Brio API with the given plugin interface.
    /// </summary>
    /// <param name="pluginInterface">The Dalamud plugin interface.</param>
    public static void InitBrioAPI(IDalamudPluginInterface pluginInterface)
    {
        hasInit = true;

        API_Version_IPC = pluginInterface.GetIpcSubscriber<(int, int)>("Brio.ApiVersion");

        Actor_Spawn_IPC = pluginInterface.GetIpcSubscriber<IGameObject?>("Brio.Actor.Spawn");
        Actor_SpawnAsync_IPC = pluginInterface.GetIpcSubscriber<Task<IGameObject?>>("Brio.Actor.SpawnAsync");
        Actor_SpawnExAsync_IPC = pluginInterface.GetIpcSubscriber<bool, bool, Task<IGameObject?>>("Brio.Actor.SpawnExAsync");

        Actor_DespawnActor_Ipc = pluginInterface.GetIpcSubscriber<IGameObject, bool>("Brio.Actor.Despawn");
        Actor_DespawnActorAsync_Ipc = pluginInterface.GetIpcSubscriber<IGameObject, Task<bool>>("Brio.Actor.DespawnAsync");

        Actor_SetModelTransform_IPC = pluginInterface.GetIpcSubscriber<IGameObject, Vector3?, Quaternion?, Vector3?, bool, bool>("Brio.Actor.SetModelTransform");
        Actor_GetModelTransform_IPC = pluginInterface.GetIpcSubscriber<IGameObject, (Vector3?, Quaternion?, Vector3?)>("Brio.Actor.GetModelTransform");
        Actor_ResetModelTransform_IPC = pluginInterface.GetIpcSubscriber<IGameObject, bool>("Brio.Actor.ResetModelTransform");

        Actor_Pose_LoadFromFile_IPC = pluginInterface.GetIpcSubscriber<IGameObject, string, bool>("Brio.Actor.Pose.LoadFromFile");
        Actor_Pose_LoadFromJson_IPC = pluginInterface.GetIpcSubscriber<IGameObject, string, bool, bool>("Brio.Actor.Pose.LoadFromJson");
        Actor_Pose_GetFromJson_IPC = pluginInterface.GetIpcSubscriber<IGameObject, string>("Brio.Actor.Pose.GetPoseAsJson");

        Actor_Pose_Reset_IPC = pluginInterface.GetIpcSubscriber<IGameObject, bool, bool>("Brio.Actor.Pose.Reset");

        Actor_Exists_IPC = pluginInterface.GetIpcSubscriber<IGameObject, bool>("Brio.Actor.Exists");
        Actor_GetAll_IPC = pluginInterface.GetIpcSubscriber<IGameObject[]?>("Brio.Actor.GetAll");
    }

    /// <summary>
    /// Checks if the API version is compatible with this implementation on Brio's IPC API.
    /// </summary>
    /// <returns>True if the API version is compatible, otherwise false.</returns>
    public static bool IsVersionCompatible()
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return API_Version_IPC.InvokeFunc() == APIVersion;
    }

    /// <summary>
    /// Gets the Brio IPC API version .
    /// </summary>
    /// <returns>A tuple containing the major and minor version numbers.</returns>
    public static (int Major, int Minor) GetVersion()
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return API_Version_IPC.InvokeFunc();
    }

    /// <summary>
    /// Spawns an actor.
    /// </summary>
    /// <returns>The spawned actor, or null if the spawn failed.</returns>
    public static IGameObject? SpawnActor()
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_Spawn_IPC.InvokeFunc();
    }

    /// <summary>
    /// Spawns an actor asynchronously.
    /// </summary>
    /// <param name="spawnWithCompanionSlot">Whether to spawn with a companion slot.</param>
    /// <param name="selectInHierarchy">Whether to select the actor in the hierarchy.</param>
    /// <returns>The spawned actor, or null if the spawn failed.</returns>
    public static Task<IGameObject?> SpawnActorAsync(bool spawnWithCompanionSlot = false, bool selectInHierarchy = false)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_SpawnExAsync_IPC.InvokeFunc(spawnWithCompanionSlot, selectInHierarchy);
    }

    /// <summary>
    /// Despawns an actor.
    /// </summary>
    /// <param name="actorToDespawn">The actor to despawn.</param>
    /// <returns>True if the actor was successfully despawned, otherwise false.</returns>
    public static bool DespawnActor(IGameObject actorToDespawn)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_DespawnActor_Ipc.InvokeFunc(actorToDespawn);
    }

    /// <summary>
    /// Despawns an actor asynchronously.
    /// </summary>
    /// <param name="actorToDespawn">The actor to despawn.</param>
    /// <returns>True if the actor was successfully despawned, otherwise false.</returns>
    public static Task<bool> DespawnActorAsync(IGameObject actorToDespawn)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_DespawnActorAsync_Ipc.InvokeFunc(actorToDespawn);
    }

    /// <summary>
    /// Sets the model transform of an actor.
    /// </summary>
    /// <param name="actor">The actor to transform.</param>
    /// <param name="position">The new position of the actor.</param>
    /// <param name="rotation">The new rotation of the actor.</param>
    /// <param name="scale">The new scale of the actor.</param>
    /// <param name="additive">Whether the transformation should be additive otherwise will override.</param>
    /// <returns>True if the transformation was successful, otherwise false.</returns>
    public static bool SetActorModelTransform(IGameObject actor, Vector3? position, Quaternion? rotation, Vector3? scale, bool additive = true)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_SetModelTransform_IPC.InvokeFunc(actor, position, rotation, scale, additive);
    }

    /// <summary>
    /// Gets the model transform of an actor.
    /// </summary>
    /// <param name="actor">The actor to get the transform of.</param>
    /// <returns>A tuple containing the position, rotation, and scale of the actor.</returns>
    public static (Vector3? Position, Quaternion? Rotation, Vector3? Scale) GetActorModelTransform(IGameObject actor)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_GetModelTransform_IPC.InvokeFunc(actor);
    }

    /// <summary>
    /// Resets the model transform of an actor.
    /// </summary>
    /// <param name="actor">The actor to reset the transform of.</param>
    /// <returns>True if the transformation was successfully reset, otherwise false.</returns>
    public static bool ResetActorModelTransform(IGameObject actor)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_ResetModelTransform_IPC.InvokeFunc(actor);
    }

    /// <summary>
    /// Sets the position of an actor.
    /// </summary>
    /// <param name="actor">The actor to set the position of.</param>
    /// <param name="position">The new position of the actor.</param>
    /// <param name="additive">Whether the position change should be additive otherwise will override.</param>
    /// <returns>True if the position was successfully set, otherwise false.</returns>
    public static bool SetActorPosition(IGameObject actor, Vector3 position, bool additive = true)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_SetModelTransform_IPC.InvokeFunc(actor, position, null, null, additive);
    }

    /// <summary>
    /// Sets the rotation of an actor.
    /// </summary>
    /// <param name="actor">The actor to set the rotation of.</param>
    /// <param name="rotation">The new rotation of the actor.</param>
    /// <param name="additive">Whether the rotation change should be additive otherwise will override.</param>
    /// <returns>True if the rotation was successfully set, otherwise false.</returns>
    public static bool SetActorRotation(IGameObject actor, Quaternion rotation, bool additive = true)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_SetModelTransform_IPC.InvokeFunc(actor, null, rotation, null, additive);
    }

    /// <summary>
    /// Sets the scale of an actor.
    /// </summary>
    /// <param name="actor">The actor to set the scale of.</param>
    /// <param name="scale">The new scale of the actor.</param>
    /// <param name="additive">Whether the scale change should be additive otherwise will override.</param>
    /// <returns>True if the scale was successfully set, otherwise false.</returns>
    public static bool SetActorScale(IGameObject actor, Vector3 scale, bool additive = true)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_SetModelTransform_IPC.InvokeFunc(actor, null, null, scale, additive);
    }

    /// <summary>
    /// Sets the pose of an actor from a file path.
    /// </summary>
    /// <param name="actor">The actor to set the pose of.</param>
    /// <param name="fileName">The file path to load the pose from.</param>
    /// <returns>True if the pose was successfully set, otherwise false.</returns>
    public static bool SetActorPoseFromFilePath(IGameObject actor, string fileName)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_Pose_LoadFromFile_IPC.InvokeFunc(actor, fileName);
    }

    /// <summary>
    /// Sets the pose of an actor from a JSON string.
    /// </summary>
    /// <param name="actor">The actor to set the pose of.</param>
    /// <param name="json">The JSON string to load the pose from.</param>
    /// <param name="isCMPformat">Whether the JSON string is in the CMP format.</param>
    /// <returns>True if the pose was successfully set, otherwise false.</returns>
    public static bool SetActorPoseFromJson(IGameObject actor, string json, bool isCMPformat = false)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_Pose_LoadFromJson_IPC.InvokeFunc(actor, json, isCMPformat);
    }

    /// <summary>
    /// Gets the pose of an actor as a JSON string.
    /// </summary>
    /// <param name="actor">The actor to get the pose of.</param>
    /// <returns>The pose of the actor as a JSON string.</returns>
    public static string GetActorPoseAsJson(IGameObject actor)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_Pose_GetFromJson_IPC.InvokeFunc(actor);
    }

    /// <summary>
    /// Resets the pose of an actor.
    /// </summary>
    /// <param name="actor">The actor to reset the pose of.</param>
    /// <param name="clearRedoHistory">Whether to clear the redo history.</param>
    /// <returns>True if the pose was successfully reset, otherwise false.</returns>
    public static bool ResetActorPose(IGameObject actor, bool clearRedoHistory = false)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_Pose_Reset_IPC.InvokeFunc(actor, clearRedoHistory);
    }

    /// <summary>
    /// Checks if an actor exists.
    /// </summary>
    /// <param name="actor">The actor to check.</param>
    /// <returns>True if the actor exists, otherwise false.</returns>
    public static bool ActorExists(IGameObject actor)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return Actor_Exists_IPC.InvokeFunc(actor);
    }

    /// <summary>
    /// Gets all active actors.
    /// </summary>
    /// <returns>A tuple containing a boolean indicating if there is at least one actor, and an array of all active actors.</returns>
    public static (bool HasAtLeastOne, IGameObject[] Actors) GetAllActiveActors()
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        var actors = Actor_GetAll_IPC.InvokeFunc();

        return (actors?.Length > 1, actors!);
    }
}
