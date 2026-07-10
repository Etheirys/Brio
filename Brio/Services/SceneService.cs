using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Capabilities.World;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Entities.WorldObjects;
using Brio.Game.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Game.Core;
using Brio.Game.Types;
using Brio.Game.World;
using Brio.Game.WorldObjects;
using Brio.IPC;
using Brio.Services.Models;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace Brio.Services;

public enum SceneChunkType : int
{
    Manifest = 1,
    Actors = 2,
    Cameras = 3,
    Environment = 4,
    Lights = 5,
    WorldObjects = 6,
    Folders = 7,
}

public class BrioScene
{
    public SceneManifestDTO Manifest { get; set; } = new();
    public List<ActorDTO> Actors { get; set; } = [];
    public List<CameraDTO> Cameras { get; set; } = [];
    public EnvironmentDTO? Environment { get; set; }
    public List<LightDTO> Lights { get; set; } = [];
    public List<WorldObjectDTO> WorldObjects { get; set; } = [];
    public List<FolderDTO> Folders { get; set; } = [];
}

public class SceneService(EntityManager _entityManager, GlamourerService _glamourerService, PenumbraService _penumbraService, CustomizePlusService _customizePlusService, VirtualCameraManager _virtualCameraManager, IFramework _framework, LightingService _lightingService, WorldObjectService _worldObjectService, IObjectTable _objectTable, ActorRedrawService _redrawService)
{
    private const int FormatVersion = 1;
    private static readonly byte[] Magic = "BRIOSCN"u8.ToArray();

    public byte[] Serialize(BrioScene scene)
    {
        var writer = new SceneWriter();

        writer.AddChunk(SceneChunkType.Manifest, scene.Manifest);
        writer.AddChunk(SceneChunkType.Actors, scene.Actors);
        writer.AddChunk(SceneChunkType.Cameras, scene.Cameras);
        writer.AddChunk(SceneChunkType.Lights, scene.Lights);
        writer.AddChunk(SceneChunkType.WorldObjects, scene.WorldObjects);
        writer.AddChunk(SceneChunkType.Folders, scene.Folders);

        if(scene.Environment is not null)
            writer.AddChunk(SceneChunkType.Environment, scene.Environment);

        return writer.Build();
    }

    public BrioScene Deserialize(byte[] data)
    {
        if(HasMagic(data))
            return ReadData(data);

        return ReadLegacy(data);
    }

    private BrioScene ReadData(byte[] data)
    {
        var reader = new SceneReader(data);
        var scene = new BrioScene();

        if(reader.TryGet<SceneManifestDTO>(SceneChunkType.Manifest, out var manifest) && manifest is not null)
            scene.Manifest = manifest;

        if(reader.TryGet<List<ActorDTO>>(SceneChunkType.Actors, out var actors) && actors is not null)
            scene.Actors = actors;

        if(reader.TryGet<List<CameraDTO>>(SceneChunkType.Cameras, out var cameras) && cameras is not null)
            scene.Cameras = cameras;

        if(reader.TryGet<List<LightDTO>>(SceneChunkType.Lights, out var lights) && lights is not null)
            scene.Lights = lights;

        if(reader.TryGet<List<WorldObjectDTO>>(SceneChunkType.WorldObjects, out var worldObjects) && worldObjects is not null)
            scene.WorldObjects = worldObjects;

        if(reader.TryGet<List<FolderDTO>>(SceneChunkType.Folders, out var folders) && folders is not null)
            scene.Folders = folders;

        if(reader.TryGet<EnvironmentDTO>(SceneChunkType.Environment, out var environment))
            scene.Environment = environment;

        return scene;
    }
    private BrioScene ReadLegacy(byte[] data)
    {
        var legacy = MessagePackSerializer.Deserialize<SceneFileLegacy>(data);

        var scene = new BrioScene
        {
            Actors = legacy.Actors ?? [],
            Cameras = legacy.GameCameras ?? [],
            Manifest = new SceneManifestDTO
            {
                Author = legacy.FileMetaData?.Author,
                Description = legacy.FileMetaData?.Description,
                Base64Image = legacy.FileMetaData?.Base64Image,
            }
        };

        if(legacy.MetaData is not null)
        {
            scene.Manifest.MetaData = new SceneMetaDataDTO
            {
                Map = legacy.MetaData.Map,
                Territory = legacy.MetaData.Territory,
                World = legacy.MetaData.World,
            };
        }

        if(legacy.EnvironmentData is not null)
        {
            scene.Environment = new EnvironmentDTO
            {
                IsTimeFrozen = legacy.EnvironmentData.IsTimeFrozen,
                EorzeaTime = legacy.EnvironmentData.EorzeaTime,
                MinuteOfDay = legacy.EnvironmentData.MinuteOfDay,
                DayOfMonth = legacy.EnvironmentData.DayOfMonth,
                CurrentWeather = legacy.EnvironmentData.CurrentWeather,
                IsWaterFrozen = legacy.EnvironmentData.IsWaterFrozen,
            };
        }

        return scene;
    }

