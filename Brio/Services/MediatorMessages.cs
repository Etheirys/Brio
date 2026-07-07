
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace Brio.Services.MediatorMessages;

public record FrameworkUpdateMessage(IFramework Framework) : SameThreadMessage;
public record PriorityFrameworkUpdateMessage(IFramework Framework) : SameThreadMessage;
public record DelayedFrameworkUpdateMessage(IFramework Framework) : SameThreadMessage;

public record TerritoryChangedMessage(uint TerritoryId) : SameThreadMessage;

public record BrioInitializedMessage : MessageBase;

public record GposeStateChangedMessage(bool NewState) : SameThreadMessage;
public record GposeStartMessage : SameThreadMessage;
public record GposeEndMessage : MessageBase;

public record ActorSpawnedMessage(IGameObject GameObject) : MessageBase;
public record ActorDespawnedMessage(IGameObject GameObject) : MessageBase;
