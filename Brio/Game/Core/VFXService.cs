using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Brio.Game.VFX.Intertop;
using Brio.Services;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NativeGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Game.Core;

public unsafe class VFXService : MediatorSubscriberBase
{
    public readonly IFramework _framework;
    public readonly GPoseService _gPoseService;

    private delegate* unmanaged<string, NativeGameObject*, NativeGameObject*, float, byte, ushort, byte, VfxData*> _createActorVfx;

    private delegate* unmanaged<VfxResourceInstance*, void> _vfxPause;
    private delegate* unmanaged<VfxResourceInstance*, byte> _vfxIsActive;
    private delegate* unmanaged<VfxResourceInstance*, float, void> _vfxSetSpeed;

    private delegate* unmanaged<IntPtr, float, uint, VfxObject*> _vxfPlayStatic;
    private delegate* unmanaged<VfxData*, void> _vxfPauseActor;
    private delegate* unmanaged<VfxData*, void> _vxfResumeActor;

    private delegate* unmanaged<VfxObject*, bool> _vfxIsActiveStatic;

    public delegate nint VfxResourceLoadDelegate(void* job, nint unk1, byte* filePath, byte* avfxData, uint dataSize, ResourceHandle* resourceHandle, uint unk2);
    public Hook<VfxResourceLoadDelegate> _vfxResourceLoadDetour;

    private readonly Lock _handledVfxLock = new();
    private readonly HashSet<ulong> HandledVFX = []; 

    public VFXService(ISigScanner scanner, GPoseService gPoseService, IGameInteropProvider hooking, IFramework framework, Mediator mediator) : base(mediator)
    {
        _framework = framework;
        _gPoseService = gPoseService;

        // TODO(KEN) make sure the sigs here are properly wild carded, as some of them aren't atm

        var vfxCreateAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 ?? 0F B6 57 ?? 48 8B C8 C0 EA");
        _createActorVfx = (delegate* unmanaged<string, NativeGameObject*, NativeGameObject*, float, byte, ushort, byte, VfxData*>)vfxCreateAddress;

        var vfxRunAddress = scanner.ScanText("E8 ?? ?? ?? ?? B0 02 EB 02");
        _vxfPlayStatic = (delegate* unmanaged<IntPtr, float, uint, VfxObject*>)vfxRunAddress;

        // E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 0F 2E C7 7A ?? 74 ?? 0F 28 CF 48 8B CB E8 - (void) (VfxResourceInstance*) | VfxResourceInstance::Pause
        var vfxResourceInstanceStopAddr = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B CB E8 ?? ?? ?? ?? 0F 2E C7 7A ?? 74 ?? 0F 28 CF 48 8B CB E8");
        _vfxPause = (delegate* unmanaged<VfxResourceInstance*, void>)vfxResourceInstanceStopAddr;

        // 48 89 5C 24 10 48 89 74 24 18 57 48 83 EC 20 48 8B 59 60 - (byte [bool]) (VfxResourceInstance*) | VfxResourceInstance::IsActive
        var vfxResourceInstanceIsActiveAddr = scanner.ScanText("48 89 5C 24 10 48 89 74 24 18 57 48 83 EC 20 48 8B 59 60");
        _vfxIsActive = (delegate* unmanaged<VfxResourceInstance*, byte>)vfxResourceInstanceIsActiveAddr;

        // Static vfx is Active
        var _vfxIsActiveStaticaddr = scanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 ?? 48 8B 4B 28 48 8B 01 FF 50 68 48 8B C8 0F 57 C9 E8");
        _vfxIsActiveStatic = (delegate* unmanaged<VfxObject*, bool>)_vfxIsActiveStaticaddr;

        var _vxfPauseStaticaddr = scanner.ScanText("48 83 EC 38 48 8B 81 C8 01 00 00 4C 8D 41 70 48 8B 0D ?? ?? ?? ?? 41 B1 01 C7 44 24 20 04 00 00 00");
        _vxfPauseActor = (delegate* unmanaged<VfxData*, void>)_vxfPauseStaticaddr;

        var _vxfResumeStaticaddr = scanner.ScanText("48 83 B9 C8 01 00 00 00 74 ?? 48 83 C1 70");
        _vxfResumeActor = (delegate* unmanaged<VfxData*, void>)_vxfResumeStaticaddr;

        // SPEED!!!
        var _vfxSetSpeedaddr = scanner.ScanText("48 89 5C 24 08 57 48 83 EC 30 48 8B 59 60");
        _vfxSetSpeed = (delegate* unmanaged<VfxResourceInstance*, float, void>)_vfxSetSpeedaddr;

        var passVFXAddr = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 85 C0 48 8B 6C 24");
        _vfxResourceLoadDetour = hooking.HookFromAddress<VfxResourceLoadDelegate>(passVFXAddr, VfxResourceLoadDetour);
        _vfxResourceLoadDetour.Enable();
    }

    //

    private nint VfxResourceLoadDetour(void* aJob, nint unk1, byte* filePath, byte* avfxData, uint dataSize, ResourceHandle* resourceHandle, uint unk2)
    {
        if(_gPoseService.IsGPosing == false) // TODO we should really disabled this Detour once we are out of Gpose, not just this 
        {
            return _vfxResourceLoadDetour.Original(aJob, unk1, filePath, avfxData, dataSize, resourceHandle, unk2)!;
        }

        var temp = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(filePath);
        var path = Encoding.UTF8.GetString(temp);

        if(IsHandledVFX(path))
        {
            Brio.Log.Verbose($"HandledVFX contains :: {path}");
            try
            {
                UnbindAllTimelineItems(avfxData, (int)dataSize);
            }
            catch(Exception ex)
            {
                Brio.Log.Error(ex, $"avfx unbind failed for {path}");
            }
        }
        else
            Brio.Log.Verbose($"HandledVFX does NOT contain :: {path}");

        return _vfxResourceLoadDetour.Original(aJob, unk1, filePath, avfxData, dataSize, resourceHandle, unk2)!;
    }

