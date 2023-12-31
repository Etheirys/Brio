using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Brio.Config;
using Penumbra.Api;
using Penumbra.Api.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.IPC;

internal class PenumbraService : IDisposable
{
    public bool IsPenumbraAvailable { get; private set; } = false;

    private readonly EventSubscriber _penumbraInitializedSubscriber;
    private readonly EventSubscriber _penumbraDisposedSubscriber;
    private readonly EventSubscriber<nint, int> _penumbraRedrawEvent;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ConfigurationService _configurationService;

    public delegate void PenumbraRedrawEvent(int gameObjectId);
    public event PenumbraRedrawEvent? OnPenumbraRedraw;

    private const int PenumbraApiMajor = 4;
    private const int PenumbraApiMinor = 22;

    public PenumbraService(DalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _penumbraInitializedSubscriber = Ipc.Initialized.Subscriber(pluginInterface, RefreshPenumbraStatus);
        _penumbraDisposedSubscriber = Ipc.Disposed.Subscriber(pluginInterface, RefreshPenumbraStatus);

        _penumbraRedrawEvent = Ipc.GameObjectRedrawn.Subscriber(pluginInterface, HandlePenumbraRedraw);

        _configurationService.OnConfigurationChanged += RefreshPenumbraStatus;



        RefreshPenumbraStatus();
    }

    public void RefreshPenumbraStatus()
    {
        if (_configurationService.Configuration.IPC.AllowPenumbraIntegration)
        {
            IsPenumbraAvailable = ConnectToPenumbra();
        }
        else
        {
            IsPenumbraAvailable = false;
        }
    }

    public string GetCollectionForObject(GameObject gameObject)
    {
        var (_, _, collection) = Ipc.GetCollectionForObject.Subscriber(_pluginInterface).Invoke(gameObject.ObjectIndex);
        return collection;
    }

    public string SetCollectionForObject(GameObject gameObject, string collectionName)
    {
        Brio.Log.Debug($"Setting gameobject {gameObject.ObjectIndex} collection to {collectionName}");
        var (_, oldCollection) = Ipc.SetCollectionForObject.Subscriber(_pluginInterface).Invoke(gameObject.ObjectIndex, collectionName, true, true);
        return oldCollection;
    }

    public IEnumerable<string> GetCollections()
    {
        return Ipc.GetCollections.Subscriber(_pluginInterface).Invoke();
    }

    private bool ConnectToPenumbra()
    {
        try
        {
            bool penumInstalled = _pluginInterface.InstalledPlugins.Count(x => x.Name == "Penumbra") > 0;
            if (!penumInstalled)
            {
                Brio.Log.Debug("Penumbra not present");
                return false;
            }

            var (major, minor) = Ipc.ApiVersions.Subscriber(_pluginInterface).Invoke();
            if (major != PenumbraApiMajor || minor < PenumbraApiMinor)
            {
                Brio.Log.Debug("Penumbra API mismatch");
                return false;
            }

            Brio.Log.Debug("Penumbra integration initialized");

            return true;
        }
        catch (Exception ex)
        {
            Brio.Log.Debug(ex, "Penumbra initialize error");
            return false;
        }
    }
    private void HandlePenumbraRedraw(nint arg1, int arg2)
    {
        Brio.Log.Debug("Penumbra redraw event received.");
        OnPenumbraRedraw?.Invoke(arg2);
    }

    public void Dispose()
    {
        _configurationService.OnConfigurationChanged -= RefreshPenumbraStatus;
        _penumbraInitializedSubscriber.Dispose();
        _penumbraDisposedSubscriber.Dispose();
    }
}
