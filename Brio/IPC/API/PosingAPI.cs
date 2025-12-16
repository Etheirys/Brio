using Brio.API.Interface;
using Brio.Capabilities.Posing;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Files;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;

namespace Brio.IPC.API;

public unsafe class PosingAPI(GPoseService gPoseService, EntityManager entityManager) : IPosing
{
    private readonly GPoseService _gPoseService = gPoseService;
    private readonly EntityManager _entityManager = entityManager;

    public (Vector3?, Quaternion?, Vector3?) GetModelTransform(IGameObject gameObject)
    {
        if(_gPoseService.IsGPosing == false) return (null, null, null);

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ModelPosingCapability>(out var transformCapability))
            {
                var transform = transformCapability.Transform;

                return (transform.Position, transform.Rotation, transform.Scale);
            }
        }
        return (null, null, null);
    }

    public string? GetPoseAsJson(IGameObject gameObject)
    {
        if(_gPoseService.IsGPosing == false) return null;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<PosingCapability>(out var posingCapability))
            {
                var pose = posingCapability.ExportPose();

                return JsonSerializer.Serialize(pose);
            }
        }
        return null;
    }

    public bool LoadPoseFromFile(IGameObject gameObject, string filename)
    {
        if(_gPoseService.IsGPosing == false || string.IsNullOrEmpty(filename)) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<PosingCapability>(out var posingCapability))
            {
                posingCapability.ImportPose(filename);

                return true;
            }
        }
        return false;
    }

    public bool LoadPoseFromJson(IGameObject gameObject, bool isLegacyCMToolPose, string json)
    {
        if(_gPoseService.IsGPosing == false || string.IsNullOrEmpty(json)) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out Entity? entity) && entity is not null)
        {
            if(entity.TryGetCapability<PosingCapability>(out var posingCapability))
            {
                try
                {
                    if(isLegacyCMToolPose)
                    {
                        posingCapability.ImportPose(JsonSerializer.Deserialize<CMToolPoseFile>(json), null, asIPCpose: true);
                    }
                    else
                    {
                        posingCapability.ImportPose(JsonSerializer.Deserialize<PoseFile>(json), null, asIPCpose: true);
                    }

                    return true;
                }
                catch
                {
                    Brio.NotifyError("Invalid pose file loaded from IPC.");
                }
            }
        }

        return false;
    }

    public bool ResetModelTransform(IGameObject gameObject)
    {
        if(_gPoseService.IsGPosing == false) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ModelPosingCapability>(out var transformCapability))
            {
                transformCapability.ResetTransform();
                return true;
            }
        }
        return false;
    }

    public bool ResetPose(IGameObject gameObject, bool clearHistory)
    {
        if(_gPoseService.IsGPosing == false) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<PosingCapability>(out var posingCapability))
            {
                posingCapability.Reset(false, false, clearHistory);

                return true;
            }
        }

        return false;
    }

    public bool SetModelTransform(IGameObject gameObject, Vector3? position, Quaternion? rotation, Vector3? scale, bool relativeMode)
    {
        if(_gPoseService.IsGPosing == false) return false;

        if(_entityManager.TryGetEntity(gameObject.Native(), out var entity))
        {
            if(entity.TryGetCapability<ModelPosingCapability>(out var transformCapability))
            {
                var transform = transformCapability.Transform;

                if(relativeMode)
                {
                    if(position.HasValue)
                        transform.Position += position.Value;
                    if(rotation.HasValue)
                        transform.Rotation += rotation.Value;
                    if(scale.HasValue)
                        transform.Scale += scale.Value;
                }
                else
                {
                    if(position.HasValue)
                        transform.Position = position.Value;
                    if(rotation.HasValue)
                        transform.Rotation = rotation.Value;
                    if(scale.HasValue)
                        transform.Scale = scale.Value;
                }

                transformCapability.Transform = transform;

                return true;
            }
        }
        return false;
    }
}