    //

    private static uint Tag(string s) =>
        (uint)(((byte)s[0] << 24) | ((byte)s[1] << 16) | ((byte)s[2] << 8) | (byte)s[3]);
    private static int ChunkLen(byte* data, int payloadStart) =>
        (int)*(uint*)(data + payloadStart - 4);
    private static int FindChunk(byte* data, int start, int len, uint tag)
    {
        int consumed = 0;

        while(consumed + 8 <= len)
        {
            uint t = *(uint*)(data + start + consumed);
            uint pl = *(uint*)(data + start + consumed + 4);
            if(t == tag) return start + consumed + 8;
            consumed += 8 + (int)((pl + 3u) & ~3u);
        }

        return -1;
    }

    // Null every BdNo in every Item of every TmLn. Returns how many were patched.
    private static int UnbindAllTimelineItems(byte* data, int len)
    {
        int avfx = FindChunk(data, 0, len, Tag("AVFX"));
        if(avfx < 0) return 0;
        int avfxLen = ChunkLen(data, avfx);

        int patched = 0;
        int consumed = 0;
        while(consumed + 8 <= avfxLen)
        {
            int childStart = avfx + consumed;
            uint tag = *(uint*)(data + childStart);
            uint payLen = *(uint*)(data + childStart + 4);
            int payStart = childStart + 8;

            if(payStart + (int)payLen > avfx + avfxLen) break;

            if(tag == Tag("TmLn"))
                patched += UnbindTimeline(data, payStart, (int)payLen, len);

            consumed += 8 + (int)((payLen + 3u) & ~3u);
        }
        return patched;
    }
    private static unsafe int UnbindTimeline(byte* data, int tmlnStart, int tmlnLen, int totalLen)
    {
        int patched = 0;
        int consumed = 0;
        while(consumed + 8 <= tmlnLen)
        {
            int childStart = tmlnStart + consumed;
            uint tag = *(uint*)(data + childStart);
            uint payLen = *(uint*)(data + childStart + 4);
            int payStart = childStart + 8;

            if(payStart + (int)payLen > tmlnStart + tmlnLen) break;

            if(tag == Tag("Item"))
            {
                int bd = FindChunk(data, payStart, (int)payLen, Tag("BdNo"));
                if(bd >= 0 && bd + 4 <= totalLen)
                {
                    *(uint*)(data + bd) = 0xFFFFFFFF;
                    patched++;
                }
            }

            consumed += 8 + (int)((payLen + 3u) & ~3u);
        }
        return patched;
    }

    //

    public void AddHandledVFX(string path)
    {
        var hash = ObjectPath.HashPath(path);
        lock(_handledVfxLock)
        {
            HandledVFX.Add(hash);
        }
    }

    public void RemoveHandledVFX(string path)
    {
        var hash = ObjectPath.HashPath(path);
        lock(_handledVfxLock)
        {
            HandledVFX.Remove(hash); // Remove is a no-op if absent; the Contains check is redundant
        }
    }

    public bool IsHandledVFX(string path)
    {
        var hash = ObjectPath.HashPath(path);
        lock(_handledVfxLock)
        {
            return HandledVFX.Contains(hash);
        }
    }
    //

    public VfxData* CreateActorVFX(string vfxName, IGameObject actor, IGameObject? target = null)
    {
        if(string.IsNullOrEmpty(vfxName))
            return null;

        target ??= actor;

        return CreateActorVFX(vfxName, actor.Native(), target.Native());
    }
    public VfxData* CreateActorVFX(string vfxName, NativeGameObject* actor, NativeGameObject* target = null)
    {
        if(string.IsNullOrEmpty(vfxName))
            return null;

        if(target == null)
            target = actor;

        return _createActorVfx(vfxName, actor, target, -1, 0, 0, 0);
    }

    public void SetVFXSpeed(VfxResourceInstance* vfxResourceInstance, float speed)
    {
        if(vfxResourceInstance is not null)
        {
            _vfxSetSpeed(vfxResourceInstance, speed);
        }
    }
    public void PauseVFX(VfxResourceInstance* vfxResourceInstance)
    {
        if(vfxResourceInstance is not null)
        {
            _vfxPause(vfxResourceInstance);
        }
    }

    public void PlayStaticVFX(VfxObject* vfxInstance)
    {
        if(vfxInstance is not null)
        {
            _vxfPlayStatic((IntPtr)vfxInstance, 0f, 0xFFFFFFFF);
        }
    }
    public bool IsActiveStatic(VfxObject* vfxInstance)
    {
        if(vfxInstance is not null)
        {
            return _vfxIsActiveStatic(vfxInstance);
        }

        return false;
    }

    public void DestroyVFX(VfxData* vfxInstance)
    {
        if(vfxInstance is not null)
        {
            vfxInstance->Vtable->Dtor(vfxInstance, 1);
        }
    }

    public override unsafe void Dispose()
    {
        _vfxResourceLoadDetour?.Dispose();
        base.Dispose();
    }
}
