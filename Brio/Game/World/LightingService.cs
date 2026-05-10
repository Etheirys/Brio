
//
// Brio's lights would not be possible without the help of the following projects:
// Massive Help with Dynamis: https://github.com/Exter-N/Dynamis by Exter-N (Ny)
// LightsCameraAction: https://github.com/NeNeppie/LightsCameraAction by NeNeppie
//

using Brio.Core;
using Brio.Entities;
using Brio.Entities.Core;
using Brio.Entities.World;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Services;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using InteropGenerator.Runtime;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using StructsAABounds = FFXIVClientStructs.FFXIV.Common.Math.AxisAlignedBounds;
using StructsTransforms = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;

namespace Brio.Game.World;

public unsafe class LightingService : MediatorSubscriberBase
{
    public LightGizmoOperation Operation { get; set; } = LightGizmoOperation.Universal;
    public LightGizmoCoordinateMode CoordinateMode { get; set; } = LightGizmoCoordinateMode.Local;

    //

    private readonly IFramework _framework;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameInteropProvider _hooks;
    private readonly GPoseService _gPoseService;
    private readonly EntityManager _entityManager;
    private readonly VirtualCameraManager _virtualCameraManager;

    private readonly delegate* unmanaged<EventGPoseControllerEX*, uint, char> _toggleGPoseLight;
    private readonly delegate* unmanaged<LightType, CStringPointer, BrioLight*, BrioLight*> _createGameLight;

    private delegate BrioLight* LightDelegate(BrioLight* light);
    private readonly Hook<LightDelegate> _lightCtorHook = null!;

    public delegate* unmanaged<BrioLight*, bool, void> Destructor;

    private delegate void LightDtorDelegate(BrioLight* thisPtr, bool free);
    private Hook<LightDtorDelegate> _lightDtorHook = null!;

    //
    //

    private readonly ComponentSet<IGameLight> _spawnedLights = [];
    private readonly ComponentSet<LightEntity> _lightEntities = [];

    public EventGPoseControllerEX* CurrentGPoseState => (EventGPoseControllerEX*)&EventFramework.Instance()->EventSceneModule.EventGPoseController;

    public int SpawnedLightEntitiesCount => _lightEntities.ActiveCount;
    public List<LightEntity> SpawnedLightEntities => [.. _lightEntities];

    //

    public LightEntity? SelectedLightEntity = null;

    //

    public LightingService(IServiceProvider serviceProvider, EntityManager entityManager, GPoseService gPoseService, VirtualCameraManager virtualCameraManager, IFramework framework, ISigScanner sigScanner, IGameInteropProvider hooks, Mediator mediator) : base(mediator)
    {
        _serviceProvider = serviceProvider;
        _gPoseService = gPoseService;
        _entityManager = entityManager;
        _virtualCameraManager = virtualCameraManager;
        _framework = framework;
        _hooks = hooks;

        var createGameLightAddress = sigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 8B F9");   // Light.Create
        _createGameLight = (delegate* unmanaged<LightType, CStringPointer, BrioLight*, BrioLight*>)createGameLightAddress;

        var toggleLightHookAddress = sigScanner.ScanText("48 83 EC 28 4C 8B C1 83 FA 03 ?? ?? 8B C2");
        _toggleGPoseLight = (delegate* unmanaged<EventGPoseControllerEX*, uint, char>)toggleLightHookAddress;

        var _lightCtorAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 89 84 ?? ?? ?? ?? ?? 48 85 C0 0F ?? ?? ?? ?? ?? 48 8B C8");   // Light.ctor
        _lightCtorHook = hooks.HookFromAddress<LightDelegate>(_lightCtorAddress, LightCtor);
        _lightCtorHook.Enable();

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
        _framework.Update += OnFrameworkUpdate;
    }

    public char ToggleGPoseLight(EventGPoseControllerEX* ptr, uint index)
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
            var gposeController = (EventGPoseControllerEX*)&EventFramework.Instance()->EventSceneModule.EventGPoseController;
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
                    ? centralPosition.Value + lightData.RelativePosition
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

        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
        _framework.Update -= OnFrameworkUpdate;

