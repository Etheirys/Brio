using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Actor.Interop;
using Brio.Game.Types;
using MessagePack;
using System;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ActorDTO
{
    public string Name { get; set; } = "";
    public string FriendlyName { get; set; } = "Actor";

    public required AnamnesisCharaFile AnamnesisCharaFile { get; set; }
    public required PoseFile PoseFile { get; set; }

    public bool HasChild { get; set; }
    public ChildActor? Child { get; set; }

    [Obsolete]
    public PropData? PropData { get; set; }

    public bool HeadSlotShown { get; set; }
    public bool MainHandSlotShown { get; set; }
    public bool OffHandSlotShown { get; set; }

    public bool ActorFrozen { get; set; }
    public bool HasBaseAnimation { get; set; }
    public int BaseAnimation { get; set; }

    [Obsolete]
    public bool IsProp { get; set; }

    public bool HasPenumbra { get; set; }
    public bool HasGlamourer { get; set; }
    public bool HasCustomizePlus { get; set; }

    public bool WasMCDF { get; set; } = false;
    public bool WasOtherPlayer { get; set; } = false;

    public Guid? PenumbraCollection { get; set; }
    public Guid? GlamourerDesign { get; set; }

    public Guid? CustomizePlusProfile { get; set; }

    public string? ParentFolderId { get; set; }

    public string? GlamourerDesignBase64 { get; set; }


    public static unsafe implicit operator ActorDTO(ActorEntity actorEntity)
    {
        var appearanceCapability = actorEntity.GetCapability<ActorAppearanceCapability>();
        var posingCapability = actorEntity.GetCapability<PosingCapability>();
        //var modelCapability = actorEntity.GetCapability<ModelPosingCapability>();

        ActorAppearanceExtended anaCharaFile = new() { Appearance = appearanceCapability.CurrentAppearance };
        BrioHuman.ShaderParams* shaderParams = appearanceCapability.Character.GetShaderParams();

        // append shader params to appearance if they exist
        if(shaderParams is not null)
        {
            anaCharaFile.ShaderParams = *shaderParams;
        }

        var actorFile = new ActorDTO
        {
            Name = actorEntity.RawName,
            FriendlyName = actorEntity.FriendlyName,
            AnamnesisCharaFile = anaCharaFile,
            PoseFile = posingCapability.ExportPoseAsFileData(),
        };

        var mcdfState = appearanceCapability.HasMCDF;
        var syncState = actorEntity.IsSynced;

        if(mcdfState || syncState)
        {
            actorFile.WasOtherPlayer = true;
            actorFile.WasMCDF = true;
        }

        {
            var state = appearanceCapability.HasClollectionOverride();
            var stae2 = appearanceCapability.HasDesignOverride();
            var stae3 = appearanceCapability.HasProfileOverride();

            if(appearanceCapability.HasGlamourerIntegration)
            {
                actorFile.HasGlamourer = true;
                actorFile.GlamourerDesignBase64 = appearanceCapability.GetCurrentDesign();

                if(stae2.Has == true)
                {
                    actorFile.GlamourerDesign = stae2.Id;
                }
            }

            if(state.Has)
            {
                actorFile.HasPenumbra = true;
                actorFile.PenumbraCollection = state.Id;
            }

            if(stae3.Has)
            {
                actorFile.HasCustomizePlus = true;
                actorFile.CustomizePlusProfile = stae3.Id;
            }

            Brio.Log.Debug($"actor state: {actorEntity.GameObject.ObjectIndex}. Collection: {state.Has} - {state.Id}, Design: {stae2.Has} - {stae2.Id}, Profile: {stae3.Has} - {stae3.Id}, GDesign: {stae2.Has == false}");
        }

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
                actorFile.Child.PoseFile = companionPosingCapability.ExportPoseAsFileData();
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

[Obsolete]
[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class PropData
{
    public Transform PropTransformDifference { get; set; }
    public Transform PropTransformAbsolute { get; set; }
}
