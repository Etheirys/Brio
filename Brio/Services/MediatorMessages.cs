
using Dalamud.Game.ClientState.Objects.Types;

namespace Brio.Services.MediatorMessages;

public record FrameworkUpdateMessage : SameThreadMessage;
public record PriorityFrameworkUpdateMessage : SameThreadMessage;
public record DelayedFrameworkUpdateMessage : SameThreadMessage;

public record BrioInitializedMessage : MessageBase;

public record GposeStartMessage : SameThreadMessage;
public record GposeEndMessage : MessageBase;

public record ActorSpawnedMessage(IGameObject GameObject) : MessageBase;
public record ActorDespawnedMessage(IGameObject GameObject) : MessageBase;
