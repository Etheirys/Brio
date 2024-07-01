using Dalamud.Game.ClientState.Objects.Types;

using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using NativeIGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Entities.Core;

internal record struct EntityId(string Unique)
{
    public static implicit operator EntityId(Entity entity)
    {
        return entity.Id;
    }

    public static implicit operator EntityId(string id)
    {
        return new EntityId(id);
    }

    public static implicit operator EntityId(IGameObjectHolder go)
    {
        return new EntityId($"actor_{go.IGameObject.Address}");
    }

    public unsafe static implicit operator EntityId(NativeCharacter* chara)
    {
        return new EntityId($"actor_{(nint)chara}");
    }

    public unsafe static implicit operator EntityId(NativeIGameObject* go)
    {
        return new EntityId($"actor_{(nint)go}");
    }
}

internal record IGameObjectHolder(IGameObject IGameObject);
