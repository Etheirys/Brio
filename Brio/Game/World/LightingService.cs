
//
// Brio's lights would not be possible without the help of the following projects:
// Massive Help with Dynamis: https://github.com/Exter-N/Dynamis by Exter-N (Ny)
// LightsCameraAction: https://github.com/NeNeppie/LightsCameraAction by NeNeppie
//
// Look at how beautiful my lights are. My lights are moist and delicious. Lights, lights, lights
//

using Brio.Core;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Game.World.Interop;
using Brio.Game.World.Lights;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Brio.Services.Models;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using InteropGenerator.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Game.World;

public unsafe class LightingService : MediatorSubscriberBase
{
    private readonly IFramework _framework;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameInteropProvider _hooks;
    private readonly GPoseService _gPoseService;
    private readonly EntityManager _entityManager;
    private readonly VirtualCameraManager _virtualCameraManager;

    //

    private readonly delegate* unmanaged<BrioEventGPoseController*, uint, char> _toggleGPoseLight;
    private readonly delegate* unmanaged<LightType, CStringPointer, BrioLight*, BrioLight*> _createGameLight;

    private delegate BrioLight* LightDelegate(BrioLight* light);
    private readonly Hook<LightDelegate> _lightCtorHook = null!;

    public delegate* unmanaged<BrioLight*, bool, void> Destructor;

    private delegate void LightDtorDelegate(BrioLight* thisPtr, bool free);
    private Hook<LightDtorDelegate> _lightDtorHook = null!;

    //

    private readonly Dictionary<IGameLight, LightDTO> _handledLightStates = [];

    private readonly ComponentSet<IGameLight> _handledLights = [];
    private readonly ComponentSet<IGameLight> _spawnedLights = [];
    private readonly ComponentSet<LightEntity> _lightEntities = [];

    public BrioEventGPoseController* CurrentGPoseState => (BrioEventGPoseController*)&EventFramework.Instance()->EventSceneModule.EventGPoseController;

    public int SpawnedLightEntitiesCount => _lightEntities.ActiveCount;
    public List<LightEntity> SpawnedLightEntities => [.. _lightEntities];

    public LightEntity? SelectedLightEntity = null;

    private readonly HashSet<nint> _worldGameLights = [];
    public IReadOnlyCollection<nint> WorldLights => _worldGameLights;

    public LightingService(IServiceProvider serviceProvider, EntityManager entityManager, GPoseService gPoseService, VirtualCameraManager virtualCameraManager, IFramework framework, ISigScanner sigScanner, IGameInteropProvider hooks, Mediator mediator) : base(mediator)
    {
        _serviceProvider = serviceProvider;
        _gPoseService = gPoseService;
        _entityManager = entityManager;
        _virtualCameraManager = virtualCameraManager;
        _framework = framework;
        _hooks = hooks;

        var createGameLightAddress = sigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 8B F9");                               // Light.Create
        _createGameLight = (delegate* unmanaged<LightType, CStringPointer, BrioLight*, BrioLight*>)createGameLightAddress;

        var toggleLightHookAddress = sigScanner.ScanText("48 83 EC 28 4C 8B C1 83 FA 03 ?? ?? 8B C2");
        _toggleGPoseLight = (delegate* unmanaged<BrioEventGPoseController*, uint, char>)toggleLightHookAddress;

        var _lightCtorAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 89 84 ?? ?? ?? ?? ?? 48 85 C0 0F ?? ?? ?? ?? ?? 48 8B C8");      // Light.ctor
        _lightCtorHook = hooks.HookFromAddress<LightDelegate>(_lightCtorAddress, LightCtor);
        _lightCtorHook.Enable();

        mediator.Subscribe<GposeStateChangedMessage>(this, (state) => OnGPoseStateChange(state.NewState));
        mediator.Subscribe<FrameworkUpdateMessage>(this, (state) => OnFrameworkUpdate(state.Framework));
    }

    private LightEntity[] gposeLights = new LightEntity[3];

