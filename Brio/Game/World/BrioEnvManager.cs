
//
// This is from ktisis, might should look at moving this in to FFXIVClientStructs (also very simple to use)
// This need clean up, better naming, comments, etc.
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
    [FieldOffset(0x008)] public uint SkyId;

    [FieldOffset(0x020)] public EnvLighting Lighting;
    [FieldOffset(0x098)] public EnvStars Stars;
    [FieldOffset(0x0C0)] public EnvFog Fog;

    [FieldOffset(0x148)] public EnvClouds Clouds;
    [FieldOffset(0x170)] public EnvRain Rain;
    [FieldOffset(0x1A4)] public EnvParticles Dust;
    [FieldOffset(0x1D8)] public EnvWind Wind;
}

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct EnvLighting
{
    [FieldOffset(0x00)] public Vector3 SunLightColor;
    [FieldOffset(0x0C)] public Vector3 MoonLightColor;
    [FieldOffset(0x18)] public Vector3 Ambient;
    //[FieldOffset(0x24)] public float _unk1;
    [FieldOffset(0x28)] public float AmbientSaturation;
    [FieldOffset(0x2C)] public float Temperature;
    //[FieldOffset(0x30)] public float _unk2;
    //[FieldOffset(0x34)] public float _unk3;
    //[FieldOffset(0x38)] public float _unk4;
}

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvStars
{
    [FieldOffset(0x00)] public float ConstellationIntensity;
    [FieldOffset(0x04)] public float Constellations;
    [FieldOffset(0x08)] public float Stars;
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
    //[FieldOffset(0x18)] public float _unk1;
    //[FieldOffset(0x1C)] public float _unk2;
    [FieldOffset(0x20)] public float Opacity;
    [FieldOffset(0x24)] public float SkyVisibility;
}


[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvClouds
{
    [FieldOffset(0x00)] public Vector3 CloudColor;
    [FieldOffset(0x0C)] public Vector3 Color2;
    [FieldOffset(0x18)] public float Gradient;
    [FieldOffset(0x1C)] public float SideHeight;
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
    //[FieldOffset(0x10)] public float _unk1;
    [FieldOffset(0x14)] public float Size;
    [FieldOffset(0x18)] public Vector4 Color;
    //[FieldOffset(0x28)] public float _unk2;
    //[FieldOffset(0x2C)] public float _unk3;
    //[FieldOffset(0x30)] public uint _unk4;
}

[StructLayout(LayoutKind.Explicit, Size = 0x34)]
public struct EnvParticles
{
    //[FieldOffset(0x00)] public float _unk1;
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
