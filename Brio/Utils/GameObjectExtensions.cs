using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Brio.Utils;

public static class GameObjectExtensions
{
    public unsafe static void SetName(this ref GameObject gameObject, string name)
    {
        for (int x = 0; x < name.Length; x++)
        {
            gameObject.Name[x] = (byte)name[x];
        }
        gameObject.Name[name.Length] = 0;
    }
}
