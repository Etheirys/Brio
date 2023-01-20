using Brio.Core;
using Brio.Game.Core;
using Brio.Game.Render;
using Dalamud.Game.ClientState.Objects.Types;
using Penumbra.Api;
using System.Collections.Generic;
using PenumbraRedrawType = Penumbra.Api.Enums.RedrawType;

namespace Brio.Game.Actor;

public class ActorRedrawService : ServiceBase<ActorRedrawService>
{
    public unsafe bool CanRedraw(GameObject gameObject) => !_redrawsActive.Contains(gameObject.AsNative()->ObjectIndex);

    private List<int> _redrawsActive = new();

    public unsafe bool Redraw(GameObject gameObject, RedrawType redrawType, bool preservePosition = true)
    {
        if(!CanRedraw(gameObject))
            return false;

        var raw = gameObject.AsNative();
        var index = raw->ObjectIndex;
        _redrawsActive.Add(index);

        var originalPositon = raw->DrawObject->Object.Position;
        var originalRotation = raw->DrawObject->Object.Rotation;

        var npcOverrideEnabled = RenderHookService.Instance.ApplyNPCOverride;
        if(redrawType == RedrawType.ForceNPCAppearance)
            RenderHookService.Instance.ApplyNPCOverride = true;

        switch(redrawType)
        {
            case RedrawType.ForceNPCAppearance:
            case RedrawType.Standard:
                raw->DisableDraw();
                raw->EnableDraw();
                break;

            case RedrawType.Penumbra:
                Ipc.RedrawObjectByIndex.Subscriber(Dalamud.PluginInterface).Invoke(index, PenumbraRedrawType.Redraw);
                break;
        }

        if(redrawType == RedrawType.ForceNPCAppearance)
            RenderHookService.Instance.ApplyNPCOverride = npcOverrideEnabled;

        if(preservePosition)
        {
            Dalamud.Framework.RunUntilSatisfied(() => raw->RenderFlags == 0,
            (_) =>
            {
                raw->DrawObject->Object.Rotation = originalRotation;
                raw->DrawObject->Object.Position = originalPositon;
                _redrawsActive.Remove(index);
            },
            50,
            3,
            true);
        }
        else
        {
            _redrawsActive.Remove(index);
        }

        return true;
    }

    public override void Dispose()
    {
        _redrawsActive.Clear();
    }
}

public enum RedrawType
{
    Standard,
    ForceNPCAppearance,
    Penumbra
}
