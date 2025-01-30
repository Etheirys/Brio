using Dalamud.Game;
using NativeGameObject =  FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using System;
using Dalamud.Game.ClientState.Objects.Types;
using Brio.Game.Actor.Extensions;

namespace Brio.Game.Actor;

internal unsafe class ActorVFXService : IDisposable
{

    private delegate* unmanaged<string, NativeGameObject*, NativeGameObject*, float, byte, ushort, byte, nint> _createActorVfx;

    private delegate* unmanaged<nint, void> _vfxDtor;


    public ActorVFXService(ISigScanner scanner)
    {
        var vfxCreateAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 ?? 0F B6 57 ?? 48 8B C8 C0 EA");
        _createActorVfx = (delegate* unmanaged<string, NativeGameObject*, NativeGameObject*, float, byte, ushort, byte, nint>)vfxCreateAddress;

        var vfxDetourAddress = scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC ?? 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 8B FA 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 ?? 48 8B 01 48 8B D3");
        _vfxDtor = (delegate* unmanaged<nint, void>)vfxDetourAddress;
    }

    public nint CreateActorVFX(string vfxName, IGameObject actor, IGameObject? target = null)
    {
        if(target == null)
            target = actor;

        return CreateActorVFX(vfxName, actor.Native(), target.Native());
    }

    public nint CreateActorVFX(string vfxName, NativeGameObject* actor, NativeGameObject* target = null)
    {
       if(target == null)
            target = actor;

       return _createActorVfx(vfxName, actor, target, -1, 0, 0, 0);
    }

    public void DestroyVFX(nint vfxInstance)
    {
        if(vfxInstance != 0)
            _vfxDtor(vfxInstance);
    }

    public unsafe void Dispose()
    {
    }
}
