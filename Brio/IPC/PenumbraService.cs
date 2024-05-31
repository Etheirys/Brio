using Brio.Config;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
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

    private readonly ApiVersion apiVersion;
    private readonly GetCollections getCollections;
    private readonly SetCollectionForObject setCollectionForObject;
    private readonly GetCollectionForObject getCollectionForObject;

    public PenumbraService(DalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _penumbraInitializedSubscriber = Penumbra.Api.IpcSubscribers.Initialized.Subscriber(pluginInterface, RefreshPenumbraStatus);
        _penumbraDisposedSubscriber = Penumbra.Api.IpcSubscribers.Disposed.Subscriber(pluginInterface, RefreshPenumbraStatus);

        _penumbraRedrawEvent = Penumbra.Api.IpcSubscribers.GameObjectRedrawn.Subscriber(pluginInterface, HandlePenumbraRedraw);

        getCollectionForObject = new Penumbra.Api.IpcSubscribers.GetCollectionForObject(_pluginInterface);
        setCollectionForObject = new Penumbra.Api.IpcSubscribers.SetCollectionForObject(_pluginInterface);
        getCollections = new Penumbra.Api.IpcSubscribers.GetCollections(_pluginInterface);
        apiVersion = new Penumbra.Api.IpcSubscribers.ApiVersion(_pluginInterface);

        _configurationService.OnConfigurationChanged += RefreshPenumbraStatus;

        RefreshPenumbraStatus();
    }

    public void RefreshPenumbraStatus()
    {
        if(_configurationService.Configuration.IPC.AllowPenumbraIntegration)
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
        var (_, _, collection) = getCollectionForObject.Invoke(gameObject.ObjectIndex);
        return collection.Name;
    }

    public Guid SetCollectionForObject(GameObject gameObject, Guid collectionName)
    {
        Brio.Log.Debug($"Setting GameObject {gameObject.ObjectIndex} collection to {collectionName}");
        var (_, oldCollection) = setCollectionForObject.Invoke(gameObject.ObjectIndex, collectionName, true, true);
        return oldCollection!.Value.Id; // TODO Fix null reference
    }

    public Dictionary<Guid, string> GetCollections()
    {
        return getCollections.Invoke();
    }

    private bool ConnectToPenumbra()
    {
        try
        {
            bool penumInstalled = _pluginInterface.InstalledPlugins.Any(x => x.Name == "Penumbra" && x.IsLoaded == true);
            if(!penumInstalled)
            {
                Brio.Log.Debug("Penumbra not present");
                return false;
            }

            var (major, minor) = apiVersion.Invoke();
            if(major != PenumbraApiMajor || minor < PenumbraApiMinor)
            {
                Brio.Log.Debug("Penumbra API mismatch");
                return false;
            }

            Brio.Log.Debug("Penumbra integration initialized");

            return true;
        }
        catch(Exception ex)
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
