using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Actor.Interop;
using Brio.Game.Types;
using MessagePack;
using System;

namespace Brio.Files;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ActorFile
{
    public string Name { get; set; } = "";

    public string FriendlyName { get; set; } = "Actor";

    public required AnamnesisCharaFile AnamnesisCharaFile { get; set; }
    public required PoseFile PoseFile { get; set; }

    public bool HasChild { get; set; }
    public ChildActor? Child { get; set; }
    public PropData? PropData { get; set; }

    public bool HeadSlotShown { get; set; }
    public bool MainHandSlotShown { get; set; }
    public bool OffHandSlotShown { get; set; }

    public bool ActorFrozen { get; set; }
    public bool HasBaseAnimation { get; set; }
    public int BaseAnimation { get; set; }

    public bool IsProp { get; set; }

    public bool HasPenumbra { get; set; }
    public bool HasGlamourer { get; set; }
    public bool HasCustomizePlus { get; set; }

    public bool WasMCDF { get; set; }
    public bool WasOtherPlayer { get; set; }

    public Guid? PenumbraCollection { get; set; }
    public Guid? GlamourerDesign { get; set; }

    public Guid? CustomizePlusProfile { get; set; }

    public static unsafe implicit operator ActorFile(ActorEntity actorEntity)
    {
        var appearanceCapability = actorEntity.GetCapability<ActorAppearanceCapability>();
        var posingCapability = actorEntity.GetCapability<PosingCapability>();
        var modelCapability = actorEntity.GetCapability<ModelPosingCapability>();

        ActorAppearanceExtended anaCharaFile = new() { Appearance = appearanceCapability.CurrentAppearance };
        BrioHuman.ShaderParams* shaderParams = appearanceCapability.Character.GetShaderParams();

        // append shader params to appearance if they exist
        if(shaderParams is not null)
        {
            anaCharaFile.ShaderParams = *shaderParams;
        }

        var actorFile = new ActorFile
        {
            Name = actorEntity.RawName,
            FriendlyName = actorEntity.FriendlyName,
            AnamnesisCharaFile = anaCharaFile,
            PoseFile = posingCapability.GeneratePoseFile(),
            IsProp = actorEntity.IsProp,
            PropData = new PropData
            {
                PropTransformAbsolute = modelCapability.Transform,
                PropTransformDifference = modelCapability.Transform.CalculateDiff(modelCapability.OriginalTransform)
            }
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
[MessagePackObject(keyAsPropertyName: true)]
public class ChildActor
{
    public required CompanionContainer Companion { get; set; }

    public PoseFile? PoseFile { get; set; }

    public bool HasPenumbra { get; set; }

    public bool WasMCDF { get; set; }

    public Guid? PenumbraCollection { get; set; }

}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class PropData
{
    public Transform PropTransformDifference { get; set; }
    public Transform PropTransformAbsolute { get; set; }
}
