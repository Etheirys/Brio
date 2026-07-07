using Brio.Core;
using Brio.Game.Actor.Appearance;
using Brio.Resources;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;

using CSObject = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;
using CSWeapon = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Weapon;
using CSWorld = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.World;

namespace Brio.Game.WorldObjects.Objects;

public unsafe class BrioPropObject : WorldObject
{
    private CSWeapon* Weapon;
    public WeaponCreateInfo WeaponInfo { get; set { field = value; IsDirty = true; } }

    public ushort ModelSetId { get => Weapon->ModelSetId; set => Weapon->ModelSetId = value; }
    public ushort SecondaryId { get => Weapon->SecondaryId; set => Weapon->SecondaryId = value; }

    public ushort Variant { get => Weapon->Variant; set => Weapon->Variant = value; }

    public byte PrimaryDye { get => Weapon->Stain0; set => Weapon->Stain0 = value; }
    public byte SecondaryDye { get => Weapon->Stain1; set => Weapon->Stain1 = value; }

    //

    public override WorldObjectType ObjectType => WorldObjectType.Prop;
    public override string FriendlyName { get; protected set; } = "Weapon | Prop";

    public override string FriendlyPath
    {
        get
        {
            if(string.IsNullOrEmpty(field))
            {
                var equip = new WeaponModelId { Id = ModelSetId, Type = SecondaryId, Variant =  Variant, Stain0 = PrimaryDye, Stain1 = SecondaryDye };

                var info = GameDataProvider.Instance.ModelDatabase.GetModelById(equip, ActorEquipSlot.Prop)?.Name ?? string.Empty;
                if(!string.IsNullOrEmpty(info))
                    FriendlyName = info;
                return field = System.IO.Path.GetFileNameWithoutExtension(Path);
            }
            else
                return field;
        }
        set;
    }

    public override nint Address => (nint)Weapon;

    public override bool IsVisible
    {
        get => IsValid && !(Weapon->GetTransparency() > 0f);
        set
        {
            if(IsValid)
                Weapon->SetTransparency(value ? 0f : 1f);
        }
    }

    public BrioPropObject(WeaponCreateInfo model, Transform transform)
    {
        WeaponInfo = model;

        Transform = transform;
        OriginalTransform = Transform;

        Create(model);
    }

    private void Create(WeaponCreateInfo model)
    {
        Weapon = CSWeapon.Create(&model);
        if(Weapon is null)
            return;

        SetTransform(Transform);

        CSWorld.Instance()->AddChild((CSObject*)Weapon);
        Weapon->OnAddedToWorld();
    }

    public void Reload()
    {
        if(!IsValid) return;

        Brio.Log.Verbose($"Reloading Prop Object {FriendlyName} (Address: {Address:X})");

        IsDirty = false;

        Weapon->CleanupRender();

        var newWeapon = WeaponInfo;
        Weapon->Initialize(&newWeapon);

        SetTransform(Transform);

        CSWorld.Instance()->AddChild((CSObject*)Weapon);
        Weapon->OnAddedToWorld();
    }

    public override Transform GetTransform()
    {
        if(!IsValid) return Transform.Identity;
        return new Transform { Position = Weapon->Position, Rotation = Weapon->Rotation, Scale = Weapon->Scale };
    }
    public override void SetTransform(Transform transform)
    {
        Transform = transform;

        if(!IsValid) return;
        Weapon->Position = transform.Position;
        Weapon->Rotation = transform.Rotation;
        Weapon->Scale = transform.Scale;

        Weapon->IsTransformChanged = true;

        Weapon->NotifyTransformChanged();
        Weapon->UpdateTransforms(false);
    }

    public override void Destroy()
    {
        Weapon->CleanupRender();
        Weapon->Dtor(1);
    }

    public override void Dispose()
    {
        if(IsValid)
            Destroy();

        Weapon = null;

        GC.SuppressFinalize(this);
    }
}
