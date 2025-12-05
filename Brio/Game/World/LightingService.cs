
//
// Brio's lights would not be possible without the help of the following projects:
// Massive Help with Dynamis: https://github.com/Exter-N/Dynamis by Exter-N (Ny)
// LightsCameraAction: https://github.com/NeNeppie/LightsCameraAction by NeNeppie
// ZoomTilt https://github.com/Tenrys/ZoomTilt
// Some signatures from Ktisis: https://github.com/ktisis-tools/Ktisis
//

using Brio.Core;
using Brio.Entities;
using Brio.Entities.World;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using StructsTransforms = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;

namespace Brio.Game.World;


// TODO (Ken) Separate this file's types into their own files

public unsafe class LightingService : IDisposable
{
    public LightGizmoOperation Operation { get; set; } = LightGizmoOperation.Universal;
    public LightGizmoCoordinateMode CoordinateMode { get; set; } = LightGizmoCoordinateMode.Local;

    //

    private readonly IFramework _framework;
    private readonly IServiceProvider _serviceProvider;
    private readonly GPoseService _gPoseService;
    private readonly EntityManager _entityManager;
    private readonly VirtualCameraManager _virtualCameraManager;

    // Spawn Lights
    private readonly unsafe delegate* unmanaged<GameLight*, void> _spawnGameLight;
    private readonly unsafe delegate* unmanaged<GameLight*, void> _spawnGameLightCreate;
    private readonly unsafe delegate* unmanaged<GameLight*, void> _spawnGameLightFinalize;

    // Update Lights
    private readonly unsafe delegate* unmanaged<LightRenderObject*, char, void> _updateGameLightRange;
    private readonly unsafe delegate* unmanaged<GameLight*, void> _updateGameLightCulling;
    private readonly unsafe delegate* unmanaged<GameLight*, void> _updateGameLightMaterial;

    private delegate bool ToggleLightDelegate(EventGPoseControllerEX* state, uint index);
    private readonly Hook<ToggleLightDelegate> _toggleLightHook = null!;

    private readonly unsafe delegate* unmanaged<EventGPoseControllerEX*, uint, char> _toggleGPoseLight;

    //
    //

    private readonly ComponentSet<IGameLight> _spawnedLights = [];
    private readonly ComponentSet<LightEntity> _lightEntities = [];

    public unsafe EventGPoseControllerEX* CurrentGPoseState => (EventGPoseControllerEX*)&EventFramework.Instance()->EventSceneModule.EventGPoseController;

    public int SpawnedLightEntitiesCount => _lightEntities.ActiveCount;
    public List<LightEntity> SpawnedLightEntities => [.. _lightEntities];

    //

    public LightEntity? SelectedLightEntity = null;

    //

    public unsafe LightingService(IServiceProvider serviceProvider, EntityManager entityManager, GPoseService gPoseService, VirtualCameraManager virtualCameraManager, IFramework framework, ISigScanner sigScanner, IGameInteropProvider hooks)
    {
        _serviceProvider = serviceProvider;
        _gPoseService = gPoseService;
        _entityManager = entityManager;
        _virtualCameraManager = virtualCameraManager;
        _framework = framework;

        var spawnGameLightAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 89 84 ?? ?? ?? ?? ?? 48 85 C0 0F ?? ?? ?? ?? ?? 48 8B C8");
        _spawnGameLight = (delegate* unmanaged<GameLight*, void>)spawnGameLightAddress;

        var createGameLightAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ?? 48 8B D3 E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 48 8B ?? ?? ?? ?? ?? 40 0F");
        _spawnGameLightCreate = (delegate* unmanaged<GameLight*, void>)createGameLightAddress;

        var finalizeGameLightAddress = sigScanner.ScanText("F6 41 38 01 ?? ?? 48 8B ?? ?? ?? ?? ?? 48");
        _spawnGameLightFinalize = (delegate* unmanaged<GameLight*, void>)finalizeGameLightAddress;

        var updateGameLightTypeRangeAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? FF 15 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? 48 8D 55");
        _updateGameLightRange = (delegate* unmanaged<LightRenderObject*, char, void>)updateGameLightTypeRangeAddress;

        var updateGameLightCullingAddress = sigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 48 8B B9 ?? ?? ?? ??");
        _updateGameLightCulling = (delegate* unmanaged<GameLight*, void>)updateGameLightCullingAddress;

        var updateGameLightMaterialAddress = sigScanner.ScanText("40 53 48 83 EC 20 0F B6 81 ?? ?? ?? ?? 48 8B D9 A8 04 75 45 0C 04 B2 05");
        _updateGameLightMaterial = (delegate* unmanaged<GameLight*, void>)updateGameLightMaterialAddress;

        var toggleLightHookAddress = sigScanner.ScanText("48 83 EC 28 4C 8B C1 83 FA 03 ?? ?? 8B C2");

        _toggleGPoseLight = (delegate* unmanaged<EventGPoseControllerEX*, uint, char>)toggleLightHookAddress;

        _toggleLightHook = hooks.HookFromAddress<ToggleLightDelegate>(toggleLightHookAddress, ToggleLightDetour);
        _toggleLightHook.Enable();

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
        _framework.Update += OnFrameworkUpdate;
    }

