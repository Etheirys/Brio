using Brio.Game.Actor;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using StructsGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Game.Core;

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

    public unsafe static bool IsGPoseActor(this DalamudGameObject gameObject) => ActorService.IsGPoseActor(gameObject);
    public unsafe static bool IsGPoseActor(this ref StructsGameObject gameObject) => ActorService.IsGPoseActor(gameObject.ObjectIndex);

    public static bool IsBattleNPC(this ref StructsGameObject gameObject) => gameObject.ObjectKind == (int)ObjectKind.Pc || gameObject.ObjectKind == (int)ObjectKind.BattleNpc || gameObject.ObjectKind == (int)ObjectKind.Ornament;
    public unsafe static bool IsBattleNPC(this DalamudGameObject gameObject) => gameObject.AsNative()->IsBattleNPC();

}
