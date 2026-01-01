
#nullable disable

using Brio.API;
using Brio.API.Helpers;
using BrioTester.Plugin.UI;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System.Numerics;

namespace Brio;

public static class BrioAPI
{
    public static readonly (int major, int minor) APIVersion = (3, 0);

    private static bool hasInit = false;

    //

    static ApiVersion apiVersion;
    static SpawnActor spawnActor;
    static DespawnActor despawnActor;
    static ActorExists actorExists;

    static BrioEventSubscriber BrioInitialized;
    static BrioEventSubscriber DeInitialized;

    //
    //

    /// <summary>
    /// Initializes the Brio API with the given plugin interface.
    /// </summary>
    /// <param name="pluginInterface">The Dalamud plugin interface.</param>
    public static void InitBrioAPI(IDalamudPluginInterface pi)
    {
        hasInit = true;

        apiVersion = new ApiVersion(pi);
        spawnActor = new SpawnActor(pi);
        despawnActor = new DespawnActor(pi);
        actorExists = new ActorExists(pi);

        BrioInitialized = Initialized.Subscriber(pi, OnBrioInitialized);
        DeInitialized = Deinitialized.Subscriber(pi, OnBrioDeinitialized);
    }

    public static void Dispose()
    {
        BrioInitialized.Dispose();
        DeInitialized.Dispose();
    }

    private static void OnBrioInitialized()
    {
        BrioTesterWindow.Message("OnBrioInitialized");
    }

    private static void OnBrioDeinitialized()
    {
        BrioTesterWindow.Message("OnBrioDeinitialized");
    }

    /// <summary>
    /// Checks if the API version is compatible with this implementation on Brio's IPC API.
    /// </summary>
    /// <returns>True if the API version is compatible, otherwise false.</returns>
    public static (int major, int minor) IsVersionCompatible()
    {
        //if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return apiVersion.Invoke();
    }

    //    /// <summary>
    //    /// Gets the Brio IPC API version .
    //    /// </summary>
    //    /// <returns>A tuple containing the major and minor version numbers.</returns>
    //    public static (int Major, int Minor) GetVersion()
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return API_Version_IPC.InvokeFunc();
    //    }