    public char ToggleGPoseLight(BrioEventGPoseController* ptr, uint index)
        => _toggleGPoseLight(ptr, index);

    private BrioLight* LightCtor(BrioLight* light)
    {
        var value = _lightCtorHook.Original(light);
        _worldGameLights.Add((nint)light);

        if(Destructor == null)
        {
            Destructor = light->VirtualTable->Destructor;
            _framework.RunOnTick(() =>
                {
                    _lightDtorHook = _hooks.HookFromAddress<LightDtorDelegate>((nint)Destructor, LightDtor);
                    _lightDtorHook.Enable();
                });
        }

        if(_gPoseService.IsGPosing is false)
            return value;

        _framework.RunOnTick(() =>
        {
            var gposeController = (BrioEventGPoseController*)&EventFramework.Instance()->EventSceneModule.EventGPoseController;
            for(uint i = 0; i < 3; i++)
            {
                var gposeLight = gposeController->GetLight(i);
                if(gposeLight != null && (nint)gposeLight == (nint)light)
                {
                    Light blight = new(gposeLight, gposeLight->Transform.Position, gposeLight->Transform.Rotation, gposeLight->Transform.Scale)
                    {
                        IsGPoseLight = true,
                        GposeLightIndex = i
                    };

                    blight.SetIndex((int)i);
                    gposeLights[i] = CreateEntity(blight);

                    break;
                }
            }
        }, delayTicks: 2);

        return value;
    }

    private void LightDtor(BrioLight* light, bool free)
    {
        if(_worldGameLights.Contains((nint)light))
        {
            _worldGameLights.Remove((nint)light);
            RemoveWroldLight(light);
        }

        if(_gPoseService.IsGPosing)
        {
            var gposeController = (BrioEventGPoseController*)&EventFramework.Instance()->EventSceneModule.EventGPoseController;
            for(uint i = 0; i < 3; i++)
            {
                var gposeLight = gposeLights[i];
                if(gposeLight != null && gposeLight.GameLight.Address == (nint)light)
                {
                    RemoveGposeLight(gposeLight.GameLight);
                    gposeLights[i] = null!;
                }
            }
        }

        _lightDtorHook.Original(light, free);
    }

    public Entity? AddWorldLight(BrioLight* light)
    {
        if(_worldGameLights.Contains((nint)light))
        {
            var blight = new Light(light, light->Transform.Position, light->Transform.Rotation, light->Transform.Scale, true);
            blight.SetIndex(_handledLights.Add(blight));

            _handledLightStates.Add(blight, SaveLightDTO(blight, Vector3.Zero)!);

            return CreateEntity(blight);
        }

        return null;
    }

    public void RemoveWroldLight(BrioLight* light)
    {
        var hlight = _handledLights.FirstOrDefault(x => x.Address == ((nint)light));
        if(hlight is not null)
        {
            RemoveLightFromEntityManager(hlight);
            _handledLights.Remove(hlight.Index);

            if(_handledLightStates.TryGetValue(hlight, out LightDTO? ogstate))
            {
                if(hlight.IsValid)
                    LoadLightFromDTO(ogstate, light);
                _handledLightStates.Remove(hlight);
            }
        }
    }

    //

    public void SpawnLight(LightType lightType)
    {
        _framework.RunOnFrameworkThread(() =>
        {
            var gamelight = SpawnGameLight(lightType);

            Light light = new(gamelight, gamelight->Transform.Position, gamelight->Transform.Rotation, gamelight->Transform.Scale);
            light.SetIndex(_spawnedLights.Add(light));

            gamelight->Update();

            CreateEntity(light);

            foreach(var gameLight in _spawnedLights.AsEnumerable())
            {
                Brio.Log.Debug($"BrioLight addres: {gameLight.Address}");
            }

        });
    }

    private LightEntity CreateEntity(Light light)
    {
        var entity = _entityManager.CreateEntityOnEntityContainer<LightEntity>(light);
        light.SetEntityIndex(_lightEntities.Add(entity));
        return entity;
    }

