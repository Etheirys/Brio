using Brio.Core;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;

using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using StructsObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Game.Actor.Extensions;

public static class GameObjectExtensions
{
    public static FontAwesomeIcon GetFriendlyIcon(this IGameObject go)
    {
        return go.ObjectKind switch
        {
            ObjectKind.Pc => FontAwesomeIcon.User,
            ObjectKind.Mount => FontAwesomeIcon.Horse,
            ObjectKind.EventNpc => FontAwesomeIcon.Robot,
            ObjectKind.Companion => FontAwesomeIcon.Paw,
            ObjectKind.Ornament => FontAwesomeIcon.Umbrella,
            ObjectKind.Retainer => FontAwesomeIcon.ConciergeBell,
            ObjectKind.BattleNpc => FontAwesomeIcon.Dog,
            _ => FontAwesomeIcon.Question
        };
    }

    public static string GetAsCustomName(this IGameObject go, string name)
    {
        return $"{name} ({go.ObjectIndex})";
    }

    public static string GetFriendlyName(this IGameObject go)
    {
        switch(go.ObjectKind)
        {
            case ObjectKind.Ornament:
                return $"Ornament ({go.ObjectIndex})";
            case ObjectKind.Mount:
                return $"Mount ({go.ObjectIndex})";
            default:
                return $"{go.Name} ({go.ObjectIndex})";
        }
    }

    public static string GetCensoredName(this IGameObject go)
    {
        if(go.ObjectIndex >= ActorTableHelpers.GPoseStart)
            return $"{(go.ObjectIndex - ActorTableHelpers.GPoseStart + 1).ToBrioName()} ({go.ObjectIndex})";

        return $"{((int)go.ObjectIndex).ToBrioName()} ({go.ObjectIndex})";
    }

    public static bool IsGPose(this IGameObject go)
    {
        return go.ObjectIndex >= ActorTableHelpers.GPoseStart && go.ObjectIndex <= ActorTableHelpers.GPoseEnd;
    }

    public static bool IsOverworld(this IGameObject go)
    {
        return go.ObjectIndex >= ActorTableHelpers.OverworldStart && go.ObjectIndex <= ActorTableHelpers.OverworldEnd;
    }

    public unsafe static StructsObject* Native(this IGameObject go)
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

    public unsafe static void SetName(this IGameObject gameObject, string name) => gameObject.Native()->SetName(name);

    public unsafe static void CalculateAndSetName(this ref StructsObject character, int index) => character.SetName(index.ToBrioName());

    public static unsafe T* GetDrawObject<T>(this IGameObject go) where T : unmanaged
    {
        return (T*)go.Native()->DrawObject;
    }
}
