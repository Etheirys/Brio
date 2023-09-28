using Brio.Core;
using Brio.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Brio.Game.Actor;

public class ActorService : ServiceBase<ActorService>
{
    public const int GPoseActorCount = 39;
    private const int GPoseFirstActor = 201;

    public delegate void ActorAction(GameObject gameObject);
    public event ActorAction? OnActorDestructing;

    private delegate void DestroyGameActorDelegate(IntPtr addr);
    private Hook<DestroyGameActorDelegate> _destroyGameActorHook = null!;

    public ReadOnlyCollection<GameObject> GPoseActors => new(_gposeActors);

    private List<GameObject> _gposeActors = new();

    public ActorService()
    {
        var destroyAddress = Dalamud.SigScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8D 05");
        _destroyGameActorHook = Dalamud.GameInteropProvider.HookFromAddress<DestroyGameActorDelegate>(destroyAddress, ActorDestructorDetour);
        _destroyGameActorHook.Enable();
    }

    public override void Start()
    {
        UpdateGPoseTable();
        base.Start();
    }

    public override void Tick(float delta)
    {
        UpdateGPoseTable();
    }

    private void ActorDestructorDetour(IntPtr addr)
    {
        var ago = Dalamud.ObjectTable.CreateObjectReference(addr);
        if(ago != null)
            OnActorDestructing?.Invoke(ago);

        _destroyGameActorHook.Original.Invoke(addr);
    }

    public void UpdateGPoseTable()
    {
        _gposeActors.Clear();
        for(int i = GPoseFirstActor; i < GPoseFirstActor + GPoseActorCount; ++i)
        {
            var go = Dalamud.ObjectTable[i];
            if(go != null)
            {
                _gposeActors.Add(go);
            }
        }
    }

    public static bool IsGPoseActor(int index) => index >= GPoseFirstActor && index < GPoseFirstActor + GPoseActorCount;
    public unsafe static bool IsGPoseActor(GameObject gameObject) => IsGPoseActor(gameObject.AsNative()->ObjectIndex);

    public override void Dispose()
    {
        _gposeActors.Clear();
        _destroyGameActorHook.Dispose();
    }
}
