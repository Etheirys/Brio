using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using StructsAABounds = FFXIVClientStructs.FFXIV.Common.Math.AxisAlignedBounds;
using StructsTransforms = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;

namespace Brio.Game.World.Interop;

[StructLayout(LayoutKind.Explicit, Size = 0xB0)]
public unsafe struct BrioLight
{
    [StructLayout(LayoutKind.Explicit)]
    public struct GameLightVirtualTable
    {
        [FieldOffset(0)]
        public delegate* unmanaged<BrioLight*, bool, nint> Destructor;

        [FieldOffset(8)]
        public delegate* unmanaged<BrioLight*, void> Cleanup;
    }

    [FieldOffset(0x00)] public GameLightVirtualTable* VirtualTable;

    [FieldOffset(0x00)] public DrawObject DrawObject;

    [FieldOffset(0x50)] public StructsTransforms Transform;

    [FieldOffset(0x88)] public byte VisibilityFlags;

    [FieldOffset(0x90)] public LightRenderObject* RenderLight;

    [FieldOffset(0x98)] public TextureResourceHandle* ProjectedCubemapTexture;

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
