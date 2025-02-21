using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Capabilities.World;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Files;
using Brio.Game.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Game.Core;
using Brio.Game.Posing;
using Dalamud.Plugin.Services;
using System.Threading.Tasks;

namespace Brio.Game.Scene;

public class SceneService(EntityManager _entityManager, VirtualCameraManager _virtualCameraManager, PosingService _posingService, ActorSpawnService _actorSpawnService, IClientState _clientState, IFramework _framework)
{
    public bool IsLoading { get; private set; }

    public SceneFile GenerateSceneFile()
    {
        SceneFile sceneFile = new();

        var entity = _entityManager.GetEntity<ActorContainerEntity>("actorContainer")!;

        foreach(var child in entity.Children)
        {
            if(child is ActorEntity actorEntity)
            {
                sceneFile.Actors.Add(actorEntity);
            }
        }

        foreach(var camera in _virtualCameraManager.GetAllCameras())
        {
            sceneFile.GameCameras.Add(new GameCameraFile { Camera = camera.VirtualCamera, CameraType = camera.CameraType });
        }

        var environmentEntity = _entityManager.GetEntity<EnvironmentEntity>("environment");

        if(environmentEntity is not null)
        {
            var tc = environmentEntity.GetCapability<TimeCapability>();
            var wc = environmentEntity.GetCapability<WeatherCapability>();
            var wrc = environmentEntity.GetCapability<WorldRenderingCapability>();
            sceneFile.EnvironmentData = new EnvironmentData
            {
                CurrentWeather = wc.WeatherService.CurrentWeather,
                IsTimeFrozen = tc.TimeService.IsTimeFrozen,
                EorzeaTime = tc.TimeService.EorzeaTime,
                DayOfMonth = tc.TimeService.DayOfMonth,
                MinuteOfDay = tc.TimeService.MinuteOfDay,
                IsWaterFrozen = wrc.WorldRenderingService.IsWaterFrozen
            };
        }

        return sceneFile;
    }

    public unsafe void LoadScene(SceneFile sceneFile, bool destroyAll = false)
    {
        IsLoading = true;

        ActorContainerEntity actorContainerEntity = _entityManager.GetEntity<ActorContainerEntity>("actorContainer")!;

        var actorCapability = actorContainerEntity.GetCapability<ActorContainerCapability>();

        if(ConfigurationService.Instance.Configuration.SceneDestoryActorsBeforeImport || destroyAll)
        {
            actorCapability.DestroyAll();
            _virtualCameraManager.DestroyAll();
        }

        foreach(ActorFile actorFile in sceneFile.Actors)
        {
            if(actorFile.IsProp)
            {
                var (actorId, actor) = actorCapability.CreateProp(false);

                _framework.RunUntilSatisfied(
                    () => actor.Native()->IsReadyToDraw(),
                    (__) =>
                    {
                        _ = LoadProp(actorId, actorFile);
                    },
                    100,
                    dontStartFor: 2
                );
            }
            else
            {
                var (actorId, actor) = actorCapability.CreateCharacter(actorFile.HasChild, false, forceSpawnActorWithoutCompanion: !actorFile.HasChild);

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

        foreach(GameCameraFile item in sceneFile.GameCameras)
        {
            _virtualCameraManager.CreateCamera(item.CameraType, false, false, item.Camera);
        }

        if(sceneFile.EnvironmentData is not null)
        {
            var environmentEntity = _entityManager.GetEntity<EnvironmentEntity>("environment")!;
            var tc = environmentEntity.GetCapability<TimeCapability>();
            var wc = environmentEntity.GetCapability<WeatherCapability>();
            var wrc = environmentEntity.GetCapability<WorldRenderingCapability>();

            wc.WeatherService.CurrentWeather = sceneFile.EnvironmentData.CurrentWeather;
            tc.TimeService.IsTimeFrozen = sceneFile.EnvironmentData.IsTimeFrozen;
            tc.TimeService.EorzeaTime = sceneFile.EnvironmentData.EorzeaTime;
            tc.TimeService.DayOfMonth = sceneFile.EnvironmentData.DayOfMonth;
            tc.TimeService.MinuteOfDay = sceneFile.EnvironmentData.MinuteOfDay;
            wrc.WorldRenderingService.IsWaterFrozen = sceneFile.EnvironmentData.IsWaterFrozen;
        }

        _framework.RunOnTick(() =>
        {
            IsLoading = false;
        }, delayTicks: 250);
    }

    private async Task LoadProp(EntityId actorId, ActorFile actorFile)
    {
        var attachedActor = _entityManager.GetEntity<ActorEntity>(actorId)!;
        var modelCapability = attachedActor.GetCapability<ModelPosingCapability>();
        var appearanceCapability = attachedActor.GetCapability<ActorAppearanceCapability>();

        await _framework.RunOnTick(async () =>
        {
            if(actorFile.PropData is not null)
                modelCapability.Transform += actorFile.PropData.PropTransformDifference;

            await _framework.RunOnTick(async () =>
            {
                await appearanceCapability.SetAppearance(actorFile.AnamnesisCharaFile, AppearanceImportOptions.Weapon);
                await _framework.RunOnTick(() =>
                {
                    appearanceCapability.AttachWeapon();
                }, delayTicks: 10);
            }, delayTicks: 10);
        }, delayTicks: 2);
    }

    private async Task ApplyDataToActor(EntityId actorId, ActorFile actorFile)
    {
        var attachedActor = _entityManager.GetEntity<ActorEntity>(actorId)!;
        var posingCapability = attachedActor.GetCapability<PosingCapability>();
        var appearanceCapability = attachedActor.GetCapability<ActorAppearanceCapability>();
        var actionTimeline = attachedActor.GetCapability<ActionTimelineCapability>();

        attachedActor.FriendlyName = actorFile.Name;

        actionTimeline.SetOverallSpeedOverride(0);

        await _framework.RunOnTick(async () =>
        {

            if(actorFile.AnamnesisCharaFile.IsExtendedAppearanceValid)
                BrioUtilities.ImportShadersFromFile(ref appearanceCapability._modelShaderOverride, actorFile.AnamnesisCharaFile);
            await appearanceCapability.SetAppearance(actorFile.AnamnesisCharaFile, AppearanceImportOptions.All);

            await _framework.RunOnTick(async () =>
            {
                bool mountPose = false;
                if(actorFile.Child is not null && actorFile.Child.Companion.Kind == Types.CompanionKind.Mount)
                    mountPose = true;

                if(mountPose == false)
                    posingCapability.ImportPose(actorFile.PoseFile, asScene: true);

                if(attachedActor.HasCapability<CompanionCapability>() == true && actorFile.HasChild && actorFile.Child is not null)
                {
                    var companionCapability = attachedActor.GetCapability<CompanionCapability>();

                    companionCapability.SetCompanion(actorFile.Child.Companion);

                    await _framework.RunOnTick(() =>
                        {
                            if(actorFile.Child.PoseFile is not null)
                            {
                                var companionEntity = companionCapability.GetCompanionAsEntity();

                                if(companionEntity is not null && companionEntity.TryGetCapability<PosingCapability>(out var posingCapability))
                                {
                                    posingCapability.ImportPose(actorFile.Child.PoseFile, asScene: true, freezeOnLoad: true);
                                }
                            }

                            if(mountPose == true)
                                posingCapability.ImportPose(actorFile.PoseFile, asScene: true);
                        });
                }
            }, delayTicks: 10); // I don't like having to set delayTicks to this but I don't think I have another way without more rework
        });
    }
}
