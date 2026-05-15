
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
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using InteropGenerator.Runtime;
using Microsoft.Extensions.DependencyInjection;
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

    private readonly ComponentSet<IGameLight> _spawnedLights = [];
    private readonly ComponentSet<LightEntity> _lightEntities = [];

    public BrioEventGPoseController* CurrentGPoseState => (BrioEventGPoseController*)&EventFramework.Instance()->EventSceneModule.EventGPoseController;

    public int SpawnedLightEntitiesCount => _lightEntities.ActiveCount;
    public List<LightEntity> SpawnedLightEntities => [.. _lightEntities];

    public LightEntity? SelectedLightEntity = null;

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

    public char ToggleGPoseLight(BrioEventGPoseController* ptr, uint index)
        => _toggleGPoseLight(ptr, index);

    public BrioLight* LightCtor(BrioLight* light)
    {
        Brio.Log.Debug($"Light Created at address: {(nint)light}");
        var value = _lightCtorHook.Original(light);

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
                    Light blight = new(gposeLight, gposeLight->Transform.Position,
                        gposeLight->Transform.Rotation, gposeLight->Transform.Scale)
                    {
                        IsGPoseLight = true,
                        GposeLightIndex = i
                    };
                    blight.SetIndex(_spawnedLights.Add(blight));
                    SpawnGPoseLight(blight);
                    break;
                }
            }
        }, delayTicks: 2);

        return value;
    }

    public void LightDtor(BrioLight* light, bool free)
    {
        Brio.Log.Debug($"Light Destroyed at address: {(nint)light}");

        if(_gPoseService.IsGPosing is false)
        {
            _lightDtorHook.Original(light, free);

            return;
        }

        var lightToRemove = _spawnedLights.AsEnumerable()
            .FirstOrDefault(x => x.IsGPoseLight && (nint)x.Address == (nint)light);

        if(lightToRemove is not null)
            RemoveGposeLight(lightToRemove);

        _lightDtorHook.Original(light, free);
    }

    public void SpawnGPoseLight(Light light)
    {
        LightEntity camEnt = ActivatorUtilities.CreateInstance<LightEntity>(_serviceProvider, light);

        CreateEntity(light);
    }

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

    public Entity CreateEntity(Light light)
    {
        var entity = _entityManager.CreateEntityOnEntityContainer<LightEntity>(light);
        light.SetEntityIndex(_lightEntities.Add(entity));
        return entity;
    }

    public BrioLight* SpawnGameLight(LightType lightType)
    {
        BrioLight* light = _createGameLight(lightType, null, null);

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

            light->RenderLight->Range = 35;
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

    public LightData? SaveLight(IGameLight light)
    {
        try
        {
            if(light == null || !light.IsValid)
            {
                Brio.Log.Warning("Cannot save an invalid or null light.");
                return null;
            }

            return new LightData
            {
                AbsolutePosition = light.Position,
                Rotation = light.Rotation,
                Color = light.GameLight->RenderLight->Color,
                Intensity = light.GameLight->RenderLight->Intensity,
                Range = light.GameLight->RenderLight->Range,
                Falloff = light.GameLight->RenderLight->FalloffFactor,
                LightAngle = light.GameLight->RenderLight->SpotLightAngleDegrees,
                FalloffAngle = light.GameLight->RenderLight->AngularFalloffDegrees,
                CharacterShadowRange = light.GameLight->RenderLight->CharacterShadowRange,
                ShadowPlaneNear = light.GameLight->RenderLight->ShadowPlaneNear,
                ShadowPlaneFar = light.GameLight->RenderLight->ShadowPlaneFar,
                LightType = light.GameLight->RenderLight->EmissionType
            };
        }
        catch(Exception ex)
        {
            Brio.Log.Error("Failed to save light.", ex);
        }

        return null;
    }

    public void LoadLight(LightData lightData, IGameLight igameLight, Vector3? centralPosition = null)
    {
        try
        {
            // var lightData = JsonSerializer.Deserialize<LightData>(json);

            if(lightData is null)
            {
                Brio.Log.Warning("No light data to load.");
                return;
            }

            _framework.RunOnFrameworkThread(() =>
            {
                BrioLight* gameLight = igameLight.GameLight;

                if(gameLight is null)
                    gameLight = SpawnGameLight(lightData.LightType);

                // Adjust position relative to the central position if provided
                gameLight->Transform.Position = centralPosition.HasValue
                    ? centralPosition.Value + (Vector3)lightData.RelativePosition
                    : lightData.AbsolutePosition;

                gameLight->Transform.Rotation = lightData.Rotation;

                if(gameLight->RenderLight != null)
                {
                    gameLight->RenderLight->Color = lightData.Color;
                    gameLight->RenderLight->Intensity = lightData.Intensity;
                    gameLight->RenderLight->Range = lightData.Range;
                    gameLight->RenderLight->FalloffFactor = lightData.Falloff;
                    gameLight->RenderLight->SpotLightAngleDegrees = lightData.LightAngle;
                    gameLight->RenderLight->AngularFalloffDegrees = lightData.FalloffAngle;
                    gameLight->RenderLight->CharacterShadowRange = lightData.CharacterShadowRange;
                    gameLight->RenderLight->ShadowPlaneNear = lightData.ShadowPlaneNear;
                    gameLight->RenderLight->ShadowPlaneFar = lightData.ShadowPlaneFar;
                }

                gameLight->Update();

                var light = new Light(gameLight, gameLight->Transform.Position, gameLight->Transform.Rotation, gameLight->Transform.Scale);
                light.SetIndex(_spawnedLights.Add(light));

                CreateEntity(light);
            });

            Brio.Log.Info($"Light loaded from {igameLight.Index}");
        }
        catch(Exception ex)
        {
            Brio.Log.Error("Failed to load light.", ex);
        }
    }

    //

    public void RemoveGposeLight(IGameLight light)
    {
        _spawnedLights.Remove(light.Index);

        _framework.RunOnFrameworkThread(() =>
        {
            if(light.IsValid)
            {
                var camEnt = _lightEntities.Components[light.Index];
                if(camEnt is not null)
                {
                    _entityManager.RemoveEntityFromEntityContainer(camEnt);
                    _lightEntities.Remove(light.Index);
                }
            }
        });
    }

    public void Destroy(IGameLight light)
    {
        _spawnedLights.Remove(light.Index);

        _framework.RunOnFrameworkThread(() =>
        {
            if(light.IsGPoseLight && CurrentGPoseState != null)
            {
                ToggleGPoseLight(CurrentGPoseState, light.GposeLightIndex);
            }

            light.Destroy();

            var camEnt = _lightEntities.Components[light.Index];
            if(camEnt is not null)
            {
                _entityManager.RemoveEntityFromEntityContainer(camEnt);
                _lightEntities.Remove(light.Index);
            }
        });
    }

    public void DestroyAllLights()
    {
        if(_framework.IsFrameworkUnloading)
            return;

        _framework.RunOnFrameworkThread(() =>
        {
            foreach(var lights in _spawnedLights)
            {
                Destroy(lights);
            }

            _spawnedLights.Clear();
            _lightEntities.Clear();
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
            DestroyAllLights();
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        _lightCtorHook?.Dispose();
        _lightDtorHook?.Dispose();

        DestroyAllLights();

        GC.SuppressFinalize(this);
    }
}
