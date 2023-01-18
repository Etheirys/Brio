using Brio.Utils;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using System;
using System.Collections.Generic;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.IPC;

public class PenumbraIPC : IDisposable
{
    public bool IsPenumbraEnabled { get; private set; } = false;
    public bool CanApplyCollection { get; private set; } = true;
    public List<string> Collections { get; } = new();

    private EventSubscriber _penumbraInitializedSubscriber;
    private EventSubscriber _penumbraDisposedSubscriber;

    public PenumbraIPC()
    {
        _penumbraInitializedSubscriber = Ipc.Initialized.Subscriber(Dalamud.PluginInterface, RefreshPenumbraStatus);
        _penumbraDisposedSubscriber = Ipc.Disposed.Subscriber(Dalamud.PluginInterface, RefreshPenumbraStatus);

        RefreshPenumbraStatus();

        Brio.GPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }

    public void RefreshPenumbraStatus() 
    {
        IsPenumbraEnabled = false;

        if (!Brio.Configuration.AllowPenumbraIntegration)
            return;

        try
        {
            bool penumInstalled = Dalamud.PluginInterface.PluginNames.Contains("Penumbra");
            if (!penumInstalled)
            {
                PluginLog.Information("Penumbra not present");
                return;
            }

            var (major, minor) = Ipc.ApiVersions.Subscriber(Dalamud.PluginInterface).Invoke();
            if (major != 4 || minor < 18)
            {
                PluginLog.Information("Penumbra API mismatch");
                return;
            }

            UpdateCollections();

            IsPenumbraEnabled = true;
            PluginLog.Information("Penumbra integration initialized");
        }
        catch (Exception ex)
        {
            PluginLog.Information(ex, "Penumbra initialize error");
        }
    }

    public unsafe void RedrawActorWithCollection(GameObject gameObject, string collectionName)
    {
        if (!IsPenumbraEnabled)
        {
            PluginLog.Warning("Tried to Penumbra collection redraw when Penumbra is disabled");
            return;
        }

        if(gameObject.ObjectKind != ObjectKind.Player)
        {
            PluginLog.Warning("Only players can be redrawn with a collection");
            return;
        }

        var chara = (Character*)gameObject.AsNative();

        try
        {
            CanApplyCollection = false;

            var index = chara->GameObject.ObjectIndex;

            // Set the new collection
            var (_, oldName) = Ipc.SetCollectionForObject.Subscriber(Dalamud.PluginInterface).Invoke(index, collectionName, true, true);

            // Redraw
            Brio.ActorRedrawService.Redraw(gameObject, Game.Actor.RedrawType.Penumbra, true);

            // Wait until the redraw is done, or 50 frames at most
            Brio.FrameworkUtils.RunUntilSatisfied(
                () => chara->GameObject.RenderFlags == 0,
                (_) =>{
                    Ipc.SetCollectionForObject.Subscriber(Dalamud.PluginInterface).Invoke(index, oldName, true, true);
                    CanApplyCollection = true;
                }, 
                50
           );
        }
        catch(Exception ex)
        {
            CanApplyCollection = true;
            PluginLog.Warning(ex, "Error during Penumbra collection redraw");
            Dalamud.ToastGui.ShowError("Unable to apply Penumbra collection.");
        }
    }

    public void RawPenumbraRefresh(int objectId)
    {
        Ipc.RedrawObjectByIndex.Subscriber(Dalamud.PluginInterface).Invoke(objectId, RedrawType.Redraw);
    }

    private void UpdateCollections()
    {
        var collections = Ipc.GetCollections.Subscriber(Dalamud.PluginInterface).Invoke();
        var defaultCollection = Ipc.GetDefaultCollectionName.Subscriber(Dalamud.PluginInterface).Invoke();
        Collections.Clear();
        Collections.Add(defaultCollection);
        Collections.Add("None");
        Collections.AddRange(collections);
    }

    private void GPoseService_OnGPoseStateChange(bool isInGpose)
    {
        if (isInGpose)
            RefreshPenumbraStatus();
    }

    public void Dispose()
    {
        Brio.GPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
        _penumbraDisposedSubscriber.Dispose();
        _penumbraInitializedSubscriber.Dispose();
        IsPenumbraEnabled = false;
    }
}