    public char ToggleGPoseLight(EventGPoseControllerEX* ptr, uint index) => _toggleGPoseLight(ptr, index);

    public unsafe bool ToggleLightDetour(EventGPoseControllerEX* state, uint index)
    {
        //
        // This is using a similar method that Ktisis' uses for a OnGposeLightToggle
        // It has issues like when using the Gpose Load/Save light preset it will desync from Brio
        // This is because this only fires when you "click" the light toggle buttons in Gpose
        //

        var result = _toggleLightHook.Original(state, index);

        try
        {
            var gposeLight = state->GetLight(index);

            if(gposeLight != null)
            {
                Light light = new(gposeLight, gposeLight->Transform.Position, gposeLight->Transform.Rotation, gposeLight->Transform.Scale)
                {
                    IsGPoseLight = true,
                    GposeLightIndex = index
                };
                light.SetIndex(_spawnedLights.Add(light));

                SpawnGPoseLight(light);
            }
            else if(gposeLight == null)
            {
                var lightToRemove = _spawnedLights.AsEnumerable().FirstOrDefault(x => x.IsGPoseLight && x.GposeLightIndex == index);

                if(lightToRemove is not null)
                    RemoveGposeLight(lightToRemove);
            }
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "An Exception while trying to handle a Gpose light toggle");
        }

