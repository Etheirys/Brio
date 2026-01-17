using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Brio.Game.Posing;
using Brio.IPC.API;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using BrioTransform = Brio.Core.Transform;

namespace Brio.Web;

public class ActorWebController(IFramework framework, PosingAPI posingAPI, SkeletonService skeletonService, ActorSpawnService actorSpawnService, ActorRedrawService redrawService) : WebApiController
{
    private readonly IFramework _framework = framework;
    private readonly ActorSpawnService _actorSpawnService = actorSpawnService;
    private readonly ActorRedrawService _redrawService = redrawService;

    private readonly PosingAPI _posingAPI = posingAPI;

    [Route(HttpVerbs.Post, "/redraw")]
    public async Task<string> RedrawActor([JsonData] RedrawRequest data)
    {
        Brio.Log.Debug("Received redraw request on WebAPI");
        try
        {
            var result = await _framework.RunOnTick(async () => await _redrawService.RedrawObjectByIndex(data.ObjectIndex));
            return result.ToString();
        }
        catch
        {
            HttpContext.Response.StatusCode = 500;
            return ActorRedrawService.RedrawResult.Failed.ToString();
        }
    }

    [Route(HttpVerbs.Post, "/spawn")]
    public async Task<int> Spawn()
    {
        Brio.Log.Debug("Received spawn request on WebAPI");
        try
        {
            ICharacter? character = null;
            var res = await _framework.RunOnFrameworkThread(() =>
            {
                if(_actorSpawnService.CreateCharacter(out ICharacter? chara, SpawnFlags.Default))
                {
                    character = chara;

                    return chara.ObjectIndex;
                }
                return -1;
            });

            if(character is not null)
            {
                await WaitForReadyToDraw(character);
            }

            return res;
        }
        catch
        {
            HttpContext.Response.StatusCode = 500;
            return -1;
        }
    }

    public unsafe Task WaitForReadyToDraw(IGameObject go)
    {
        return _framework.RunUntilSatisfied(
           () =>
           {
               if(go.IsValid())
                   return go.Native()->IsReadyToDraw();
               return false;
           },
           (_) => { },
           100,
           dontStartFor: 2
       );
    }

    [Route(HttpVerbs.Post, "/despawn")]
    public async Task<bool> Despawn([JsonData] DespawnRequest data)
    {
        Brio.Log.Debug("Received despawn request on WebAPI");
        try
        {
            var didDestroy = await _framework.RunOnFrameworkThread(() => _actorSpawnService.DestroyObject(data.ObjectIndex));
            return didDestroy;
        }
        catch
        {
            HttpContext.Response.StatusCode = 500;
            return false;
        }

    }

    [Route(HttpVerbs.Get, "/status")]
    public void GetStatus()
    {
        var status = new
        {
            status = "online",
            version = $"{Brio.MajorAPIVersion}.{Brio.MinorAPIVersion}",
            brioAvailable = true,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(status);
        var bytes = Encoding.UTF8.GetBytes(json);

        Response.ContentType = "application/json";
        Response.ContentLength64 = bytes.Length;
        Response.StatusCode = 200;
        Response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    [Route(HttpVerbs.Post, "/bones")]
    public void Bones([JsonData] Dictionary<string, BoneTransformDTO> data)
    {
        if(data == null || data.Count == 0)
        {
            return;
        }

        var objectid = Request.QueryString["objectid"];

        if(string.IsNullOrEmpty(objectid) || !uint.TryParse(objectid, out uint objectId))
        {
            Response.StatusCode = 400;
            return;
        }

        bool success = false;

        var boneTransforms = new Dictionary<string, BrioTransform>();

        Brio.Log.Debug($"Received bone transform update for object ID {objectId} with {data.Count} bones.");
        foreach(var kvp in data)
        {
            boneTransforms[kvp.Key] = new BrioTransform
            {
                Position = kvp.Value.Position?.ToVector3() ?? default,
                Rotation = kvp.Value.Rotation?.ToQuaternion() ?? default,
                Scale = kvp.Value.Scale?.ToVector3() ?? default
            };

            Brio.Log.Debug($"Bone: {kvp.Key}, Pos: {boneTransforms[kvp.Key].Position}, Rot: {boneTransforms[kvp.Key].Rotation}, Scale: {boneTransforms[kvp.Key].Scale}");   
        }

        success = skeletonService.SetBoneTransforms(objectId, boneTransforms);

        Response.StatusCode = 200;
    }
}

public class StreamClient : IDisposable
{
    private readonly HttpListenerContext context;
    private bool closed;

    public bool IsClosed => closed;

    public StreamClient(HttpListenerContext context)
    {
        this.context = context;
    }

    public async Task SendAsync(string message)
    {
        if(closed) return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            await context.Response.OutputStream.FlushAsync();
        }
        catch
        {
            closed = true;
            throw;
        }
    }

    public void Dispose()
    {
        closed = true;
        try
        {
            context.Response.Close();
        }
        catch { }
    }
}


public class DespawnRequest
{
    public int ObjectIndex { get; set; }
}

public class RedrawRequest
{
    public int ObjectIndex { get; set; }
}

//

[Serializable]
public class PoseableCharacter
{
    /// <summary>
    /// Unique identifier for this character
    /// </summary>
    public ulong ObjectId { get; set; }

    /// <summary>
    /// Character name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Raw pose data in JSON format (compatible with Brio/Anamnesis)
    /// </summary>
    public string? PoseDataJson { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

//

public class BoneTransformDTO
{
    public Vector3DTO? Position { get; set; }
    public QuaternionDTO? Rotation { get; set; }
    public Vector3DTO? Scale { get; set; }
}

public class Vector3DTO
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3 ToVector3() => new(X, Y, Z);
}

public class QuaternionDTO
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public Quaternion ToQuaternion() => new(X, Y, Z, W);
}
