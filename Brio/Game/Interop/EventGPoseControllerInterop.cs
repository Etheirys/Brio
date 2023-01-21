using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using System;

namespace Brio.Game.Interop;

public class EventGPoseControllerInterop
{
    // TODO: All this should go back to FFXIV Client Structs
    // Track: https://github.com/aers/FFXIVClientStructs/pull/295

    private delegate uint AddToGPoseDelegate(IntPtr instance, IntPtr target, uint u1 = 0);
    [Signature("E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 BE 8C 00 00 00 02", ScanType = ScanType.Text)]
    private AddToGPoseDelegate _addToGPose = null!;
    public unsafe uint AddToGPose(IntPtr objectPtr) => _addToGPose.Invoke((nint)EventFramework.Instance() + 0x380, objectPtr, 0);

    private delegate uint RemoveFromGPoseDelegate(IntPtr instance, IntPtr target);
    [Signature("45 33 D2 4C 8D 81 38 2A 00 00 41 8B C2 4C 8B C9 49 3B 10 ?? ?? FF C0 49 83 C0 18 83 F8 1E ?? ?? ?? 8B", ScanType = ScanType.Text)]
    private RemoveFromGPoseDelegate _removeFromGPose = null!;
    public unsafe uint RemoveFromGPose(IntPtr objectPtr) => _removeFromGPose.Invoke((nint)EventFramework.Instance() + 0x380, objectPtr);

    public EventGPoseControllerInterop()
    {
        SignatureHelper.Initialise(this);
    }
}