    private bool HasMagic(byte[] data)
    {
        if(data.Length < Magic.Length)
            return false;

        for(int i = 0; i < Magic.Length; i++)
        {
            if(data[i] != Magic[i])
                return false;
        }

        return true;
    }

    //

    private class SceneWriter
    {
        private readonly List<(SceneChunkType Type, int Version, byte[] Payload)> _chunks = [];

        public void AddChunk<T>(SceneChunkType type, T payload, int version = 1)
        {
            _chunks.Add((type, version, MessagePackSerializer.Serialize(payload)));
        }

        public byte[] Build()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(_chunks.Count);

            foreach(var (type, version, payload) in _chunks)
            {
                writer.Write((int)type);
                writer.Write(version);
                writer.Write(payload.Length);
                writer.Write(payload);
            }

            writer.Flush();
            return stream.ToArray();
        }
    }

    private class SceneReader
    {
        private readonly Dictionary<SceneChunkType, byte[]> _chunks = [];
        public int Version { get; private set; }
        public int ChunkCount { get; private set; }

        public SceneReader(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            reader.ReadBytes(Magic.Length);
            Version = reader.ReadInt32();
            ChunkCount = reader.ReadInt32();

            for(int i = 0; i < ChunkCount; i++)
            {
                var type = (SceneChunkType)reader.ReadInt32();
                _ = reader.ReadInt32();
                var length = reader.ReadInt32();
                var payload = reader.ReadBytes(length);

                _chunks[type] = payload;
            }
        }

        public bool TryGet<T>(SceneChunkType type, out T? value)
        {
            if(_chunks.TryGetValue(type, out var payload))
            {
                value = MessagePackSerializer.Deserialize<T>(payload);
                return true;
            }

            value = default;
            return false;
        }
    }


    //
    //


    public bool IsLoading { get; private set; }

    public BrioScene CaptureScene()
    {
        BrioScene scene = new();

        Vector3 anchor = _objectTable.LocalPlayer?.Position ?? Vector3.Zero;

        static string? FolderOf(Entity e)
            => e.Parent is FolderEntity f && f.IsEditable ? f.Id.Unique : null;

        foreach(var entity in EnumerateEntities(_entityManager.EntityManagerContainer))
        {
            switch(entity)
            {
                case ActorEntity actorEntity:

                    ActorDTO actorDto = actorEntity;
                    actorDto.ParentFolderId = FolderOf(actorEntity);
                    scene.Actors.Add(actorDto);

                    break;
                case LightEntity lightEntity:

                    if(lightEntity.GameLight.IsWorldLight)
                        break;

                    var lightDto = LightDTO.ToDTO(lightEntity, _lightingService, anchor);
                    if(lightDto is not null)
                    {
                        lightDto.ParentFolderId = FolderOf(lightEntity);
                        scene.Lights.Add(lightDto);
                    }

                    break;
                case WorldObjectEntity worldObjectEntity:

                    var worldObjectDto = WorldObjectDTO.ToDTO(worldObjectEntity, anchor);
                    worldObjectDto.ParentFolderId = FolderOf(worldObjectEntity);
                    scene.WorldObjects.Add(worldObjectDto);

                    break;
                case FolderEntity folderEntity:

                    if(folderEntity.IsEditable)
                        scene.Folders.Add(new FolderDTO
                        {
                            Id = folderEntity.Id.Unique,
                            ParentId = FolderOf(folderEntity),
                            FriendlyName = folderEntity.FriendlyName,
                            AreChildrenHidden = folderEntity.AreChildrenHidden
                        });

                    break;
                case CameraEntity cameraEntity:
                    if(cameraEntity.IsDefaultCamera)
                        continue;

                    scene.Cameras.Add(new CameraDTO
                    {
                        Camera = cameraEntity.VirtualCamera,
                        CameraType = cameraEntity.CameraType,
                        ParentFolderId = FolderOf(cameraEntity)
                    });

                    break;
            }
        }

        var environmentEntity = _entityManager.GetEntity<EnvironmentContainerEntity>("environment");

        if(environmentEntity is not null)
        {
            var wc = environmentEntity.GetCapability<TimeWeatherCapability>();
            var wrc = environmentEntity.GetCapability<WorldRenderingCapability>();

            scene.Environment = new EnvironmentDTO
            {
                CurrentWeather = wc.EnvironmentService.CurrentWeather,
                IsTimeFrozen = wc.TimeService.IsTimeFrozen,
                EorzeaTime = wc.TimeService.EorzeaTime,
                DayOfMonth = wc.TimeService.DayOfMonth,
                MinuteOfDay = wc.TimeService.MinuteOfDay,
                IsWaterFrozen = wrc.WorldRenderingService.IsWaterFrozen
            };
        }

        return scene;

        static IEnumerable<Entity> EnumerateEntities(Entity root)
        {
            foreach(var child in root.Children)
            {
                yield return child;

                foreach(var descendant in EnumerateEntities(child))
                    yield return descendant;
            }
        }
    }

    public unsafe void ImportScene(BrioScene scene, bool destroyAll = true, bool useRelativeLightPositions = true, bool useRelativeWorldObjectPositions = true, SceneImportOptions importOptions = SceneImportOptions.All)
    {
        IsLoading = true;

        Vector3? lightAnchor = useRelativeLightPositions ? _objectTable.LocalPlayer?.Position ?? Vector3.Zero : null;
        Vector3? worldObjectAnchor = useRelativeWorldObjectPositions ? _objectTable.LocalPlayer?.Position ?? Vector3.Zero : null;

        var actorCapability = _entityManager.EntityManagerContainer.GetCapability<ActorContainerCapability>();

        if(ConfigurationService.Instance.Configuration.SceneDestoryActorsBeforeImport || destroyAll)
        {
            actorCapability.DestroyAll();

            _virtualCameraManager.DestroyAll();
            _lightingService.DestroyAll();
            _worldObjectService.DestroyAll();

            _entityManager.DestroyAllFolders(); //Do this last to everything is already gone by the time we get here
        }

        var folderMap = new Dictionary<string, FolderEntity>();
        if(importOptions.HasFlag(SceneImportOptions.Folders))
        {
            foreach(var folderDto in scene.Folders)
            {
                var folder = _entityManager.CreateEntityOnEntityContainer<FolderEntity>(folderDto.FriendlyName);

                folder.IsLoadedFromProject = true;
                folder.AreChildrenHidden = folderDto.AreChildrenHidden;

                folderMap[folderDto.Id] = folder;
            }

            foreach(var folderDto in scene.Folders)
            {
                if(folderDto.ParentId is not null
                    && folderMap.TryGetValue(folderDto.ParentId, out var parent)
                    && folderMap.TryGetValue(folderDto.Id, out var child))

                    _entityManager.MoveEntity(child, parent);
            }
        }

        if(importOptions.HasFlag(SceneImportOptions.Actors))
        {
            foreach(ActorDTO actorFile in scene.Actors)
            {
                if(destroyAll)
                {
                    _framework.RunOnTick(() =>
                    {
                        SpawnActor();
                    }, delayTicks: 5);
                }
                else
                {
                    SpawnActor();
                }

                void SpawnActor()
                {
                    var (actorId, actor) = actorCapability.CreateCharacter(actorFile.HasChild, false, forceSpawnActorWithoutCompanion: !actorFile.HasChild);

                    _framework.RunUntilSatisfied(
                        () => actor.Native()->IsReadyToDraw(),
                        (__) =>
                        {
                            _framework.RunOnTick(() =>
                            {
                                var entity = _entityManager.GetEntity<ActorEntity>(actorId);
                                if(entity is not null)
                                {
                                    entity.IsLoading = true;
                                    entity.LoadingDescription = $"Loading {actorFile.Name}...";
                                    entity.IsLoadedFromProject = true;

                                    Reparent(entity, actorFile.ParentFolderId);

                                    _ = ApplyDataToActor(entity, actorFile);
                                }
                            });
                        },
                        100,
                        dontStartFor: 3
                    );
                }
            }
        }

        if(importOptions.HasFlag(SceneImportOptions.Cameras))
        {
            foreach(CameraDTO item in scene.Cameras)
            {
                var (_, cameraId) = _virtualCameraManager.CreateCamera(item.CameraType, false, false, item.Camera);
                Reparent(_entityManager.GetEntity<CameraEntity>(new CameraId(cameraId)), item.ParentFolderId);
            }
        }

        // I really hate this Folder shit. But I don't know anyother way to do it atm

        if(importOptions.HasFlag(SceneImportOptions.Lights))
        {
            foreach(LightDTO light in scene.Lights)
            {
                _lightingService.SpawnFromDTO(light, lightAnchor, FolderFor(light.ParentFolderId));
            }
        }

        if(importOptions.HasFlag(SceneImportOptions.WorldObjects))
        {
            foreach(WorldObjectDTO worldObject in scene.WorldObjects)
            {
                _worldObjectService.SpawnFromDTO(worldObject, worldObjectAnchor, FolderFor(worldObject.ParentFolderId));
            }
        }

        if(importOptions.HasFlag(SceneImportOptions.Environment) && scene.Environment is not null)
        {
            var environmentEntity = _entityManager.GetEntity<EnvironmentContainerEntity>("environment")!;
            var wc = environmentEntity.GetCapability<TimeWeatherCapability>();
            var wrc = environmentEntity.GetCapability<WorldRenderingCapability>();

            wc.EnvironmentService.CurrentWeather = scene.Environment.CurrentWeather;
            wc.TimeService.IsTimeFrozen = scene.Environment.IsTimeFrozen;
            wc.TimeService.EorzeaTime = scene.Environment.EorzeaTime;
            wc.TimeService.DayOfMonth = scene.Environment.DayOfMonth;
            wc.TimeService.MinuteOfDay = scene.Environment.MinuteOfDay;
            wrc.WorldRenderingService.IsWaterFrozen = scene.Environment.IsWaterFrozen;
        }

        _framework.RunOnTick(() =>
        {
            IsLoading = false;
        }, delayTicks: 250);

        //

        void Reparent(Entity? entity, string? parentFolderId)
        {
            if(entity is null || parentFolderId is null)
                return;

            if(folderMap.TryGetValue(parentFolderId, out var folder))
                _entityManager.MoveEntity(entity, folder);
        }

        FolderEntity? FolderFor(string? parentFolderId)
            => parentFolderId is not null && folderMap.TryGetValue(parentFolderId, out var folder) ? folder : null;
    }

    private async Task ApplyDataToActor(ActorEntity actorEntity, ActorDTO actorFile)
    {
        var posingCapability = actorEntity.GetCapability<PosingCapability>();
        var appearanceCapability = actorEntity.GetCapability<ActorAppearanceCapability>();
        var actionTimeline = actorEntity.GetCapability<ActionTimelineCapability>();

        actorEntity.FriendlyName = actorFile.Name;

        actionTimeline.SetOverallSpeedOverride(0);

        await _framework.RunOnTick(async () =>
        {
            try
            {
                Brio.Log.Verbose("Applying data to actor: " + actorEntity.Id);

                var pem = actorFile.HasPenumbra;
                var wasSync = actorFile.WasMCDF && actorFile.WasOtherPlayer;

                if(actorFile.WasMCDF)
                {
                    Brio.Log.Info($"Actor {actorFile.Name} - m:{actorFile.WasMCDF} - s:{wasSync} was locked at the time of saving. Appearance will not be imported.");
                    Brio.PopToast($"Actor {actorFile.Name} was locked at the time of saving. Appearance will not be imported.", "Brio Scene Import", NotificationType.Warning);
                }

                //else
                {
                    var applyGlamourer = _glamourerService.IsAvailable && actorFile.HasGlamourer
                        && (actorFile.GlamourerDesignBase64 is not null || actorFile.GlamourerDesign is not null);

                    if(applyGlamourer)
                    {
                        Brio.Log.Verbose("Applying glamourer design for actor: " + actorEntity.FriendlyName);
                        if(actorFile.GlamourerDesignBase64 is not null)
                        {
                            _glamourerService.SetState(actorEntity.GameObject, actorFile.GlamourerDesignBase64);
                        }
                        else
                        {
                            _glamourerService.ApplyDesign(actorFile.GlamourerDesign!.Value, actorEntity.GameObject);
                        }
                    }
                    else
                    {
                        if(actorFile.AnamnesisCharaFile.IsExtendedAppearanceValid)
                        {
                            BrioUtilities.ImportShadersFromFile(ref appearanceCapability._modelShaderOverride, actorFile.AnamnesisCharaFile);
                            await appearanceCapability.SetAppearance(actorFile.AnamnesisCharaFile, AppearanceImportOptions.All);
                        }
                        else
                        {
                            await appearanceCapability.SetAppearance(actorFile.AnamnesisCharaFile, AppearanceImportOptions.All);
                        }
                    }

                    if(pem && actorFile.PenumbraCollection.HasValue && _penumbraService.IsAvailable)
                    {
                        Brio.Log.Verbose($"Applying penumbra collection for actor: {actorEntity.FriendlyName} - P:{actorFile.PenumbraCollection.Value}");
                        _penumbraService.SetCollectionForObject(actorEntity.GameObject, actorFile.PenumbraCollection.Value);
                    }

                    if(actorFile.HasCustomizePlus && _customizePlusService.IsAvailable)
                    {
                        Brio.Log.Verbose($"Applying customize plus profile for actor: {actorEntity.FriendlyName} - C:{actorFile.CustomizePlusProfile}");
                    }

                    await _redrawService.WaitForDrawing(actorEntity.GameObject);
                }

                Brio.Log.Verbose("Importing pose for actor: " + actorFile.Name);

                var mountPose = false;
                if(actorFile.Child is not null && actorFile.Child.Companion.Kind == CompanionKind.Mount)
                    mountPose = true;

                if(mountPose == false)
                    posingCapability.ImportPose(actorFile.PoseFile, asScene: true);

                if(actorEntity.HasCapability<CompanionCapability>() == true && actorFile.HasChild && actorFile.Child is not null)
                {
                    var companionCapability = actorEntity.GetCapability<CompanionCapability>();

                    companionCapability.SetCompanion(actorFile.Child.Companion);

                    await _framework.RunUntilSatisfied(
                        () => companionCapability.GetCompanionAsEntity() is not null,
                        (_) =>
                        {
                            Brio.Log.Verbose("Importing pose for companion: " + actorFile.Child);

                            if(actorFile.Child.PoseFile is not null)
                            {
                                var companionEntity = companionCapability.GetCompanionAsEntity();

                                if(companionEntity is not null && companionEntity.TryGetCapability<PosingCapability>(out var companionPosing))
                                {
                                    companionPosing.ImportPose(actorFile.Child.PoseFile, asScene: true, freezeOnLoad: true);
                                }
                            }

                            if(mountPose == true)
                                posingCapability.ImportPose(actorFile.PoseFile, asScene: true);
                        },
                        100,
                        dontStartFor: 3
                    );
                }

                Brio.Log.Debug("Finished applying data to actor: " + actorFile.Name);
            }
            catch(Exception ex)
            {
                Brio.Log.Error(ex, "Failed applying data to actor: " + actorFile.Name);
            }
            finally
            {
                actorEntity.IsLoading = false;
            }

        }, delayTicks: 10);
    }
}
