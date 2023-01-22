using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using StructsBattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;

namespace Brio.Game.Actor.Extensions;

public static class BattleCharaExtensions
{
    public unsafe static StructsBattleChara* AsNative(this BattleChara battleChara) => (StructsBattleChara*)battleChara.Address;
    public unsafe static StatusManager* GetStatusManager(this BattleChara battleChara) => &battleChara.AsNative()->StatusManager;
}
