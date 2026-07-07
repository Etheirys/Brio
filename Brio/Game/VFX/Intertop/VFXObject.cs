using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Common.Math;
using System.Runtime.InteropServices;

namespace Brio.Game.VFX.Intertop;


[StructLayout(LayoutKind.Explicit, Size = 0x390)]
public unsafe struct VFXObject
{
    [FieldOffset(0x00)] public VfxObject* BaseObject;

    [FieldOffset(0x38)] public byte DrawObjectFlags;

    [FieldOffset(0x50)] public Vector3 Position;
    [FieldOffset(0x60)] public Quaternion Rotation;
    [FieldOffset(0x70)] public Vector3 Scale;

    [FieldOffset(0x128)] public int ActorCaster;
    [FieldOffset(0x130)] public int ActorTarget;

    [FieldOffset(0x1B8)] public int StaticCaster;
    [FieldOffset(0x1C0)] public int StaticTarget;

    [FieldOffset(0x248)] public uint Flags;

    [FieldOffset(0x24C)] public byte LoopMode;
    [FieldOffset(0x24D)] public byte LoopModeB;

    [FieldOffset(0x250)] public uint TriggerParam;
    [FieldOffset(0x254)] public uint Unk1;
    [FieldOffset(0x25C)] public uint CurrentTrigger;

    [FieldOffset(0x260)] public byte Red;
    [FieldOffset(0x264)] public byte Green;
    [FieldOffset(0x268)] public byte Blue;

    [FieldOffset(0x26C)] public float Alpha;

}
