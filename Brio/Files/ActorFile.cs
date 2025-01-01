using System;
using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Entities.Actor;

namespace Brio.Files;

[Serializable]
internal class ActorFile
{
    public string FriendlyName { get; set; } = "";
    
    public required AnamnesisCharaFile AnamnesisCharaFile { get; set; }
    public required PoseFile PoseFile { get; set; }

    public static implicit operator ActorFile(ActorEntity actorEntity)
    {   
        
        var appearanceCapability = actorEntity.GetCapability<ActorAppearanceCapability>();
        var posingCapability = actorEntity.GetCapability<PosingCapability>();

        posingCapability.GeneratePoseFile();
        
        var actorFile = new ActorFile
        {
            FriendlyName = actorEntity.RawName,
            AnamnesisCharaFile = appearanceCapability.CurrentAppearance,
            PoseFile = posingCapability.GeneratePoseFile()
        };
        return actorFile;
        
    }
}
