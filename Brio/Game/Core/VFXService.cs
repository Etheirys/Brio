using Brio.Game.Actor.Extensions;
using Brio.Game.VFX.Intertop;
using Brio.Services;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;

using NativeGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.Game.Core;

public unsafe class VFXService : MediatorSubscriberBase
{
    public readonly IFramework _framework;

    private delegate* unmanaged<string, NativeGameObject*, NativeGameObject*, float, byte, ushort, byte, VfxData*> _createActorVfx;
    private delegate* unmanaged<IntPtr, float, uint, VfxObject*> _vxfRunStatic;
 
    public VFXService(ISigScanner scanner, IFramework framework, Mediator mediator) : base(mediator)
    {
        _framework = framework;

        var vfxCreateAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 ?? 0F B6 57 ?? 48 8B C8 C0 EA");
        _createActorVfx = (delegate* unmanaged<string, NativeGameObject*, NativeGameObject*, float, byte, ushort, byte, VfxData*>)vfxCreateAddress;
    
        var vfxRunAddress = scanner.ScanText("E8 ?? ?? ?? ?? B0 02 EB 02");
        _vxfRunStatic = (delegate* unmanaged<IntPtr, float, uint, VfxObject*>)vfxRunAddress;
    }

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

    public void RunStaticVFX(VfxObject* vfxInstance)
    {
        if(vfxInstance is not null)
        {
            _vxfRunStatic((IntPtr)vfxInstance, 0f, 0xFFFFFFFF);
        }
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
        base.Dispose();

    }
}
