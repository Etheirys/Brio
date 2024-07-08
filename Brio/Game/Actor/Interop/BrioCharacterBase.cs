using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Game.Actor.Interop;

[StructLayout(LayoutKind.Explicit, Size = 0x9D0)]
internal struct BrioCharacterBase
{
    [FieldOffset(0x0)] public CharacterBase CharacterBase;

    [FieldOffset(0x0D0)] public Attach Attach;

    [FieldOffset(0x290)] public Vector4 Tint;

    [FieldOffset(0x2A0)] public float ScaleFactor1;
    [FieldOffset(0x2A4)] public float ScaleFactor2;

    [FieldOffset(0x2E4)] public float Wetness;
    [FieldOffset(0x2E8)] public float WetnessDepth;


    public readonly float ScaleFactor => ScaleFactor1 * ScaleFactor2;
}

[StructLayout(LayoutKind.Explicit, Size = 0x78)]
internal unsafe struct Attach
{
    [FieldOffset(0x0)] public Task Task;

    [FieldOffset(0x50)] public AttachType Type;

    [FieldOffset(0x58)] public unsafe Skeleton* Target;
    [FieldOffset(0x60)] public unsafe void* Parent; // See Type

    [FieldOffset(0x68)] public uint AttachmentCount;
    [FieldOffset(0x70)] public unsafe AttachmentEntry* Attachments;
}

[StructLayout(LayoutKind.Explicit, Size = 0x68)]
internal struct AttachmentEntry
{
    [FieldOffset(0x02)] public ushort BoneIdx;
}

internal enum AttachType : uint
{
    None = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    CharacterBase = 3, // CharacterBase*
    Skeleton = 4, // Skeleton*
}