    private BrioLight* SpawnGameLight(LightType lightType)
    {
        BrioLight* light = _createGameLight(lightType, null, null);

        if(_worldGameLights.Contains((nint)light))
        {
            _worldGameLights.Remove((nint)light);
        }

        if(_virtualCameraManager.CurrentCamera is not null)
        {
            if(_virtualCameraManager.CurrentCamera.IsFreeCamera)
            {
                light->Transform.Position = _virtualCameraManager.CurrentCamera.Position;
                light->Transform.Rotation = _virtualCameraManager.CurrentCamera.FreeCameraRotationAsQuaternion;
            }
            else
            {
                light->Transform.Position = _virtualCameraManager.CurrentCamera.BrioCamera->Position;
                light->Transform.Rotation = _virtualCameraManager.CurrentCamera.BrioCamera->CalculateDirectionAsQuaternion();
            }
        }

        if(light->RenderLight != null)
        {
            light->RenderLight->EmissionType = lightType;
            light->RenderLight->Transform = &light->Transform;
            light->RenderLight->LightFlags = LightFlags.Reflection;

            light->RenderLight->Color = new Vector3(20f);
            light->RenderLight->Intensity = 1f;

            light->RenderLight->FalloffType = FalloffType.Quadratic;
            light->RenderLight->FalloffFactor = 1f;
            light->RenderLight->SpotLightAngleDegrees = 45.0f;
            light->RenderLight->AngularFalloffDegrees = 0.5f;

            if(lightType is LightType.SpotLight)
                light->RenderLight->Range = 15;
            if(lightType is LightType.FlatLight)
                light->RenderLight->Range = 10;
            if(lightType is LightType.PointLight)
                light->RenderLight->Range = 8;

            light->RenderLight->FlatLightSkewAngleDegrees = Vector2.Zero;

            light->RenderLight->CharacterShadowRange = 110f;
            light->RenderLight->ShadowPlaneNear = 0.01f;
            light->RenderLight->ShadowPlaneFar = 17.0f;
        }

        return light;
    }

    public void Clone(IGameLight sourceLight)
    {
        if(sourceLight == null || !sourceLight.IsValid)
        {
            Brio.Log.Error("Cannot clone an invalid or null light.");
            return;
        }

        _framework.RunOnFrameworkThread(() =>
        {
            // Spawn a new GameLight
            var clonedGameLight = SpawnGameLight(sourceLight.GameLight->RenderLight->EmissionType);

            Light clonedLight = new(clonedGameLight, clonedGameLight->Transform.Position, clonedGameLight->Transform.Rotation, clonedGameLight->Transform.Scale);
            clonedLight.SetIndex(_spawnedLights.Add(clonedLight));

            // Copy properties from the source light to the cloned light
            clonedGameLight->Transform.Position = sourceLight.GameLight->Transform.Position;
            clonedGameLight->Transform.Rotation = sourceLight.GameLight->Transform.Rotation;

            if(clonedGameLight->RenderLight != null && sourceLight.GameLight->RenderLight != null)
            {
                clonedGameLight->RenderLight->Color = sourceLight.GameLight->RenderLight->Color;
                clonedGameLight->RenderLight->Intensity = sourceLight.GameLight->RenderLight->Intensity;
                clonedGameLight->RenderLight->Range = sourceLight.GameLight->RenderLight->Range;
                clonedGameLight->RenderLight->FalloffFactor = sourceLight.GameLight->RenderLight->FalloffFactor;
                clonedGameLight->RenderLight->SpotLightAngleDegrees = sourceLight.GameLight->RenderLight->SpotLightAngleDegrees;
                clonedGameLight->RenderLight->AngularFalloffDegrees = sourceLight.GameLight->RenderLight->AngularFalloffDegrees;
                clonedGameLight->RenderLight->CharacterShadowRange = sourceLight.GameLight->RenderLight->CharacterShadowRange;
                clonedGameLight->RenderLight->ShadowPlaneNear = sourceLight.GameLight->RenderLight->ShadowPlaneNear;
                clonedGameLight->RenderLight->ShadowPlaneFar = sourceLight.GameLight->RenderLight->ShadowPlaneFar;
            }

            clonedGameLight->Update();

            CreateEntity(clonedLight);

            foreach(var gameLight in _spawnedLights.AsEnumerable())
            {
                Brio.Log.Debug($"Cloned BrioLight address: {gameLight.Address}");
            }
        });
    }

