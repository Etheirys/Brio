using System;
using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Posing;
using Dalamud.Plugin.Services;

namespace Brio.Game.Scene;

internal class SceneService(EntityManager entityManager, PosingService posingService, IFramework framework)
{
    internal static SceneFile BuildSceneFile(EntityManager entityManager)
    {
        SceneFile sceneFile = new();
        
        var entity = entityManager.GetEntity<ActorContainerEntity>("actorContainer") 
                                      ?? throw new NullReferenceException("Error: Entity ActorContainerEntity may not be null!");
        
        foreach(var child in entity.Children)
        {
            if(child is ActorEntity actorEntity)
            {
                sceneFile.AddActor(actorEntity);
            }
        }
        
        return sceneFile;
    }

    internal void BuildScene(SceneFile sceneFile)
    {
        ActorContainerEntity actorContainerEntity = entityManager.GetEntity<ActorContainerEntity>("actorContainer") 
                                                    ?? throw new NullReferenceException("Error: Entity ActorContainerEntity may not be null!");
        
        var actorCapability = actorContainerEntity.GetCapability<ActorContainerCapability>();

        if(ConfigurationService.Instance.Configuration.SceneDestoryActorsBeforeImport)
        {
            actorCapability.DestroyAll();
        }
        
        foreach(ActorFile actorFile in sceneFile.Actors)
        {
            EntityId actorId = actorCapability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
            
            framework.RunOnTick(() =>
            {
                ApplyDataToActor(actorId, actorFile);
            }, delayTicks: 4); // Waiting 4 frames to give the Actor time to be attached
            
        }
    }
    private void ApplyDataToActor(EntityId actorId, ActorFile actorFile)
    {
        var attachedActor = entityManager.GetEntity<ActorEntity>(actorId) ?? throw new NullReferenceException("Error: Failed to import Actor");
        var posingCapability = attachedActor.GetCapability<PosingCapability>();
        var appearanceCapability = attachedActor.GetCapability<ActorAppearanceCapability>();

        attachedActor.FriendlyName = actorFile.FriendlyName;
        
        var poseOptions = posingService.DefaultImporterOptions;
        poseOptions.ApplyModelTransform = ConfigurationService.Instance.Configuration.Import.ApplyModelTransform;

        poseOptions.PositionTransformType = ConfigurationService.Instance.Configuration.Import.PositionTransformType;
        poseOptions.RotationTransformType = ConfigurationService.Instance.Configuration.Import.RotationTransformType;
        poseOptions.ScaleTransformType = ConfigurationService.Instance.Configuration.Import.ScaleTransformType;
            
        posingCapability.ImportPose(actorFile.PoseFile, poseOptions);
        
        _ = appearanceCapability.SetAppearance(actorFile.AnamnesisCharaFile, AppearanceImportOptions.Default);
    }
}
