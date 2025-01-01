using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Brio.Game.Posing;
using Dalamud.Plugin.Services;
using System.Threading.Tasks;

namespace Brio.Game.Scene;

internal class SceneService(EntityManager _entityManager, PosingService _posingService, IFramework _framework)
{
    internal static SceneFile GenerateSceneFile(EntityManager entityManager)
    {
        SceneFile sceneFile = new();

        var entity = entityManager.GetEntity<ActorContainerEntity>("actorContainer")!;

        foreach(var child in entity.Children)
        {
            if(child is ActorEntity actorEntity)
            {
                sceneFile.AddActor(actorEntity);
            }
        }

        return sceneFile;
    }

    internal unsafe void LoadScene(SceneFile sceneFile)
    {
        ActorContainerEntity actorContainerEntity = _entityManager.GetEntity<ActorContainerEntity>("actorContainer")!;

        var actorCapability = actorContainerEntity.GetCapability<ActorContainerCapability>();

        if(ConfigurationService.Instance.Configuration.SceneDestoryActorsBeforeImport)
        {
            actorCapability.DestroyAll();
        }

        foreach(ActorFile actorFile in sceneFile.Actors)
        {
            var (actorId, actor) = actorCapability.CreateCharacter(false, false, forceSpawnActorWithoutCompanion: true);

            _framework.RunUntilSatisfied(
                () => actor.Native()->IsReadyToDraw(),
                (__) =>
                {
                    _ = ApplyDataToActor(actorId, actorFile);
                },
                100,
                dontStartFor: 2
            );
        }
    }

    private async Task ApplyDataToActor(EntityId actorId, ActorFile actorFile)
    {
        var attachedActor = _entityManager.GetEntity<ActorEntity>(actorId)!;
        var posingCapability = attachedActor.GetCapability<PosingCapability>();
        var appearanceCapability = attachedActor.GetCapability<ActorAppearanceCapability>();
        var actionTimeline = attachedActor.GetCapability<ActionTimelineCapability>();

        attachedActor.FriendlyName = actorFile.FriendlyName;

        actionTimeline.SetOverallSpeedOverride(0);

        await _framework.RunOnTick(async () =>
        {
            await appearanceCapability.SetAppearance(actorFile.AnamnesisCharaFile, AppearanceImportOptions.Default);

            await _framework.RunOnTick(() =>
            {
                posingCapability.ImportPose(actorFile.PoseFile, null, asScene: true);
            }, delayTicks: 10); // I dont like having to-do this but I dont think I have another way without rework
        });
    }
}
