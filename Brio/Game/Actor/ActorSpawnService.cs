using Brio.Core;
using Brio.Game.GPose;
using Brio.Utils;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Collections.Generic;
using System.Linq;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

namespace Brio.Game.Actor;

public class ActorSpawnService : ServiceBase<ActorSpawnService>
{
    public bool CanSpawn => GPoseService.Instance.IsInGPose;

    private ClientObjectManager _clientObjectManager;
    private List<ushort> _createdIndexes = new();

    public ActorSpawnService()
    {
        _clientObjectManager = new ClientObjectManager();
    }

    public override void Start()
    {
        GPoseService.Instance.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
        ActorService.Instance.OnActorDestructing += ActorService_OnActorDestructing;
        Dalamud.ClientState.TerritoryChanged += ClientState_TerritoryChanged;

        base.Start();
    }

    private void ClientState_TerritoryChanged(object? sender, ushort e)
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
        if(ActorService.Instance.IsGPoseActor(gameObject))
        {
            var idx = _clientObjectManager.GetIndexByObject(gameObject.Address);
            if (idx < ushort.MaxValue)
                _createdIndexes.Remove((ushort)idx);
        }
    }

    public unsafe ushort? Spawn()
    {
        var localPlayer = Dalamud.ClientState.LocalPlayer;
        if (localPlayer == null)
            return null;

        Character* originalPlayer = (Character*)localPlayer.AsNative();
        if(originalPlayer == null) return null;

        uint idCheck = _clientObjectManager.CreateBattleCharacter();
        if (idCheck == 0xffffffff) return null;
        ushort newId = (ushort)idCheck;

        Character* newPlayer = (Character*) _clientObjectManager.GetObjectByIndex(newId);
        if (newPlayer == null) return null;

        newPlayer->CopyFromCharacter(originalPlayer, 0); // We copy the Player as the created actor is just blank

        *((sbyte*)newPlayer + 0x95) &= ~2; // Disable selection just incase this somehow leaks out of GPose
        newPlayer->GameObject.Position= originalPlayer->GameObject.Position;
        newPlayer->GameObject.SetName(((int)newId).ToCharacterName());
        newPlayer->GameObject.ObjectKind = 1;

        newPlayer->GameObject.DisableDraw();

        newPlayer->CopyFromCharacter(newPlayer, 0); // Some tools get confused (Like Penumbra) unless we copy onto ourselves after name change

        newPlayer->GameObject.EnableDraw();

        _createdIndexes.Add(newId);

        return newId;
    }

    public unsafe void DestroyAllCreated()
    {
        var indexes = _createdIndexes.ToList();
        foreach (var idx in indexes)
        {
            var goa = _clientObjectManager.GetObjectByIndex(idx);
            if (goa == 0) continue;
            var ago = Dalamud.ObjectTable.CreateObjectReference(goa);
            if (ago == null) continue;
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

    public unsafe void DestroyObject(DalamudGameObject go)
    {
        var native = go.AsNative();
        var idx = _clientObjectManager.GetIndexByObject((nint)native);
        if (idx != 0xFFFFFFFF)
        {
            _clientObjectManager.DeleteObjectByIndex((ushort)idx, 0);
            ActorService.Instance.UpdateGPoseTable();
        }
    }

    public override void Dispose()
    {
        DestroyAllCreated();
        GPoseService.Instance.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
    }
}