        DestroyAllLights();
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct EventGPoseControllerEX
{
    [FieldOffset(0x000)] public EventGPoseController EventGPoseController;

    [FieldOffset(0x0E0)] public unsafe fixed ulong Lights[3];

    public unsafe BrioLight* GetLight(uint index) => (BrioLight*)Lights[index];
}

public class LightData
{
    public Vector3 AbsolutePosition { get; set; }
    public Vector3 RelativePosition { get; set; }

    public Quaternion Rotation { get; set; }

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

public unsafe class Light : IGameLight, IDisposable
{
    private BrioLight* _gameLight;
    private int _index;
    private int _entityIndex;

    public int Index => _index;
    public int EntityIndex => _entityIndex;
    public bool IsValid => GameLight != null;

    public BrioLight* GameLight => _gameLight;
    public IntPtr Address => (nint)GameLight;

    public Vector3 Position => GameLight->Transform.Position;
    public Quaternion Rotation => GameLight->Transform.Rotation;

    public Vector3 SpawnPosition { get; set; }
    public Quaternion SpawnRotation { get; set; }
    public Vector3 SpawnScale { get; set; }

    public bool IsGismoVisible { get; set; } = false;
    public bool NeedsUpdate { get; set; }

    public bool IsGPoseLight { get; set; }
    public uint GposeLightIndex { get; set; }

    public Light(BrioLight* gameLight, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        _gameLight = gameLight;

        SpawnPosition = position;
        SpawnRotation = rotation;
        SpawnScale = scale;
    }

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void SetEntityIndex(int entityIndex)
    {
        _entityIndex = entityIndex;
    }

    public void Destroy()
    {
        if(IsValid && IsGPoseLight is false)
        {
            GameLight->Destroy();

            // NativeHelpers.FreeMemory((nint)GameLight); // Is this overkill?

            _gameLight = null;
        }
    }

    public void Update()
    {
        if(IsValid)
        {
            GameLight->Update();
        }
    }

    public virtual void Dispose()
    {
        Destroy();

        GC.SuppressFinalize(this);
    }
}

public unsafe interface IGameLight
{
    public int Index { get; }
    public int EntityIndex { get; }
    public bool IsValid { get; }
    public bool NeedsUpdate { get; set; }
    public bool IsVisible => GameLight != null && GameLight->VisibilityFlags != 0;

    public Vector3 SpawnPosition { get; set; }
    public Quaternion SpawnRotation { get; set; }
    public Vector3 SpawnScale { get; set; }

    public BrioLight* GameLight { get; }
    public IntPtr Address { get; }

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }

    public bool IsGismoVisible { get; set; }
    public bool IsGPoseLight { get; set; }

    public uint GposeLightIndex { get; set; }

    public void Destroy();
    public void Update();
    public void ToggleLight() => GameLight->VisibilityFlags = (byte)(GameLight->VisibilityFlags == 0 ? 79 : 0);
    public void SetVisibility(bool visible) => GameLight->VisibilityFlags = (byte)(visible ? 79 : 0);
}


[StructLayout(LayoutKind.Explicit, Size = 0xB0)]
public unsafe struct BrioLight
{
    [StructLayout(LayoutKind.Explicit)]
    public struct GameLightVirtualTable
    {
        [FieldOffset(0)]
        public delegate* unmanaged<BrioLight*, bool, void> Destructor;

        [FieldOffset(8)]
        public delegate* unmanaged<BrioLight*, void> Cleanup;
    }

    [FieldOffset(0x00)] public GameLightVirtualTable* VirtualTable;

    [FieldOffset(0x00)] public DrawObject DrawObject;

    [FieldOffset(0x50)] public StructsTransforms Transform;

    [FieldOffset(0x88)] public byte VisibilityFlags;

    [FieldOffset(0x90)] public LightRenderObject* RenderLight;

    //[FieldOffset(0x98)] public TextureResourceHandle* ProjectedCubemapTexture;

