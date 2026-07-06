using Brio.Core;
using Brio.Entities.WorldObjects;
using Brio.Game.WorldObjects;
using Brio.Game.WorldObjects.Objects;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using MessagePack;
using System;
using System.Numerics;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class WorldObjectDTO
{
    public static WorldObjectDTO ToDTO(WorldObjectEntity entity, Vector3 anchor)
    {
        var gameObject = entity.GameBgObject;

        var dto = new WorldObjectDTO
        {
            FriendlyName = entity.RawName,
            ObjectType = gameObject.ObjectType,
            Path = gameObject.Path,
            Transform = gameObject.Transform,
            RelativePosition = gameObject.Transform.Position - anchor,
        };

        if(gameObject is BrioPropObject prop)
            dto.PropModel = PropModelDTO.ToDTO(prop.WeaponInfo);

        return dto;
    }

    public string FriendlyName { get; set; } = string.Empty;
    public WorldObjectType ObjectType { get; set; }

    public string Path { get; set; } = string.Empty;

    public PropModelDTO? PropModel { get; set; }
    public Transform Transform { get; set; }
    public Vector3 RelativePosition { get; set; }

    public string? ParentFolderId { get; set; }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class PropModelDTO
{
    public ushort ModelId { get; set; }
    public ushort ModelType { get; set; }
    public ushort ModelVariant { get; set; }

    public byte Stain0 { get; set; }
    public byte Stain1 { get; set; }

    public byte AnimationVariant { get; set; }

    public WeaponCreateInfo ToWeaponCreateInfo() => new()
    {
        WeaponModelId =
        {
            Id      = ModelId,
            Type    = ModelType,
            Variant = ModelVariant,
            Stain0  = Stain0,
            Stain1  = Stain1,
        },
        AnimationVariant = AnimationVariant,
    };

    public static PropModelDTO ToDTO(WeaponCreateInfo wci) => new()
    {
        ModelId = wci.WeaponModelId.Id,
        ModelType = wci.WeaponModelId.Type,
        ModelVariant = wci.WeaponModelId.Variant,
        Stain0 = wci.WeaponModelId.Stain0,
        Stain1 = wci.WeaponModelId.Stain1,
        AnimationVariant = wci.AnimationVariant,
    };
}
