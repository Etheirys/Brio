using Dalamud.Game.ClientState.Objects.Enums;
using System;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Game.Actor;

public class ActorRedrawService : IDisposable
{
    public unsafe void StandardRedraw(DalamudGameObject gameObject)
    {
        GameObject* raw = (GameObject*)gameObject.Address;
        raw->DisableDraw();
        raw->EnableDraw();
    }

    public unsafe void ModernNPCHackRedraw(DalamudGameObject gameObject)
    {
        var wasEnabled = Brio.RenderHooks.ApplyNPCOverride;

        Brio.RenderHooks.ApplyNPCOverride = true;

        GameObject* raw = (GameObject*)gameObject.Address;
        raw->DisableDraw();
        raw->EnableDraw();

        Brio.RenderHooks.ApplyNPCOverride = wasEnabled;

    }

    public unsafe void LegacyNPCHackRedraw(DalamudGameObject gameObject)
    {
        GameObject* raw = (GameObject*)gameObject.Address;
        if (raw->ObjectKind == (byte)ObjectKind.Player)
        {
            raw->DisableDraw();
            raw->ObjectKind = (byte)ObjectKind.BattleNpc;
            raw->EnableDraw();
            raw->ObjectKind = (byte)ObjectKind.Player;
        }
    }

    public void Dispose()
    {

    }
}
