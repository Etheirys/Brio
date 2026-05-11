using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Game.VFX.Intertop;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxResourceInstanceVtable
{

}

[StructLayout(LayoutKind.Explicit, Size = 0xD0)]
public unsafe struct VfxResourceInstance
{
    [FieldOffset(0x00)] public VfxResourceInstanceVtable* Vtable;

    [FieldOffset(0x08)] public VfxResourceObject* VfxResourceHandle;
    [FieldOffset(0x10)] public VfxResourceInstanceListenerVtable* OwnerListener;              // this points to the VfxData's "VfxResourceInstanceListener" ListenerVtable
    [FieldOffset(0x18)] public VfxData* Owner;

    [FieldOffset(0x70)] public int Cue;
    [FieldOffset(0x74)] public int ElapsedFrames;
 
    [FieldOffset(0x90)] public Vector3 Scale; // Relly unsure about these two
    [FieldOffset(0xA0)] public Vector4 Color;

    [FieldOffset(0xB4)] public byte PlayMode;
    [FieldOffset(0xB5)] public byte SpeedFlag;
    [FieldOffset(0xC4)] public byte ActiveFlag;
}


[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxResourceObjectVtable
{
    [FieldOffset(0)]
    public delegate* unmanaged<VfxResourceObject*, nint, nint> Dtor;
}

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct VfxResourceObject // VfxResourceHandle
{
    [FieldOffset(0x00)] public VfxResourceObjectVtable* Vtable;

    [FieldOffset(0x08)] public int RefCount;
    [FieldOffset(0x18)] public ApricotResourceHandle* ApricotResourceHandle;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxResourceInstanceListenerVtable
{

}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxDataVtable
{
    [FieldOffset(0)]
    public delegate* unmanaged<VfxData*, uint, nint> Dtor;

    [FieldOffset(8)]
    public delegate* unmanaged<VfxData*, uint, nint> GetCaster;

}

// Client::Graphics::Vfx::VfxData
//
[StructLayout(LayoutKind.Explicit, Size = 0x1E0)]
public unsafe struct VfxData
{
    [FieldOffset(0x00)] public VfxDataVtable* Vtable;

    [FieldOffset(0x10)] public fixed byte BuilderData[0x1A0];

    [FieldOffset(0xE0)] public int AttachFlags;                                              // bone attach, masked with 0xFF1FFFFF before packing

    [FieldOffset(0x1B0)] public VfxResourceInstanceListenerVtable* ListenerVtable;           // VfxResourceInstance->OwnerListener points here

    [FieldOffset(0x1B8)] public VfxResourceInstance* VfxResourceInstance;

    //[FieldOffset(0x1C0)] public VfxObject* VfxObject;

    [FieldOffset(0x1C8)] public byte Unk2;

    [FieldOffset(0x1CC)] public int Flags;

    [FieldOffset(0x1D0)] public byte StateFlags;
}