    //

    public void LoadLightFromDTO(LightDTO dto, LightEntity entity)
    {
        if(entity.GameLight.IsValid)
            LoadLightFromDTO(dto, entity.GameLight.GameLight);
    }

    public void LoadLightFromDTO(LightDTO dto, BrioLight* gameLight, Vector3? anchor = null)
    {
        _framework.RunOnFrameworkThread(() =>
        {
            gameLight->Transform.Position = anchor.HasValue ? anchor.Value + dto.RelativePosition : dto.Transform.Position;
            gameLight->Transform.Rotation = dto.Transform.Rotation;
            gameLight->Transform.Scale = dto.Transform.Scale;

            if(gameLight->RenderLight != null)
            {
                gameLight->RenderLight->EmissionType = dto.LightType;
                gameLight->RenderLight->Color = dto.Color;
                gameLight->RenderLight->Intensity = dto.Intensity;
                gameLight->RenderLight->Range = dto.Range;
                gameLight->RenderLight->FalloffFactor = dto.Falloff;
                gameLight->RenderLight->SpotLightAngleDegrees = dto.LightAngle;
                gameLight->RenderLight->AngularFalloffDegrees = dto.FalloffAngle;
                gameLight->RenderLight->CharacterShadowRange = dto.CharacterShadowRange;
                gameLight->RenderLight->ShadowPlaneNear = dto.ShadowPlaneNear;
                gameLight->RenderLight->ShadowPlaneFar = dto.ShadowPlaneFar;
            }

            gameLight->Update();
            gameLight->UpdateTransforms(false);
        });
    }

    public LightDTO? SaveLightDTO(IGameLight light, Vector3 anchor)
    {
        if(light == null || !light.IsValid || light.GameLight->RenderLight == null)
            return null;

        var gameLight = light.GameLight;
        var renderLight = gameLight->RenderLight;

        return new LightDTO
        {
            Transform = new Transform
            {
                Position = gameLight->Transform.Position,
                Rotation = gameLight->Transform.Rotation,
                Scale = gameLight->Transform.Scale
            },

            RelativePosition = ((Vector3)gameLight->Transform.Position) - anchor,

            LightType = renderLight->EmissionType,
            Color = renderLight->Color,
            Intensity = renderLight->Intensity,
            Range = renderLight->Range,
            Falloff = renderLight->FalloffFactor,
            LightAngle = renderLight->SpotLightAngleDegrees,
            FalloffAngle = renderLight->AngularFalloffDegrees,
            CharacterShadowRange = renderLight->CharacterShadowRange,
            ShadowPlaneNear = renderLight->ShadowPlaneNear,
            ShadowPlaneFar = renderLight->ShadowPlaneFar,
            IsGPoseLight = light.IsGPoseLight,
            GposeLightIndex = light.GposeLightIndex
        };
    }

