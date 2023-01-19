using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using StructsGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Utils;

public static class GameObjectExtensions
{
    public unsafe static void SetName(this ref StructsGameObject gameObject, string name)
    {
        for(int x = 0; x < name.Length; x++)
        {
            gameObject.Name[x] = (byte)name[x];
        }
        gameObject.Name[name.Length] = 0;
    }

    public unsafe static void SetName(this DalamudGameObject gameObject, string name) => gameObject.AsNative()->SetName(name);

    public unsafe static StructsGameObject* AsNative(this DalamudGameObject gameObject) => (StructsGameObject*)gameObject.Address;
}
