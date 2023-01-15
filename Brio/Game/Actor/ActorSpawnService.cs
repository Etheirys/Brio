using Brio.Utils;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;

namespace Brio.Game.Actor;

public class ActorSpawnService : IDisposable
{
    private ClientObjectManager _clientObjectManager;
    private List<ushort> CreatedIndexes = new List<ushort>();

    public bool CanSpawn => Brio.GPoseService.IsInGPose && _clientObjectManager.CalculateNextIndex() != 0xffffffff;

    public ActorSpawnService()
    {
        _clientObjectManager = new ClientObjectManager();

        Brio.GPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
        Dalamud.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
    }

    private void ClientState_TerritoryChanged(object? sender, ushort e)
    {
        CreatedIndexes.Clear();
    }

    private void GPoseService_OnGPoseStateChange(bool isInGpose)
    {
        if(!isInGpose)
        {
            DestroyAllCreated();
        }
    }

    public unsafe ushort? Spawn()
    {
        var localPlayer = Dalamud.ClientState.LocalPlayer;
        if (localPlayer == null)
            return null;

        Character* originalPlayer = (Character*)localPlayer.Address;
        if(originalPlayer == null) return null;

        uint idCheck = _clientObjectManager.CreateBattleCharacter();
        if (idCheck == 0xffffffff) return null;
        ushort newId = (ushort)idCheck;

        Character* newPlayer = (Character*) _clientObjectManager.GetObjectByIndex(newId);
        if (newPlayer == null) return null;

        newPlayer->CopyFromCharacter(originalPlayer, 0); 
        *((sbyte*)newPlayer + 0x95) &= ~2; // Disable selection just incase this somehow leaks out of GPose
        newPlayer->GameObject.Position= originalPlayer->GameObject.Position;
        newPlayer->GameObject.SetName($"Brio {newId}");

        newPlayer->GameObject.EnableDraw();

        CreatedIndexes.Add(newId);

        return newId;
    }

    public unsafe void DestroyAllCreated()
    {
        foreach(var idx in CreatedIndexes)
        {
            _clientObjectManager.DeleteObjectByIndex(idx, 0);
        }
        CreatedIndexes.Clear();
    }

    public unsafe void DestroyAll()
    {
        DestroyAllCreated();

        var gposeObjects = Brio.GPoseService.GPoseObjects;
        foreach(var obj in gposeObjects)
        {
            var idx = _clientObjectManager.GetIndexByObject(new IntPtr(obj.Address));
            if (idx != 0xFFFFFFFF)
                _clientObjectManager.DeleteObjectByIndex((ushort)idx, 0);
        }
    }

    public unsafe void DestroyObject(GameObject* go)
    {
        var idx = _clientObjectManager.GetIndexByObject(new IntPtr(go));
        if(idx != 0xFFFFFFFF)
            _clientObjectManager.DeleteObjectByIndex((ushort)idx, 0);
    }

    public void Dispose()
    {
        DestroyAllCreated();

        Brio.GPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
    }
}
