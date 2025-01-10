using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Entities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Types;
using System;

namespace Brio.Files;

[Serializable]
internal class ActorFile
{
    public string Name { get; set; } = "";

    public required AnamnesisCharaFile AnamnesisCharaFile { get; set; }
    public required PoseFile PoseFile { get; set; }

    public bool HasChild { get; set; }
    public ChildActor? Child { get; set; }

    public bool HeadSlotShown { get; set; }
    public bool MainHandSlotShown { get; set; }
    public bool OffHandSlotShown { get; set; }

    public bool ActorFrozen { get; set; }
    public bool HasBaseAnimation { get; set; }
    public int BaseAnimation { get; set; }

    public static implicit operator ActorFile(ActorEntity actorEntity)
    {
        var appearanceCapability = actorEntity.GetCapability<ActorAppearanceCapability>();
        var posingCapability = actorEntity.GetCapability<PosingCapability>();

        var actorFile = new ActorFile
        {
            Name = actorEntity.RawName,
            AnamnesisCharaFile = appearanceCapability.CurrentAppearance,
            PoseFile = posingCapability.GeneratePoseFile()
        };

        CompanionContainer? companionContainer;

        if(posingCapability.Character.HasSpawnedCompanion())
        {
            var companionCapability = actorEntity.GetCapability<CompanionCapability>();

            companionContainer = companionCapability.Character.GetCompanionInfo();

            actorFile.HasChild = true;
            actorFile.Child = new ChildActor() { Companion = companionContainer.Value };


            var companionEntity = companionCapability.GetCompanionAsEntity();

            if(companionEntity is not null && companionEntity.TryGetCapability<PosingCapability>(out var companionPosingCapability))
            {
                actorFile.Child.PoseFile = companionPosingCapability.GeneratePoseFile();
            }
        }

        return actorFile;
    }
}

[Serializable]
internal class ChildActor
{
    public required CompanionContainer Companion { get; set; }

    public PoseFile? PoseFile { get; set; }
}
