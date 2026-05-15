using FFXIVClientStructs.FFXIV.Client.Game.Event;
using System.Runtime.InteropServices;

namespace Brio.Game.World.Interop;

[StructLayout(LayoutKind.Explicit)]
public struct BrioEventGPoseController
{
    [FieldOffset(0x000)] public EventGPoseController EventGPoseController;

    [FieldOffset(0x0E0)] public unsafe fixed ulong Lights[3];

    public unsafe BrioLight* GetLight(uint index) => (BrioLight*)Lights[index];
}
