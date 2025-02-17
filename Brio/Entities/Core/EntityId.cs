using Dalamud.Game.ClientState.Objects.Types;

using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using NativeGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Entities.Core;

public record struct EntityId(string Unique)
{
    public EntityId(IGameObject go) : this($"actor_{go.Address}")
    {
    }
    public EntityId(CameraId id) : this($"camera_{id}")
    {
    }

    public static implicit operator EntityId(Entity entity)
    {
        return entity.Id;
    }

    public static implicit operator EntityId(string id)
    {
        return new EntityId(id);
    }

    public static implicit operator EntityId(CameraId id)
    {
        return new EntityId(id);
    }

    public unsafe static implicit operator EntityId(NativeCharacter* chara)
    {
        return new EntityId($"actor_{(nint)chara}");
    }

    public unsafe static implicit operator EntityId(NativeGameObject* go)
    {
        return new EntityId($"actor_{(nint)go}");
    }
}
