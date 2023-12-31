using Dalamud.Plugin.Services;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Brio.Game.Actor;
using System.Threading.Tasks;

namespace Brio.Web;

internal class ActorWebController(IFramework framework, ActorSpawnService actorSpawnService, ActorRedrawService redrawService) : WebApiController
{
    private readonly IFramework _framework = framework;
    private readonly ActorSpawnService _actorSpawnService = actorSpawnService;
    private readonly ActorRedrawService _redrawService = redrawService;

    [Route(HttpVerbs.Post, "/redraw")]
    public async Task<string> RedrawActor([JsonData] RedrawRequest data)
    {
        Brio.Log.Debug("Received redraw request on WebAPI");
        try
        {
            var result = await _framework.RunOnFrameworkThread(async () => await _redrawService.RedrawActor(data.ObjectIndex));
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
            return await _framework.RunOnFrameworkThread(() =>
            {
                if (_actorSpawnService.CreateCharacter(out var chara, SpawnFlags.Default))
                    return chara.ObjectIndex;
                return -1;
            });
        }
        catch
        {
            HttpContext.Response.StatusCode = 500;
            return -1;
        }
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
}

public class DespawnRequest
{
    public int ObjectIndex { get; set; }
}

public class RedrawRequest
{
    public int ObjectIndex { get; set; }
}