    public bool IsVisible
    {
        get => DrawObject.IsVisible;
        set => DrawObject.IsVisible = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        UpdateCulling();
        UpdateMaterials();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Destroy()
    {
        VirtualTable->Cleanup((BrioLight*)Unsafe.AsPointer(ref this));
        VirtualTable->Destructor((BrioLight*)Unsafe.AsPointer(ref this), true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateCulling()
    {
        DrawObject.VirtualTable->UpdateCulling((DrawObject*)Unsafe.AsPointer(in this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateTransforms(bool a2_unk)
    {
        DrawObject.VirtualTable->UpdateTransforms((DrawObject*)Unsafe.AsPointer(in this), a2_unk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateMaterials()
    {
        DrawObject.VirtualTable->UpdateMaterials((DrawObject*)Unsafe.AsPointer(in this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateRender()
    {
        DrawObject.VirtualTable->UpdateRender((DrawObject*)Unsafe.AsPointer(in this));
    }
}

[StructLayout(LayoutKind.Explicit, Size = 0x160)]
public unsafe struct LightRenderObject
{
    [FieldOffset(0x18)] public LightFlags LightFlags;
    [FieldOffset(0x1C)] public LightType EmissionType;
    [FieldOffset(0x20)] public StructsTransforms* Transform;
    [FieldOffset(0x28)] public Vector4 ColorIntensity;
    [FieldOffset(0x40)] public StructsAABounds MaxRange;
    [FieldOffset(0x60)] public float ShadowPlaneNear;
    [FieldOffset(0x64)] public float ShadowPlaneFar;
    [FieldOffset(0x68)] public FalloffType FalloffType;             // Type 1: 2 (Cubic), Type 2: 1 (Quadratic), Type 3: 0 (Linear)
    [FieldOffset(0x70)] public Vector2 FlatLightSkewAngleDegrees;
    [FieldOffset(0x80)] public float FalloffFactor;
    [FieldOffset(0x84)] public float SpotLightAngleDegrees;
    [FieldOffset(0x88)] public float AngularFalloffDegrees;
    [FieldOffset(0x8C)] public float Range;                         // Seems to be centered on the player
    [FieldOffset(0x90)] public float CharacterShadowRange;

    [FieldOffset(0xA0)] public StructsAABounds CullingBounds;
    [FieldOffset(0xC0)] public StructsAABounds RangeBounds;

    public Vector3 Color { readonly get => new(ColorIntensity.X, ColorIntensity.Y, ColorIntensity.Z); set => ColorIntensity = new Vector4(value, ColorIntensity.W); }
    public float Intensity { readonly get => ColorIntensity.W; set => ColorIntensity.W = value; }
}

[Flags]
public enum LightFlags
{
    Reflection = 1,
    Dynamic = 2,
    CharaShadow = 4,
    ObjectShadow = 8
}

public enum LightType : uint
{
    WorldLight = 1,
    PointLight = 2,
    SpotLight = 3,
    FlatLight = 4
}

public enum FalloffType : uint
{
    Linear = 0,
    Quadratic = 1,
    Cubic = 2
}


//
// Did someone call for some tech debt?
// 

public enum LightGizmoCoordinateMode
{
    Local,
    World
}

public enum LightGizmoOperation
{
    Translate,
    Rotate,
    Universal
}

public static class LightExtensions
{
    public static ImGuizmoMode AsGizmoMode(this LightGizmoCoordinateMode mode) => mode switch
    {
        LightGizmoCoordinateMode.Local => ImGuizmoMode.Local,
        LightGizmoCoordinateMode.World => ImGuizmoMode.World,
        _ => ImGuizmoMode.Local
    };

    public static ImGuizmoOperation AsGizmoOperation(this LightGizmoOperation operation) => operation switch
    {
        LightGizmoOperation.Translate => ImGuizmoOperation.Translate,
        LightGizmoOperation.Rotate => ImGuizmoOperation.Rotate,
        LightGizmoOperation.Universal => ImGuizmoOperation.Translate | ImGuizmoOperation.Rotate,
        _ => ImGuizmoOperation.Universal
    };
}
