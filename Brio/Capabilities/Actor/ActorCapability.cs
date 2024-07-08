using Brio.Capabilities.Core;
using Brio.Entities.Actor;
using Dalamud.Game.ClientState.Objects.Types;

namespace Brio.Capabilities.Actor;

internal abstract class ActorCapability(ActorEntity parent) : Capability(parent)
{
    public ActorEntity Actor => (ActorEntity)Entity;

    public IGameObject GameObject => Actor.GameObject;
}

internal abstract class ActorCharacterCapability(ActorEntity parent) : ActorCapability(parent)
{
    public ICharacter Character => (ICharacter)GameObject;
}
