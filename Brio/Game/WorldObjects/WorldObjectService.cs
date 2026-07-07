using Brio.Core;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Entities.WorldObjects;
using Brio.Game.Camera;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.WorldObjects.Objects;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Brio.Services.Models;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Brio.Game.WorldObjects;

public unsafe class WorldObjectService : MediatorSubscriberBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IObjectTable _gameObjects;
    private readonly IFramework _framework;
    private readonly VirtualCameraManager _cameraManager;
    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly VFXService _vfxService;
    private readonly SGLService _sglService;

    private readonly ComponentSet<IWorldObject> _spawnedObjects = [];
    private readonly ComponentSet<WorldObjectEntity> _worldObjectEntities = [];

    public int SpawnedCount => _worldObjectEntities.ActiveCount;
    public List<WorldObjectEntity> SpawnedEntities => [.. _worldObjectEntities];

    public WorldObjectService(IServiceProvider serviceProvider, IObjectTable gameObjects, IFramework framework, EntityManager entityManager, GPoseService gPoseService, VFXService vFXService, SGLService sGLService, VirtualCameraManager cameraManager, Mediator mediator) : base(mediator)
    {
        _serviceProvider = serviceProvider;
        _entityManager = entityManager;
        _framework = framework;
        _cameraManager = cameraManager;
        _gPoseService = gPoseService;
        _gameObjects = gameObjects;
        _vfxService = vFXService;
        _sglService = sGLService;

        mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => OnFrameworkUpdate());
        mediator.Subscribe<GposeStateChangedMessage>(this, (state) => OnGPoseStateChanged(state.NewState));
    }

    private void OnGPoseStateChanged(bool state)
    {
        if(state == false)
        {
            DestroyAll();
        }
    }

    private void OnFrameworkUpdate()
    {
        if(_framework.IsFrameworkUnloading) return;

        if(_gPoseService.IsGPosing is false)
            return;

        for(var i = _spawnedObjects.ActiveCount - 1; i >= 0; i--)
        {
            var obj = _spawnedObjects.Components[i];

            switch(obj)
            {
                case StaticVfxObject vfx:
                    if(vfx.IsValid is false)
                        break;

                    if(vfx.IsDirty)
                    {
                        vfx.IsDirty = false;

                        vfx.VFX->SomeFlags = 0xF7;
                        vfx.VFX->Update(0.0f);
                    }

                    if((vfx.Moved && vfx.ShouldStartWithoutSpeed) || (vfx.IsDirty && vfx.ShouldStartWithoutSpeed))
                    {
                        vfx.Moved = false;
                        vfx.SetSpeed(0f);
                    }

                    break;

                case BrioPropObject prop:
                    if(prop.IsValid && prop.IsDirty)
                    {
                        prop.Reload();
                    }
                    break;

                case BGOObject bgObj:
                    if(bgObj.IsValid && bgObj.IsDirty)
                    {
                        bgObj.BgObject->UpdateCulling();
                    }
                    break;

                case FurnitureObject furniture:
                    if(furniture.IsValid && furniture.IsDirty)
                    {
                        furniture.SGL->Instances.SetCollidersActive(false);
                        if(furniture.VsualStateDirty)
                            furniture.ClearColor();
                    }
                    break;
            }
        }
    }

    //

    public void SpawnBgObject(string path) =>
        _framework.RunOnFrameworkThread(() => SpawnBgObjectInternal(path));
    public void SpawnProp(WeaponCreateInfo path) =>
        _framework.RunOnFrameworkThread(() => SpawnPropInternal(path));
    public void SpawnStaticVfx(string path) =>
        _framework.RunOnFrameworkThread(() => SpawnStaticVfxInternal(path));
    public void SpawnFurniture(string path) =>
        _framework.RunOnFrameworkThread(() => SpawnFurnitureInternal(path));

    public Task<BGOObject?> SpawnBgObjectAsync(string path) =>
        _framework.RunOnFrameworkThread(() => SpawnBgObjectInternal(path));
    public Task<StaticVfxObject?> SpawnStaticVfxAsync(string path) =>
        _framework.RunOnFrameworkThread(() => SpawnStaticVfxInternal(path));

    private BGOObject? SpawnBgObjectInternal(string path)
    {
        if(string.IsNullOrWhiteSpace(path)) return null;

        var handle = new BGOObject(path, new Transform
        {
            Position = _gameObjects.LocalPlayer!.Position,
            Rotation = Quaternion.Identity,
            Scale = new Vector3(1, 1, 1)
        });
        handle.SetIndex(_spawnedObjects.Add(handle));
        CreateAndAttachEntity(handle);

        return handle;
    }
    private StaticVfxObject? SpawnStaticVfxInternal(string path)
    {
        if(string.IsNullOrWhiteSpace(path)) return null;

        var handle = new StaticVfxObject(path, _vfxService, new Transform
        {
            Position = _gameObjects.LocalPlayer!.Position,
            Rotation = Quaternion.Identity,
            Scale = new Vector3(1, 1, 1)
        });
        handle.SetIndex(_spawnedObjects.Add(handle));
        CreateAndAttachEntity(handle);

        return handle;
    }
    private BrioPropObject? SpawnPropInternal(WeaponCreateInfo? wci)
    {
        if(wci is null) return null;

        var handle = new BrioPropObject(wci.Value, new Transform
        {
            Position = _gameObjects.LocalPlayer!.Position,
            Rotation = Quaternion.Identity,
            Scale = new Vector3(1, 1, 1)
        });
        handle.SetIndex(_spawnedObjects.Add(handle));
        CreateAndAttachEntity(handle);

        return handle;
    }
    private FurnitureObject SpawnFurnitureInternal(string path)
    {
        var handle = new FurnitureObject(path, _sglService, new Transform
        {
            Position = _gameObjects.LocalPlayer!.Position,
            Rotation = Quaternion.Identity,
            Scale = new Vector3(1, 1, 1)
        });
        handle.SetIndex(_spawnedObjects.Add(handle));
        CreateAndAttachEntity(handle);

        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateAndAttachEntity(IWorldObject obj)
    {
        var entity = _entityManager.CreateEntityOnEntityContainer<WorldObjectEntity>(obj);
        obj.SetEntityIndex(_worldObjectEntities.Add(entity));
    }

    //

    public void SpawnFromDTO(WorldObjectDTO dto, Vector3? anchor = null, FolderEntity? folder = null)
    {
        var transform = dto.Transform;
        transform.Position = anchor.HasValue ? anchor.Value + dto.RelativePosition : dto.Transform.Position;

        switch(dto.ObjectType)
        {
            case WorldObjectType.BgObject:
                _framework.RunOnFrameworkThread(() =>
                {
                    var worldObj = SpawnBgObjectInternal(dto.Path);
                    worldObj?.SetTransform(transform);
                    MoveToFolder(worldObj, folder);
                });
                break;
            case WorldObjectType.StaticVfx:
                _framework.RunOnFrameworkThread(() =>
                {
                    var worldObj = SpawnStaticVfxInternal(dto.Path);
                    worldObj?.SetTransform(transform);

                    if(dto.Color.HasValue)
                        worldObj?.VFX->Color = dto.Color.Value;

                    MoveToFolder(worldObj, folder);
                });
                break;
            case WorldObjectType.Furniture:
                _framework.RunOnFrameworkThread(() =>
                {
                    var worldObj = SpawnFurnitureInternal(dto.Path);
                    worldObj?.SetTransform(transform);

                    Brio.Log.Warning($"Furniture spawn: {dto.Path}, Color: {dto.Color} - Stain ID: {dto.StainID}");

                    _framework.RunUntilSatisfied(
                        () => worldObj?.VsualStateDirty is false,
                        (_) => {
                            if(dto.Color.HasValue)
                                worldObj?.SetCustomColor(dto.Color.Value);
                           else if(dto.StainID != 0)
                                worldObj?.SetStain((byte)dto.StainID);
                        },
                        1000,
                        dontStartFor: 1);
          
                    MoveToFolder(worldObj, folder);
                });
                break;
            case WorldObjectType.Prop:
                if(dto.PropModel is null)
                    break;
                _framework.RunOnFrameworkThread(() =>
                {
                    var worldObj = SpawnPropInternal(dto.PropModel.ToWeaponCreateInfo());
                    worldObj?.SetTransform(transform);
                    MoveToFolder(worldObj, folder);
                });
                break;
        }
    }

    private void MoveToFolder(IWorldObject? obj, FolderEntity? folder)
    {
        if(obj is null || folder is null)
            return;

        var entity = _worldObjectEntities.Components[obj.EntityIndex];

        if(entity is not null)
            _entityManager.MoveEntity(entity, folder);
    }

    public void Clone(IWorldObject obj)
    {
        var currentTransform = obj.Transform;
        switch(obj.ObjectType)
        {
            case WorldObjectType.BgObject:
                _framework.RunOnFrameworkThread(() =>
                {
                    var worldObj = SpawnBgObjectInternal(obj.Path);
                    worldObj?.SetTransform(currentTransform);
                });
                break;
            case WorldObjectType.StaticVfx:
                _framework.RunOnFrameworkThread(() =>
                {
                    var worldObj = SpawnStaticVfxInternal(obj.Path);
                    worldObj?.SetTransform(currentTransform);
                });
                break;
            case WorldObjectType.Furniture:
                _framework.RunOnFrameworkThread(() =>
                {
                    var worldObj = SpawnFurnitureInternal(obj.Path);
                    worldObj?.SetTransform(currentTransform);

                    if(obj is FurnitureObject furniture)
                    {
                        _framework.RunUntilSatisfied(
                            () => worldObj?.VsualStateDirty is false,
                            (_) => {
                                if(furniture.IsCustomColor)
                                    worldObj?.SetCustomColor(furniture.CustomColor);
                                else if(furniture.StainID != 0)
                                    worldObj?.SetStain((byte)furniture.StainID);
                            },
                            1000,
                            dontStartFor: 1);
                    }
                });
                break;
            case WorldObjectType.Prop:
                _framework.RunOnFrameworkThread(() =>
                {
                    var wci = (obj as BrioPropObject)!.WeaponInfo;
                    var worldObj = SpawnPropInternal(wci);
                    worldObj?.SetTransform(currentTransform);
                });
                break;
        }
    }

    public void MoveToCamera(IWorldObject obj)
    {
        if(_cameraManager.CurrentCamera is null) return;

        var cam = _cameraManager.CurrentCamera;
        Vector3 position;

        if(cam.IsFreeCamera)
        {
            position = cam.Position;
        }
        else
        {
            position = cam.BrioCamera->Position;
        }

        var current = obj.Transform;
        current.Position = position;
        obj.SetTransform(current);
    }

    public void DestroyAll()
    {
        if(_framework.IsFrameworkUnloading) return;

        _framework.RunOnFrameworkThread(() =>
        {
            foreach(var obj in _spawnedObjects)
                Destroy(obj);

            _spawnedObjects.Clear();
            _worldObjectEntities.Clear();
        });
    }
    public void Destroy(IWorldObject obj)
    {
        _spawnedObjects.Remove(obj.Index);

        _framework.RunOnFrameworkThread(() =>
        {
            var entity = _worldObjectEntities.Components[obj.EntityIndex];
            if(entity is not null)
            {
                _entityManager.DetachEntity(entity, true);
                _worldObjectEntities.Remove(obj.EntityIndex);
            }
            obj.Dispose();
        });
    }

    public override void Dispose()
    {
        DestroyAll();

        base.Dispose();

        GC.SuppressFinalize(this);
    }
}
