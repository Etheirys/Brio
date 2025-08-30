using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;
using System.Threading.Tasks;

namespace Brio.Game.Actor;

public class ActorRedrawService(IFramework framework, IObjectTable objectTable)
{
    public delegate void ActorRedrawEventDelegate(IGameObject go, RedrawStage stage);

    public event ActorRedrawEventDelegate? ActorRedrawEvent;

    private readonly IFramework _framework = framework;
    private readonly IObjectTable _objectTable = objectTable;

    public Task<RedrawResult> RedrawObjectByIndex(int objectIndex)
    {
        var actor = _objectTable[objectIndex];
        if(actor == null)
            return Task.FromResult(RedrawResult.Failed);

        return Redraw(actor);
    }

    public async Task<RedrawResult> Redraw(IGameObject go)
    {
        Brio.Log.Debug($"Beginning Brio redraw on gameobject {go.ObjectIndex}...");
        DisableDraw(go);
        try
        {
            ActorRedrawEvent?.Invoke(go, RedrawStage.After);
            await DrawWhenReady(go);
            await WaitForDrawing(go);
            ActorRedrawEvent?.Invoke(go, RedrawStage.After);
            Brio.Log.Debug($"Brio redraw complete on gameobject {go.ObjectIndex}.");
            return RedrawResult.Full;
        }
        catch(Exception e)
        {
            Brio.Log.Error(e, $"Brio redraw failed on gameobject {go.ObjectIndex}.");
            return RedrawResult.Failed;
        }
    }

    public async Task RedrawAndWait(IGameObject go)
    {
        Brio.Log.Debug($"Beginning Brio redraw on gameobject {go.ObjectIndex}...");
        try
        {
            DisableDraw(go);

            ActorRedrawEvent?.Invoke(go, RedrawStage.Before);

            _ = DrawWhenReady(go);
            
            var start = DateTime.Now;         
            do
            {
                if(await _framework.RunOnFrameworkThread(() => IsDrawing(go)))
                {
                    Brio.Log.Debug($"Brio redraw complete on gameobject {go.ObjectIndex}.");
                   
                    ActorRedrawEvent?.Invoke(go, RedrawStage.After);

                    return;
                }

                await Task.Delay(200);
            } while(go.IsValid() && (DateTime.Now - start).TotalSeconds < 3);        
        }
        catch(Exception e)
        {
            Brio.Log.Error(e, $"Brio redraw failed on gameobject {go.ObjectIndex}.");
        }
    }

    public unsafe bool IsDrawing(IGameObject go)
    {
        var native = go.Native();
        if(native is null) return false;
        return native->RenderFlags == 0x00;
    }

    public unsafe void DisableDraw(IGameObject go)
    {
        var native = go.Native();
        native->DisableDraw();
    }

    public unsafe void EnableDraw(IGameObject go)
    {
        var native = go.Native();
        native->EnableDraw();
    }

    public unsafe Task DrawWhenReady(IGameObject go)
    {
        return _framework.RunUntilSatisfied(
           () => go.Native()->IsReadyToDraw(),
           (_) => EnableDraw(go),
           100,
           dontStartFor: 2
       );
    }

    public unsafe Task WaitForDrawing(IGameObject go)
    {
        return _framework.RunUntilSatisfied(
           () =>
           {
               var drawObject = go.Native()->DrawObject;
               if(drawObject == null)
                   return false;

               return drawObject->IsVisible;
           },
           (_) => { },
           100,
           dontStartFor: 2
           );
    }

    public enum RedrawResult
    {
        NoChange,
        Optmized,
        Full,
        Failed
    }

    public enum RedrawStage
    {
        Before,
        After
    }
}
