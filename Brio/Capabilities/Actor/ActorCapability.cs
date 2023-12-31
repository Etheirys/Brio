using Dalamud.Game.ClientState.Objects.Types;
using Brio.Capabilities.Core;
using Brio.Entities.Actor;

namespace Brio.Capabilities.Actor;

internal abstract class ActorCapability(ActorEntity parent) : Capability(parent)
{
    public ActorEntity Actor => (ActorEntity)Entity;

    public GameObject GameObject => Actor.GameObject;
}

internal abstract class ActorCharacterCapability(ActorEntity parent) : ActorCapability(parent)
{
    public Character Character => (Character)GameObject;
}