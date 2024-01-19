using Brio.Core;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using StructsObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Game.Actor.Extensions;

internal static class GameObjectExtensions
{
    public static FontAwesomeIcon GetFriendlyIcon(this GameObject go)
    {
        return go.ObjectKind switch
        {
            ObjectKind.Player => FontAwesomeIcon.User,
            ObjectKind.MountType => FontAwesomeIcon.Horse,
            ObjectKind.EventNpc => FontAwesomeIcon.Robot,
            ObjectKind.Companion => FontAwesomeIcon.Paw,
            ObjectKind.Ornament => FontAwesomeIcon.Umbrella,
            ObjectKind.Retainer => FontAwesomeIcon.ConciergeBell,
            ObjectKind.BattleNpc => FontAwesomeIcon.Dog,
            _ => FontAwesomeIcon.Question
        };
    }

    public static string GetFriendlyName(this GameObject go)
    {
        switch(go.ObjectKind)
        {
            case ObjectKind.Ornament:
                return $"Ornament ({go.ObjectIndex})";
            case ObjectKind.MountType:
                return $"Mount ({go.ObjectIndex})";
            default:
                return $"{go.Name} ({go.ObjectIndex})";
        }
    }

    public static string GetCensoredName(this GameObject go)
    {
        if(go.ObjectIndex >= ActorTableHelpers.GPoseStart)
            return $"{(go.ObjectIndex - ActorTableHelpers.GPoseStart + 1).ToBrioName()} ({go.ObjectIndex})";

        return $"{((int)go.ObjectIndex).ToBrioName()} ({go.ObjectIndex})";
    }

    public static bool IsGPose(this GameObject go)
    {
        return go.ObjectIndex >= ActorTableHelpers.GPoseStart && go.ObjectIndex <= ActorTableHelpers.GPoseEnd;
    }

    public static bool IsOverworld(this GameObject go)
    {
        return go.ObjectIndex >= ActorTableHelpers.OverworldStart && go.ObjectIndex <= ActorTableHelpers.OverworldEnd;
    }

    public unsafe static StructsObject* Native(this GameObject go)
    {
        return (StructsObject*)go.Address;
    }

    public unsafe static void SetName(this ref StructsObject gameObject, string name)
    {
        for(int x = 0; x < name.Length; x++)
        {
            gameObject.Name[x] = (byte)name[x];
        }
        gameObject.Name[name.Length] = 0;
    }

    public unsafe static void SetName(this GameObject gameObject, string name) => gameObject.Native()->SetName(name);

    public unsafe static void CalculateAndSetName(this ref StructsObject gameObject, int index) => gameObject.SetName(index.ToBrioName());

    public unsafe static void CalculateAndSetName(this GameObject gameObject, int index) => gameObject.Native()->CalculateAndSetName(index);

    public static unsafe T* GetDrawObject<T>(this GameObject go) where T : unmanaged
    {
        return (T*)go.Native()->DrawObject;
    }
}
