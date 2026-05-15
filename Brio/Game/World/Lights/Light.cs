using Brio.Game.World.Interop;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;

namespace Brio.Game.World.Lights;

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