    public void SpawnFromDTO(LightDTO dto, Vector3? anchor = null, FolderEntity? folder = null)
    {
        _framework.RunOnFrameworkThread(() =>
       {
           var gameLight = SpawnGameLight(dto.LightType);

           gameLight->Transform.Position = anchor.HasValue ? anchor.Value + dto.RelativePosition : dto.Transform.Position;
           gameLight->Transform.Rotation = dto.Transform.Rotation;
           gameLight->Transform.Scale = dto.Transform.Scale;

           Light light = new(gameLight, gameLight->Transform.Position, gameLight->Transform.Rotation, gameLight->Transform.Scale);
           light.SetIndex(_spawnedLights.Add(light));

           if(gameLight->RenderLight != null)
           {
               gameLight->RenderLight->EmissionType = dto.LightType;
               gameLight->RenderLight->Color = dto.Color;
               gameLight->RenderLight->Intensity = dto.Intensity;
               gameLight->RenderLight->Range = dto.Range;
               gameLight->RenderLight->FalloffFactor = dto.Falloff;
               gameLight->RenderLight->SpotLightAngleDegrees = dto.LightAngle;
               gameLight->RenderLight->AngularFalloffDegrees = dto.FalloffAngle;
               gameLight->RenderLight->CharacterShadowRange = dto.CharacterShadowRange;
               gameLight->RenderLight->ShadowPlaneNear = dto.ShadowPlaneNear;
               gameLight->RenderLight->ShadowPlaneFar = dto.ShadowPlaneFar;
           }

           gameLight->Update();
           gameLight->UpdateTransforms(false);

           var entity = CreateEntity(light);

           if(!string.IsNullOrEmpty(dto.FriendlyName) && entity is LightEntity lightEntity)
               lightEntity.RawName = dto.FriendlyName;

           if(folder is not null)
               _entityManager.MoveEntity(entity, folder);
       });
    }

    //

    public void RemoveLightFromEntityManager(IGameLight light)
    {
        _framework.RunOnTick(() =>
        {
            var camEnt = _lightEntities.Components[light.EntityIndex];
            if(camEnt is not null)
            {
                _entityManager.DetachEntity(camEnt, true);
                _lightEntities.Remove(light.EntityIndex);
            }
        }, delayTicks: 5);
    }

    public void RemoveGposeLight(IGameLight light)
    {
        _framework.RunOnFrameworkThread(() =>
        {
            if(light.IsValid)
            {
                var camEnt = _lightEntities.Components[light.EntityIndex];
                if(camEnt is not null)
                {
                    _entityManager.DetachEntity(camEnt, true);
                    _lightEntities.Remove(light.EntityIndex);
                }
            }
        });
    }

    public void Destroy(IGameLight light)
    {
        _framework.RunOnFrameworkThread(() =>
        {
            if(light.IsWorldLight)
            {
                RemoveWroldLight(light.GameLight);
                return;
            }

            if(light.IsGPoseLight && CurrentGPoseState != null)
            {
                ToggleGPoseLight(CurrentGPoseState, light.GposeLightIndex);
                return;
            }

            _spawnedLights.Remove(light.Index);
            light.Destroy();

            var camEnt = _lightEntities.Components[light.EntityIndex];
            if(camEnt is not null)
            {
                _entityManager.DetachEntity(camEnt, true);
                _lightEntities.Remove(light.EntityIndex);
            }
        });
    }

    public void DestroyAll()
    {
        if(_framework.IsFrameworkUnloading)
            return;

        _framework.RunOnFrameworkThread(() =>
        {
            foreach(var lights in _spawnedLights)
            {
                Destroy(lights);
            }

            foreach(var light in _handledLights)
            {
                RemoveWroldLight(light.GameLight);
            }

            for(int i = 0; i < gposeLights.Length; i++)
            {
                var gposeLight = gposeLights[i];
                if(gposeLight is not null)
                {
                    RemoveGposeLight(gposeLight.GameLight);
                    gposeLights[i] = null!;
                }
            }
        });
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if(_gPoseService.IsGPosing || framework.IsFrameworkUnloading == false)
        {
            foreach(var light in _spawnedLights.AsEnumerable().Where(x => x.IsValid))
            {
                if(light.GameLight->VisibilityFlags == 0)
                    continue;

                light.Update();
            }
        }
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(newState is false)
        {
            DestroyAll();
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        _lightCtorHook?.Dispose();
        _lightDtorHook?.Dispose();

        DestroyAll();

        GC.SuppressFinalize(this);
    }
}