        return result;
    }

    //

    public void UpdateLight(GameLight* light)
    {
        if(light is not null)
        {
            _updateGameLightCulling(light);
            _updateGameLightMaterial(light);
        }
    }

    public unsafe void SpawnGPoseLight(Light light)
    {
        LightEntity camEnt = ActivatorUtilities.CreateInstance<LightEntity>(_serviceProvider, light);

        if(_entityManager.TryGetEntity("environment", out var ent))
        {
            _entityManager.AttachEntity(camEnt, ent);

            light.SetEntityIndex(_lightEntities.Add(camEnt));
        }
        else
        {
            // TODO: Remove the light we just created if the entity is not found
        }
    }

    public unsafe void SpawnLight(LightType lightType)
    {
        _framework.RunOnFrameworkThread(() =>
        {
            var gamelight = SpawnGameLight(lightType);

            Light light = new(gamelight, gamelight->Transform.Position, gamelight->Transform.Rotation, gamelight->Transform.Scale);
            light.SetIndex(_spawnedLights.Add(light));

            UpdateLight(gamelight);

            LightEntity camEnt = ActivatorUtilities.CreateInstance<LightEntity>(_serviceProvider, light);

            if(_entityManager.TryGetEntity("environment", out var ent))
            {
                _entityManager.AttachEntity(camEnt, ent);

                light.SetEntityIndex(_lightEntities.Add(camEnt));
            }
            else
            {
                // TODO: Remove the light we just created if the entity is not found
            }

            foreach(var gameLight in _spawnedLights.AsEnumerable())
            {
                Brio.Log.Debug($"GameLight addres: {gameLight.Address}");
            }

        });
    }

    public unsafe GameLight* SpawnGameLight(LightType lightType)
    {
        // This causes memory fragmentation over time I think, maybe we can implement a pooling system later?
        GameLight* light = (GameLight*)NativeHelpers.AllocateAlignedMemory(sizeof(GameLight), 8).Aligned;

        _spawnGameLight(light);
        _spawnGameLightCreate(light);
        _spawnGameLightFinalize(light);

        if(_virtualCameraManager.CurrentCamera is not null)
        {
            if(_virtualCameraManager.CurrentCamera.IsFreeCamera)
            {
                light->Transform.Position = _virtualCameraManager.CurrentCamera.Position;
                light->Transform.Rotation = _virtualCameraManager.CurrentCamera.Rotation.ToEulerAngles();
            }
            else
            {
                light->Transform.Position = _virtualCameraManager.CurrentCamera.BrioCamera->Position;
                light->Transform.Rotation = _virtualCameraManager.CurrentCamera.BrioCamera->CalculateDirectionAsQuaternion();
            }
        }

        if(light->LightRenderObject != null)
        {
            light->LightRenderObject->EmissionType = lightType;
            light->LightRenderObject->Transform = &light->Transform;
            light->LightRenderObject->LightFlags = LightFlags.Reflection;

            light->LightRenderObject->Color = new Vector3(20f);
            light->LightRenderObject->Intensity = 1f;

            light->LightRenderObject->FalloffType = FalloffType.Quadratic;
            light->LightRenderObject->Falloff = 1f;
            light->LightRenderObject->LightAngle = 45.0f;
            light->LightRenderObject->FalloffAngle = 0.5f;

            light->LightRenderObject->Range = 35;
            light->LightRenderObject->Angle = Vector2.Zero;

            light->LightRenderObject->CharacterShadowRange = 110f;
            light->LightRenderObject->ShadowPlaneNear = 0.01f;
            light->LightRenderObject->ShadowPlaneFar = 17.0f;
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
            var clonedGameLight = SpawnGameLight(sourceLight.GameLight->LightRenderObject->EmissionType);

            Light clonedLight = new(clonedGameLight, clonedGameLight->Transform.Position, clonedGameLight->Transform.Rotation, clonedGameLight->Transform.Scale);
            clonedLight.SetIndex(_spawnedLights.Add(clonedLight));

            // Copy properties from the source light to the cloned light
            clonedGameLight->Transform.Position = sourceLight.GameLight->Transform.Position;
            clonedGameLight->Transform.Rotation = sourceLight.GameLight->Transform.Rotation;

            if(clonedGameLight->LightRenderObject != null && sourceLight.GameLight->LightRenderObject != null)
            {
                clonedGameLight->LightRenderObject->Color = sourceLight.GameLight->LightRenderObject->Color;
                clonedGameLight->LightRenderObject->Intensity = sourceLight.GameLight->LightRenderObject->Intensity;
                clonedGameLight->LightRenderObject->Range = sourceLight.GameLight->LightRenderObject->Range;
                clonedGameLight->LightRenderObject->Falloff = sourceLight.GameLight->LightRenderObject->Falloff;
                clonedGameLight->LightRenderObject->LightAngle = sourceLight.GameLight->LightRenderObject->LightAngle;
                clonedGameLight->LightRenderObject->FalloffAngle = sourceLight.GameLight->LightRenderObject->FalloffAngle;
                clonedGameLight->LightRenderObject->CharacterShadowRange = sourceLight.GameLight->LightRenderObject->CharacterShadowRange;
                clonedGameLight->LightRenderObject->ShadowPlaneNear = sourceLight.GameLight->LightRenderObject->ShadowPlaneNear;
                clonedGameLight->LightRenderObject->ShadowPlaneFar = sourceLight.GameLight->LightRenderObject->ShadowPlaneFar;
            }

            UpdateLight(clonedGameLight);

            if(_entityManager.TryGetEntity("environment", out var ent))
            {
                var camEnt = ActivatorUtilities.CreateInstance<LightEntity>(_serviceProvider, clonedLight);
                _entityManager.AttachEntity(camEnt, ent);

                clonedLight.SetEntityIndex(_lightEntities.Add(camEnt));
            }
            else
            {
                // TODO: Remove the light we just created if the entity is not found
            }

            foreach(var gameLight in _spawnedLights.AsEnumerable())
            {
                Brio.Log.Debug($"Cloned GameLight address: {gameLight.Address}");
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
                Color = light.GameLight->LightRenderObject->Color,
                Intensity = light.GameLight->LightRenderObject->Intensity,
                Range = light.GameLight->LightRenderObject->Range,
                Falloff = light.GameLight->LightRenderObject->Falloff,
                LightAngle = light.GameLight->LightRenderObject->LightAngle,
                FalloffAngle = light.GameLight->LightRenderObject->FalloffAngle,
                CharacterShadowRange = light.GameLight->LightRenderObject->CharacterShadowRange,
                ShadowPlaneNear = light.GameLight->LightRenderObject->ShadowPlaneNear,
                ShadowPlaneFar = light.GameLight->LightRenderObject->ShadowPlaneFar,
                LightType = light.GameLight->LightRenderObject->EmissionType
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
                GameLight* gameLight = igameLight.GameLight;

                if(gameLight is null)
                    gameLight = SpawnGameLight(lightData.LightType);

                // Adjust position relative to the central position if provided
                gameLight->Transform.Position = centralPosition.HasValue
                    ? centralPosition.Value + lightData.RelativePosition
                    : lightData.AbsolutePosition;

                gameLight->Transform.Rotation = lightData.Rotation;

                if(gameLight->LightRenderObject != null)
                {
                    gameLight->LightRenderObject->Color = lightData.Color;
                    gameLight->LightRenderObject->Intensity = lightData.Intensity;
                    gameLight->LightRenderObject->Range = lightData.Range;
                    gameLight->LightRenderObject->Falloff = lightData.Falloff;
                    gameLight->LightRenderObject->LightAngle = lightData.LightAngle;
                    gameLight->LightRenderObject->FalloffAngle = lightData.FalloffAngle;
                    gameLight->LightRenderObject->CharacterShadowRange = lightData.CharacterShadowRange;
                    gameLight->LightRenderObject->ShadowPlaneNear = lightData.ShadowPlaneNear;
                    gameLight->LightRenderObject->ShadowPlaneFar = lightData.ShadowPlaneFar;
                }

                UpdateLight(gameLight);

                var light = new Light(gameLight, gameLight->Transform.Position, gameLight->Transform.Rotation, gameLight->Transform.Scale);
                light.SetIndex(_spawnedLights.Add(light));

                if(_entityManager.TryGetEntity("environment", out var ent))
                {
                    var camEnt = ActivatorUtilities.CreateInstance<LightEntity>(_serviceProvider, light);
                    _entityManager.AttachEntity(camEnt, ent);

                    light.SetEntityIndex(_lightEntities.Add(camEnt));
                }
            });

            Brio.Log.Info($"Light loaded from {igameLight.Index}");
        }
        catch(Exception ex)
        {
            Brio.Log.Error("Failed to load light.", ex);
        }
    }

    //

    public unsafe void RemoveGposeLight(IGameLight light)
    {
        _spawnedLights.Remove(light.Index);

        _framework.RunOnFrameworkThread(() =>
        {
            if(light.IsValid && _entityManager.TryGetEntity("environment", out var ent))
            {
                var camEnt = _lightEntities.Components[light.Index];
                if(camEnt is not null)
                {
                    ent.RemoveChild(camEnt);
                    _lightEntities.Remove(light.Index);
                }
            }
        });
    }

    public unsafe void Destroy(IGameLight light)
    {
        _spawnedLights.Remove(light.Index);

        _framework.RunOnFrameworkThread(() =>
        {
            if(light.IsGPoseLight && CurrentGPoseState != null)
            {
                ToggleGPoseLight(CurrentGPoseState, light.GposeLightIndex);
            }

            light.Destroy();

            if(_entityManager.TryGetEntity("environment", out var ent))
            {
                var camEnt = _lightEntities.Components[light.Index];
                if(camEnt is not null)
                {
                    ent.RemoveChild(camEnt);
                    _lightEntities.Remove(light.Index);
                }
            }
        });
    }

    public unsafe void DestroyAllLights()
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
                if(light.GameLight->LightFlags == 0)
                    continue;

                UpdateLight(light.GameLight);
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

    public void Dispose()
    {
        _toggleLightHook.Dispose();

        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
        _framework.Update -= OnFrameworkUpdate;

        DestroyAllLights();

        GC.SuppressFinalize(this);
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct EventGPoseControllerEX
{
    [FieldOffset(0x000)] public EventGPoseController EventGPoseController;

    [FieldOffset(0x0E0)] public unsafe fixed ulong Lights[3];

    public unsafe GameLight* GetLight(uint index) => (GameLight*)Lights[index];
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
    private GameLight* _gameLight;
    private int _index;
    private int _entityIndex;

    public int Index => _index;
    public int EntityIndex => _entityIndex;
    public bool IsValid => GameLight != null;

    public GameLight* GameLight => _gameLight;
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

    public Light(GameLight* gameLight, Vector3 position, Quaternion rotation, Vector3 scale)
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

            NativeHelpers.FreeMemory((nint)GameLight); // Is this overkill?

            _gameLight = null;
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
    public bool IsVisible => GameLight != null && GameLight->LightFlags != 0;

    public Vector3 SpawnPosition { get; set; }
    public Quaternion SpawnRotation { get; set; }
    public Vector3 SpawnScale { get; set; }

    public GameLight* GameLight { get; }
    public IntPtr Address { get; }

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }

    public bool IsGismoVisible { get; set; }
    public bool IsGPoseLight { get; set; }

    public uint GposeLightIndex { get; set; }

    public void Destroy();
    public void ToggleLight() => GameLight->LightFlags = (byte)(GameLight->LightFlags == 0 ? 79 : 0);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct GameLightVirtualTable
{
    [FieldOffset(0)]
    public unsafe delegate* unmanaged<GameLight*, bool, void> Destructor;

    [FieldOffset(8)]
    public unsafe delegate* unmanaged<GameLight*, void> Cleanup;
}

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public unsafe struct GameLight
{
    [FieldOffset(0x00)] public unsafe GameLightVirtualTable* VirtualTable;

    [FieldOffset(0x00)] public DrawObject DrawObject;
    [FieldOffset(0x50)] public StructsTransforms Transform;
    [FieldOffset(0x88)] public byte LightFlags;                      // This seems to be only useful for visibility? (0 = off, 79 = on)
    [FieldOffset(0x90)] public LightRenderObject* LightRenderObject; // GetObjectType() == 5


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Destroy()
    {
        VirtualTable->Cleanup((GameLight*)Unsafe.AsPointer(ref this));
        VirtualTable->Destructor((GameLight*)Unsafe.AsPointer(ref this), false);
    }
}

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public unsafe struct LightRenderObject
{
    [FieldOffset(0x00)] public nint* VirtualTable;

    [FieldOffset(0x18)] public LightFlags LightFlags;
    [FieldOffset(0x1C)] public LightType EmissionType;
    [FieldOffset(0x20)] public StructsTransforms* Transform;
    [FieldOffset(0x28)] public Vector3 Color;
    [FieldOffset(0x34)] public float Intensity;
    [FieldOffset(0x40)] public Vector3 MaxRangeNegative;            // Gpose lights have "unlimited" (-10000) range
    [FieldOffset(0x50)] public Vector3 MaxRangePositive;            // Gpose lights have "unlimited" (10000) range
    [FieldOffset(0x60)] public float ShadowPlaneNear;
    [FieldOffset(0x64)] public float ShadowPlaneFar;
    [FieldOffset(0x68)] public FalloffType FalloffType;             // Type 1: 2 (Cubic), Type 2: 1 (Quadratic), Type 3: 0 (Linear)
    [FieldOffset(0x70)] public Vector2 Angle;
    [FieldOffset(0x80)] public float Falloff;
    [FieldOffset(0x84)] public float LightAngle;
    [FieldOffset(0x88)] public float FalloffAngle;
    [FieldOffset(0x8C)] public float Range;                         // Seems to be centered on the player
    [FieldOffset(0x90)] public float CharacterShadowRange;
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
    AreaLight = 2,
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
