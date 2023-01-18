using Brio.Game.GPose;
using Brio.Utils;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Penumbra.Api;
using System;
using System.Collections.Generic;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Game.Actor;

public class PenumbraCollectionService : IDisposable
{
    public bool CanApplyCollection(GameObject gameObject) => Brio.ActorRedrawService.CanRedraw(gameObject);
    public List<string> Collections { get; } = new();

    public Dictionary<int, string> _appliedCollections = new();

    public PenumbraCollectionService()
    {
        Brio.PenumbraIPC.OnPenumbraStateChange += PenumbraIPC_OnPenumbraStateChange;
        Brio.ActorService.OnActorDestructing += ActorService_OnActorDestructing;
        RefreshCollections();
    }

    public unsafe void RedrawActorWithCollection(GameObject gameObject, string collectionName)
    {
        if (!Brio.PenumbraIPC.IsPenumbraEnabled)
        {
            PluginLog.Warning("Tried to Penumbra collection redraw when Penumbra is disabled");
            return;
        }

        var chara = (Character*)gameObject.AsNative();

        try
        {
            var index = chara->GameObject.ObjectIndex;

            // Set the new collection
            var (_, oldName) = Ipc.SetCollectionForObject.Subscriber(Dalamud.PluginInterface).Invoke(index, collectionName, true, true);

            // Redraw
            Brio.ActorRedrawService.Redraw(gameObject, RedrawType.Penumbra, true);

            if (!_appliedCollections.ContainsKey(index))
                _appliedCollections[index] = oldName;

        }
        catch (Exception ex)
        {
            PluginLog.Warning(ex, "Error during Penumbra collection redraw");
            Dalamud.ToastGui.ShowError("Unable to apply Penumbra collection.");
        }
    }

    public void RefreshCollections()
    {
        Collections.Clear();

        if (!Brio.PenumbraIPC.IsPenumbraEnabled)
        {
            Brio.PenumbraIPC.RefreshPenumbraStatus();
            return;
        }

        var collections = Ipc.GetCollections.Subscriber(Dalamud.PluginInterface).Invoke();
        var defaultCollection = Ipc.GetDefaultCollectionName.Subscriber(Dalamud.PluginInterface).Invoke();
        Collections.Add(defaultCollection);
        Collections.Add("None");
        Collections.AddRange(collections);
    }

    private void CleanupOverrides()
    {
        foreach (var (index, name) in _appliedCollections)
        {
            Ipc.SetCollectionForObject.Subscriber(Dalamud.PluginInterface).Invoke(index, name, true, true);
        }
        _appliedCollections.Clear();
    }

    private void CleanupOverride(int index)
    {
        if (_appliedCollections.ContainsKey(index))
        {
            Ipc.SetCollectionForObject.Subscriber(Dalamud.PluginInterface).Invoke(index, _appliedCollections[index], true, true);
            _appliedCollections.Remove(index);
        }
    }

    private void PenumbraIPC_OnPenumbraStateChange(bool isActive)
    {
        if (isActive)
        {
            RefreshCollections();
        }
        else
        {
            try
            {
                CleanupOverrides();
            }
            catch
            {
                PluginLog.Warning("Failed to cleanup collections on Penumbra disable");
            }
        }
    }

    private unsafe void ActorService_OnActorDestructing(GameObject gameObject)
    {
        CleanupOverride(gameObject.AsNative()->ObjectIndex);
    }

    public void Dispose()
    {
        CleanupOverrides();
        Brio.PenumbraIPC.OnPenumbraStateChange -= PenumbraIPC_OnPenumbraStateChange;
    }
}
