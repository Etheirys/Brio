using Brio.Core;
using Brio.Entities.World;
using Brio.Game.World;
using Brio.Game.World.Interop;
using MessagePack;
using System;
using System.Numerics;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class LightDTO
{
    public static LightDTO? ToDTO(LightEntity entity, LightingService lightingService, Vector3 anchor)
    {
        var dto = lightingService.SaveLightDTO(entity.GameLight, anchor);
        if(dto is null)
            return null;

        dto.FriendlyName = entity.RawName;
        return dto;
    }

    public string FriendlyName { get; set; } = string.Empty;
    public bool IsVisible { get; set; }

    public bool IsGPoseLight { get; set; }
    public uint GposeLightIndex { get; set; }

    public Transform Transform { get; set; }
    public Vector3 RelativePosition { get; set; }
    public string? ParentFolderId { get; set; }

    public LightType LightType { get; set; }
    public Vector3 Color { get; set; }

    public float Intensity { get; set; }
    public float Range { get; set; }
    public float Falloff { get; set; }
    public float LightAngle { get; set; }
    public float FalloffAngle { get; set; }
    public float CharacterShadowRange { get; set; }
    public float ShadowPlaneNear { get; set; }
    public float ShadowPlaneFar { get; set; }
}
