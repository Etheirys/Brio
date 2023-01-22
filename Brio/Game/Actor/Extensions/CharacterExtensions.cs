using Dalamud.Game.ClientState.Objects.Types;
using StructsCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Game.Actor.Extensions;

public static class CharacterExtensions
{
    public unsafe static StructsCharacter* AsNative(this Character chara) => (StructsCharacter*)chara.Address;
}
