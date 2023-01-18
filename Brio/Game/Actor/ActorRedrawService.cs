using Brio.Utils;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;

namespace Brio.Game.Actor;

public class ActorRedrawService : IDisposable
{
    public bool CanRedraw { get; private set; } = true;

    public unsafe void Redraw(GameObject gameObject, RedrawType redrawType, bool preservePosition = true)
    {
        CanRedraw = false;

        var raw = gameObject.AsNative();

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
                Brio.PenumbraIPC.RawPenumbraRefresh(raw->ObjectIndex);
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
                CanRedraw = true;
            },
            50,
            1,
            true);
        }
        else
        {
            CanRedraw = true;
        }

    }


    public void Dispose()
    {
        CanRedraw = true;
    }
}

public enum RedrawType
{
    Standard,
    ForceNPCAppearance,
    Penumbra
}