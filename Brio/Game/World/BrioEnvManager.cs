
//
// This is originally from ktisis, Now modified and made correct. should look at moving this in to FFXIVClientStructs
//

using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Game.World;

[StructLayout(LayoutKind.Explicit, Size = 0x910)]
public struct BrioEnvManager
{
    [FieldOffset(0x000)] public EnvManager Manager;
    [FieldOffset(0x058)] public EnvState EnvState;

    public unsafe static BrioEnvManager* Instance() => (BrioEnvManager*)EnvManager.Instance();
}

[StructLayout(LayoutKind.Explicit, Size = 0x2F8)]
public struct EnvState
{
    [FieldOffset(0x008)] public uint SkyTextureID;
    [FieldOffset(0x020)] public EnvLighting EnvironmentLighting;
    [FieldOffset(0x098)] public EnvStars Stars;
    [FieldOffset(0x0C0)] public EnvFog Fog;
    [FieldOffset(0x148)] public EnvClouds Clouds;
    [FieldOffset(0x170)] public EnvRain Rain;
    [FieldOffset(0x1A4)] public EnvParticles Particles;
    [FieldOffset(0x1D8)] public EnvWind Wind;
}

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct EnvLighting
{
    [FieldOffset(0x00)] public Vector3 SunlightColor;
    [FieldOffset(0x0C)] public Vector3 MoonlightColor;
    [FieldOffset(0x18)] public Vector3 AmbientColor;
    [FieldOffset(0x24)] public float Unknown1;
    [FieldOffset(0x28)] public float AmbientSaturation;
    [FieldOffset(0x2C)] public float AmbientTemperature;
    [FieldOffset(0x30)] public float Unknown2;              // Something with sadow colors
    [FieldOffset(0x34)] public float LightDistance;         // This is like a World Vignette, something something light distance from the Camera's 
    [FieldOffset(0x38)] public float Unknown4;
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvStars
{
    [FieldOffset(0x00)] public float ConstellationIntensity;
    [FieldOffset(0x04)] public float ConstellationCount;
    [FieldOffset(0x08)] public float StarCount;
    [FieldOffset(0x0C)] public float GalaxyIntensity;
    [FieldOffset(0x10)] public float StarIntensity;
    [FieldOffset(0x14)] public Vector4 MoonColor;
    [FieldOffset(0x24)] public float MoonBrightness;
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvFog
{
    [FieldOffset(0x00)] public Vector4 Color;
    [FieldOffset(0x10)] public float Distance;
    [FieldOffset(0x14)] public float Thickness; 
    [FieldOffset(0x18)] public float SkySmoothness;         // SkyOpacity Smoothness, These two values are probably actually near and far sky depth values, but this is what I will name them
    [FieldOffset(0x1C)] public float SkyOpacity;            // This is fog Opacity 0 - 10
    [FieldOffset(0x20)] public float FogOpacity;
    [FieldOffset(0x24)] public float SunVisibility;
}


[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvClouds
{
    [FieldOffset(0x00)] public Vector3 CloudColor1;
    [FieldOffset(0x0C)] public Vector3 CloudColor2;
    [FieldOffset(0x18)] public float ShadowStop;
    [FieldOffset(0x1C)] public float CloudHeight;
    [FieldOffset(0x20)] public uint CloudTexture;
    [FieldOffset(0x24)] public uint CloudSideTexture;
}

[StructLayout(LayoutKind.Explicit, Size = 0x34)]
public struct EnvRain
{
    [FieldOffset(0x00)] public float Raindrops;
    [FieldOffset(0x04)] public float Intensity;
    [FieldOffset(0x08)] public float Weight;
    [FieldOffset(0x0C)] public float Scatter;
    [FieldOffset(0x10)] public float Unknown1;
    [FieldOffset(0x14)] public float Size;
    [FieldOffset(0x18)] public Vector4 Color;
    [FieldOffset(0x28)] public float Unknown2;
    [FieldOffset(0x2C)] public float Unknown3;
    [FieldOffset(0x30)] public uint Unknown4;
}

[StructLayout(LayoutKind.Explicit, Size = 0x34)]
public struct EnvParticles
{
    [FieldOffset(0x00)] public float Unknown1;
    [FieldOffset(0x04)] public float Intensity;
    [FieldOffset(0x08)] public float Weight;
    [FieldOffset(0x0C)] public float Spread;
    [FieldOffset(0x10)] public float Speed;
    [FieldOffset(0x14)] public float Size;
    [FieldOffset(0x18)] public Vector4 Color;
    [FieldOffset(0x28)] public float Glow;
    [FieldOffset(0x2C)] public float Spin;
    [FieldOffset(0x30)] public uint TextureId;
}

[StructLayout(LayoutKind.Explicit, Size = 0x0C)]
public struct EnvWind
{
    [FieldOffset(0x00)] public float Direction;
    [FieldOffset(0x04)] public float Angle;
    [FieldOffset(0x08)] public float Speed;
}
