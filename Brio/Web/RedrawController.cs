using Brio.Game.Actor;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Utils;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Brio.Web;

public class RedrawController : WebApiController
{
    [Route(HttpVerbs.Post, "/redraw")]
    
    public async Task<bool> RedrawActor([JsonData] RedrawRequest data)
    {
        try
        {
            return await FrameworkService.Instance.RunUntilSatisfied(
                () =>
                {
                    var gameObject = Dalamud.ObjectTable[data.ActorIndex];
                    if(gameObject == null)
                        return false;

                    return ActorRedrawService.Instance.CanRedraw(gameObject);
                },
                (success) =>
                {
                    if(success)
                    {
                        var actor = Dalamud.ObjectTable[data.ActorIndex];
                        if(actor != null)
                            ActorRedrawService.Instance.Redraw(actor, RedrawType.Standard);
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
}

public class RedrawRequest
{
    public int ActorIndex { get; set;  }
}
