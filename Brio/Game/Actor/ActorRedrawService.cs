using Brio.Utils;
using Dalamud.Game.ClientState.Objects.Types;
using Penumbra.Api;
using System;
using System.Collections.Generic;
using PenumbraRedrawType = Penumbra.Api.Enums.RedrawType;

namespace Brio.Game.Actor;

public class ActorRedrawService : IDisposable
{
    public unsafe bool CanRedraw(GameObject gameObject) => !_redrawsActive.Contains(gameObject.AsNative()->ObjectIndex);

    private List<int> _redrawsActive = new();

    public unsafe void Redraw(GameObject gameObject, RedrawType redrawType, bool preservePosition = true)
    {
        var raw = gameObject.AsNative();
        var index = raw->ObjectIndex;
        _redrawsActive.Add(index);

        var originalPositon = raw->DrawObject->Object.Position;
        var originalRotation = raw->DrawObject->Object.Rotation;

        var npcOverrideEnabled = Brio.RenderHooks.ApplyNPCOverride;
        if (redrawType == RedrawType.ForceNPCAppearance)
            Brio.RenderHooks.ApplyNPCOverride = true;

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

        if (redrawType == RedrawType.ForceNPCAppearance)
            Brio.RenderHooks.ApplyNPCOverride = npcOverrideEnabled;

        if (preservePosition)
        {
            Brio.FrameworkUtils.RunUntilSatisfied(() => raw->RenderFlags == 0,
            (_) =>
            {
                raw->DrawObject->Object.Rotation = originalRotation;
                raw->DrawObject->Object.Position = originalPositon;
                _redrawsActive.Remove(index);
            },
            50,
            1,
            true);
        }
        else
        {
            _redrawsActive.Remove(index);
        }

    }

    public void Dispose()
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