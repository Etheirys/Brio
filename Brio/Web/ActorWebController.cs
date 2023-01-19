using Brio.Game.Actor;
using Brio.Game.Core;
using Brio.Game.GPose;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Threading.Tasks;

namespace Brio.Web;

public class ActorWebController : WebApiController
{
    [Route(HttpVerbs.Post, "/redraw")]
    public async Task<bool> RedrawActor([JsonData] RedrawRequest data)
    {
        try
        {
            return await Dalamud.Framework.RunUntilSatisfied(
                () =>
                {
                    var gameObject = Dalamud.ObjectTable[data.ObjectIndex];
                    if(gameObject == null)
                        return false;

                    return ActorRedrawService.Instance.CanRedraw(gameObject);
                },
                (success) =>
                {
                    if(success)
                    {
                        var actor = Dalamud.ObjectTable[data.ObjectIndex];
                        if(actor != null)
                            ActorRedrawService.Instance.Redraw(actor, data.RedrawType ?? RedrawType.ForceNPCAppearance);
                    }

                },
                30);
        }
        catch
        {
            HttpContext.Response.StatusCode = 500;
            return false;
        }
    }

    [Route(HttpVerbs.Post, "/spawn")]
    public async Task<int> SpawnActor()
    {
        try
        {
            return await Dalamud.Framework.RunOnFrameworkThread(() =>
            {
                if(!GPoseService.Instance.IsInGPose)
                    return -1;

                var actorId = ActorSpawnService.Instance.Spawn();
                if(actorId == null)
                    return -1;

                return (int)actorId;
            });
        }
        catch
        {
            HttpContext.Response.StatusCode = 500;
            return -1;
        }
    }
}

public class RedrawRequest
{
    public int ObjectIndex { get; set; }
    public RedrawType? RedrawType { get; set; }
}
