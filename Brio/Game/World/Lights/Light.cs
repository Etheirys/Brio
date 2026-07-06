using Brio.Game.World.Interop;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;

namespace Brio.Game.World.Lights;

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

    public bool IsGismoVisible { get; set; } = true;
    public bool IsAdvancedGismoVisible { get; set; } = false;
    public bool NeedsUpdate { get; set; }
    public bool IsWorldLight { get; init; }

    public bool IsGPoseLight { get; set; }
    public uint GposeLightIndex { get; set; }

    public Light(BrioLight* gameLight, Vector3 position, Quaternion rotation, Vector3 scale, bool isWorldLight = false)
    {
        _gameLight = gameLight;

        SpawnPosition = position;
        SpawnRotation = rotation;
        SpawnScale = scale;
        IsWorldLight = isWorldLight;
    }

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void SetEntityIndex(int entityIndex)
    {
        _entityIndex = entityIndex;
    }

    public void CopyFrom(BrioLight data)
    {
        if(IsValid)
        {
            GameLight->Transform.Position = data.Transform.Position;
            GameLight->Transform.Rotation = data.Transform.Rotation;

            GameLight->RenderLight->Color = data.RenderLight->Color;
            GameLight->RenderLight->Intensity = data.RenderLight->Intensity;
            GameLight->RenderLight->Range = data.RenderLight->Range;
            GameLight->RenderLight->FalloffFactor = data.RenderLight->FalloffFactor;
            GameLight->RenderLight->SpotLightAngleDegrees = data.RenderLight->SpotLightAngleDegrees;
            GameLight->RenderLight->AngularFalloffDegrees = data.RenderLight->AngularFalloffDegrees;
            GameLight->RenderLight->CharacterShadowRange = data.RenderLight->CharacterShadowRange;
            GameLight->RenderLight->ShadowPlaneNear = data.RenderLight->ShadowPlaneNear;
            GameLight->RenderLight->ShadowPlaneFar = data.RenderLight->ShadowPlaneFar;
        }
    }

    public void Destroy()
    {
        if(IsValid && !IsGPoseLight && !IsWorldLight)
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
    public bool IsAdvancedGismoVisible { get; set; }
    public bool IsWorldLight { get; }
    public bool IsGPoseLight { get; set; }
    public uint GposeLightIndex { get; set; }

    public void Destroy();
    public void Update();
    public void CopyFrom(BrioLight data);
    public void ToggleLight()
    {
        if(IsWorldLight)
        {
            if(GameLight->RenderLight->Intensity > 0f)
                GameLight->RenderLight->Intensity = 0f;
            else
                GameLight->RenderLight->Intensity = 1f;

            return;
        }

        GameLight->VisibilityFlags = (byte)(GameLight->VisibilityFlags == 0 ? 79 : 0);
    }

    public void SetVisibility(bool visible) => GameLight->VisibilityFlags = (byte)(visible ? 79 : 0);
}
