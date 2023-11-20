using Brio.Core;
using Brio.Game.Actor.Extensions;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Utils;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

namespace Brio.Game.Actor;

public class ActorSpawnService : ServiceBase<ActorSpawnService>
{
    public bool CanSpawn => GPoseService.Instance.IsInGPose;


    private List<ushort> _createdIndexes = new();

    public override void Start()
    {
        GPoseService.Instance.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
        ActorService.Instance.OnActorDestructing += ActorService_OnActorDestructing;
        Dalamud.ClientState.TerritoryChanged += ClientState_TerritoryChanged;

        base.Start();
    }

    private void ClientState_TerritoryChanged(ushort e)
    {
        _createdIndexes.Clear();
    }

    private void GPoseService_OnGPoseStateChange(GPoseState state)
    {
        if(state == GPoseState.Outside)
        {
            DestroyAllCreated();
        }
    }

    private unsafe void ActorService_OnActorDestructing(DalamudGameObject gameObject)
    {
        if(ActorService.IsGPoseActor(gameObject))
        {
            var com = ClientObjectManager.Instance();
            var idx = com->GetIndexByObject(gameObject.AsNative());
            if(idx < ushort.MaxValue)
                _createdIndexes.Remove((ushort)idx);
        }
    }

    public unsafe ushort? Spawn(SpawnOptions options = SpawnOptions.None)
    {
        var localPlayer = Dalamud.ClientState.LocalPlayer;
        if(localPlayer == null)
            return null;

        var com = ClientObjectManager.Instance();

        Character* originalPlayer = (Character*)localPlayer.AsNative();
        if(originalPlayer == null) return null;

        bool reserveCompanionSlot = options.HasFlag(SpawnOptions.ReserveCompanionSlot);

        uint idCheck = com->CreateBattleCharacter(param: (byte)(reserveCompanionSlot ? 1 : 0));
        if(idCheck == 0xffffffff) return null;
        ushort newId = (ushort)idCheck;

        Character* newPlayer = (Character*)com->GetObjectByIndex(newId);
        if(newPlayer == null) return null;

        var gposeController = &EventFramework.Instance()->EventSceneModule.EventGPoseController;
        gposeController->AddCharacterToGPose(newPlayer); // This is safe even if the list is full. The game will also cleanup for us.

        newPlayer->CharacterSetup.CopyFromCharacter(originalPlayer, CharacterSetup.CopyFlags.None); // We copy the Player as the created actor is just blank

        newPlayer->GameObject.Position = originalPlayer->GameObject.Position;
        newPlayer->GameObject.DefaultPosition = originalPlayer->GameObject.Position;
        newPlayer->GameObject.Rotation = originalPlayer->GameObject.Rotation;
        newPlayer->GameObject.DefaultRotation = originalPlayer->GameObject.Rotation;

        newPlayer->GameObject.SetName(((int)newId).ToCharacterName());

        newPlayer->GameObject.DisableDraw();
        newPlayer->CharacterSetup.CopyFromCharacter(newPlayer, CharacterSetup.CopyFlags.None); // Some tools get confused (Like Penumbra) unless we copy onto ourselves after name change
        newPlayer->GameObject.EnableDraw();

        _createdIndexes.Add(newId);

        if(options.HasFlag(SpawnOptions.ApplyModelPosition))
        {
            Dalamud.Framework.RunUntilSatisfied(() => newPlayer->GameObject.RenderFlags == 0,
                (_) =>
                {
                    newPlayer->GameObject.DrawObject->Object.Position = newPlayer->GameObject.Position;
                    return true;
                }, 50, 3, true);
        }

        return newPlayer->GameObject.ObjectIndex;
    }

    public unsafe void DestroyAllCreated()
    {
        var indexes = _createdIndexes.ToList();
        var com = ClientObjectManager.Instance();
        foreach(var idx in indexes)
        {
            var goa = com->GetObjectByIndex(idx);
            if(goa == null) continue;
            var ago = Dalamud.ObjectTable.CreateObjectReference((IntPtr)goa);
            if(ago == null) continue;
            DestroyObject(ago);
        }
        _createdIndexes.Clear();
    }

    public unsafe void DestroyAll()
    {
        var gposeObjects = ActorService.Instance.GPoseActors.ToList();
        foreach(var obj in gposeObjects)
        {
            DestroyObject(obj);
        }
    }

    public unsafe bool DestroyObject(DalamudGameObject go)
    {
        var com = ClientObjectManager.Instance();
        var native = go.AsNative();
        var idx = com->GetIndexByObject(native);
        if(idx != 0xFFFFFFFF)
        {
            com->DeleteObjectByIndex((ushort)idx, 0);
            ActorService.Instance.UpdateGPoseTable();
            return true;
        }

        return false;
    }

    public override void Stop()
    {
        GPoseService.Instance.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
        ActorService.Instance.OnActorDestructing -= ActorService_OnActorDestructing;
        Dalamud.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
    }

    public override void Dispose()
    {
        DestroyAllCreated();
        GPoseService.Instance.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
    }
}

[Flags]
public enum SpawnOptions
{
    None = 0,
    ApplyModelPosition = 1,
    ReserveCompanionSlot = 2,
}