    /// <summary>
    /// Spawns an actor.
    /// </summary>
    /// <returns>The spawned actor, or null if the spawn failed.</returns>
    public static IGameObject? SpawnActor()
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return spawnActor.Invoke();
    }

    //    /// <summary>
    //    /// Spawns an actor asynchronously.
    //    /// </summary>
    //    /// <param name="spawnWithCompanionSlot">Whether to spawn with a companion slot.</param>
    //    /// <param name="selectInHierarchy">Whether to select the actor in the hierarchy.</param>
    //    /// <param name="spawnFrozen">Whether to spawn the actor with animation speed of 0f.</param>
    //    /// <returns>The spawned actor, or null if the spawn failed.</returns>
    //    public static async Task<IGameObject?> SpawnActorAsync(bool spawnWithCompanionSlot = false, bool selectInHierarchy = false, bool spawnFrozen = false)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return await Actor_SpawnExAsync_IPC.InvokeFunc(spawnWithCompanionSlot, selectInHierarchy, spawnFrozen);
    //    }

    /// <summary>
    /// Despawns an actor.
    /// </summary>
    /// <param name="actorToDespawn">The actor to despawn.</param>
    /// <returns>True if the actor was successfully despawned, otherwise false.</returns>
    public static bool DespawnActor(IGameObject actorToDespawn)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return despawnActor.Invoke(actorToDespawn);
    }

    //    /// <summary>
    //    /// Despawns an actor asynchronously.
    //    /// </summary>
    //    /// <param name="actorToDespawn">The actor to despawn.</param>
    //    /// <returns>True if the actor was successfully despawned, otherwise false.</returns>
    //    public static Task<bool> DespawnActorAsync(IGameObject actorToDespawn)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_DespawnActorAsync_Ipc.InvokeFunc(actorToDespawn);
    //    }

    //    /// <summary>
    //    /// Sets the model transform of an actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to transform.</param>
    //    /// <param name="position">The new position of the actor.</param>
    //    /// <param name="rotation">The new rotation of the actor.</param>
    //    /// <param name="scale">The new scale of the actor.</param>
    //    /// <param name="additive">Whether the transformation should be additive otherwise will override.</param>
    //    /// <returns>True if the transformation was successful, otherwise false.</returns>
    //    public static bool SetActorModelTransform(IGameObject actor, Vector3? position, Quaternion? rotation, Vector3? scale, bool additive = true)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_SetModelTransform_IPC.InvokeFunc(actor, position, rotation, scale, additive);
    //    }

    //    /// <summary>
    //    /// Gets the model transform of an actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to get the transform of.</param>
    //    /// <returns>A tuple containing the position, rotation, and scale of the actor.</returns>
    //    public static (Vector3? Position, Quaternion? Rotation, Vector3? Scale) GetActorModelTransform(IGameObject actor)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_GetModelTransform_IPC.InvokeFunc(actor);
    //    }

    //    /// <summary>
    //    /// Resets the model transform of an actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to reset the transform of.</param>
    //    /// <returns>True if the transformation was successfully reset, otherwise false.</returns>
    //    public static bool ResetActorModelTransform(IGameObject actor)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_ResetModelTransform_IPC.InvokeFunc(actor);
    //    }

    //    /// <summary>
    //    /// Sets the position of an actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to set the position of.</param>
    //    /// <param name="position">The new position of the actor.</param>
    //    /// <param name="additive">Whether the position change should be additive otherwise will override.</param>
    //    /// <returns>True if the position was successfully set, otherwise false.</returns>
    //    public static bool SetActorPosition(IGameObject actor, Vector3 position, bool additive = true)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_SetModelTransform_IPC.InvokeFunc(actor, position, null, null, additive);
    //    }

    //    /// <summary>
    //    /// Sets the rotation of an actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to set the rotation of.</param>
    //    /// <param name="rotation">The new rotation of the actor.</param>
    //    /// <param name="additive">Whether the rotation change should be additive otherwise will override.</param>
    //    /// <returns>True if the rotation was successfully set, otherwise false.</returns>
    //    public static bool SetActorRotation(IGameObject actor, Quaternion rotation, bool additive = true)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_SetModelTransform_IPC.InvokeFunc(actor, null, rotation, null, additive);
    //    }

    //    /// <summary>
    //    /// Sets the scale of an actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to set the scale of.</param>
    //    /// <param name="scale">The new scale of the actor.</param>
    //    /// <param name="additive">Whether the scale change should be additive otherwise will override.</param>
    //    /// <returns>True if the scale was successfully set, otherwise false.</returns>
    //    public static bool SetActorScale(IGameObject actor, Vector3 scale, bool additive = true)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_SetModelTransform_IPC.InvokeFunc(actor, null, null, scale, additive);
    //    }

    //    /// <summary>
    //    /// Sets the pose of an actor from a file path.
    //    /// </summary>
    //    /// <param name="actor">The actor to set the pose of.</param>
    //    /// <param name="fileName">The file path to load the pose from.</param>
    //    /// <returns>True if the pose was successfully set, otherwise false.</returns>
    //    public static bool SetActorPoseFromFilePath(IGameObject actor, string fileName)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_Pose_LoadFromFile_IPC.InvokeFunc(actor, fileName);
    //    }

    //    /// <summary>
    //    /// Sets the pose of an actor from a JSON string.
    //    /// </summary>
    //    /// <param name="actor">The actor to set the pose of.</param>
    //    /// <param name="json">The JSON string to load the pose from.</param>
    //    /// <param name="isCMPformat">Whether the JSON string is in the CMP format.</param>
    //    /// <returns>True if the pose was successfully set, otherwise false.</returns>
    //    public static bool SetActorPoseFromJson(IGameObject actor, string json, bool isCMPformat = false)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_Pose_LoadFromJson_IPC.InvokeFunc(actor, json, isCMPformat);
    //    }

    //    /// <summary>
    //    /// Gets the pose of an actor as a JSON string.
    //    /// </summary>
    //    /// <param name="actor">The actor to get the pose of.</param>
    //    /// <returns>The pose of the actor as a JSON string.</returns>
    //    public static string GetActorPoseAsJson(IGameObject actor)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_Pose_GetFromJson_IPC.InvokeFunc(actor);
    //    }

    //    /// <summary>
    //    /// Resets the pose of an actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to reset the pose of.</param>
    //    /// <param name="clearRedoHistory">Whether to clear the redo history.</param>
    //    /// <returns>True if the pose was successfully reset, otherwise false.</returns>
    //    public static bool ResetActorPose(IGameObject actor, bool clearRedoHistory = false)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_Pose_Reset_IPC.InvokeFunc(actor, clearRedoHistory);
    //    }

    /// <summary>
    /// Checks if an actor exists.
    /// </summary>
    /// <param name="actor">The actor to check.</param>
    /// <returns>True if the actor exists, otherwise false.</returns>
    public static bool ActorExists(IGameObject actor)
    {
        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

        return actorExists.Invoke(actor);
    }

    //    /// <summary>
    //    /// Gets all active actors.
    //    /// </summary>
    //    /// <returns>A tuple containing a boolean indicating if there is at least one actor, and an array of all active actors.</returns>
    //    public static (bool HasAtLeastOne, IGameObject[] Actors) GetAllActiveActors()
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        var actors = Actor_GetAll_IPC.InvokeFunc();

    //        return (actors?.Length > 1, actors!);
    //    }

    //    /// <summary>
    //    /// Sets the speed of the specified actor.
    //    /// </summary>
    //    /// <param name="actor">The actor whose speed is to be set.</param>
    //    /// <param name="speed">The speed to set for the actor.</param>
    //    /// <returns>True if the speed was successfully set, otherwise false.</returns>
    //    public static bool SetActorSpeed(IGameObject actor, float speed)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_SetSpeed_IPC.InvokeFunc(actor, speed);
    //    }

    //    /// <summary>
    //    /// Gets the speed of the specified actor.
    //    /// </summary>
    //    /// <param name="actor">The actor whose speed is to be retrieved.</param>
    //    /// <returns>The speed of the specified actor.</returns>
    //    public static float GetActorSpeed(IGameObject actor)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_GetSpeed_IPC.InvokeFunc(actor);
    //    }

    //    /// <summary>
    //    /// Freezes the specified actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to be frozen.</param>
    //    /// <returns>True if the actor was successfully frozen, otherwise false.</returns>
    //    public static bool FreezeActor(IGameObject actor)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_Freeze_IPC.InvokeFunc(actor);
    //    }

    //    /// <summary>
    //    /// Unfreezes the specified actor.
    //    /// </summary>
    //    /// <param name="actor">The actor to be unfrozen.</param>
    //    /// <returns>True if the actor was successfully unfrozen, otherwise false.</returns>
    //    public static bool UnFreezeActor(IGameObject actor)
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return Actor_UnFreeze_IPC.InvokeFunc(actor);
    //    }

    //    /// <summary>
    //    /// Freezes FFXIV's physics simulation. 
    //    /// </summary>
    //    public static bool FreezePhysics()
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return FreezePhysics_IPC.InvokeFunc();
    //    }

    //    /// <summary>
    //    /// Unfreezes FFXIV's physics simulation. 
    //    /// </summary>
    //    public static bool UnFreezePhysics()
    //    {
    //        if (hasInit is false) throw new Exception("Call BrioAPI.InitBrioAPI first!");

    //        return UnFreezePhysics_IPC.InvokeFunc();
    //    }
}
