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
    public bool PenumbraUseLegacyApi { get; private set; } = false;

    private const int PenumbraApiMajor = 5;
    private const int PenumbraApiMinor = 0;

    private readonly DalamudPluginInterface _pluginInterface;
    private readonly ConfigurationService _configurationService;

    public delegate void PenumbraRedrawEvent(int gameObjectId);
    public event PenumbraRedrawEvent? OnPenumbraRedraw;

    private readonly EventSubscriber _penumbraInitializedSubscriber;
    private readonly EventSubscriber _penumbraDisposedSubscriber;
    private readonly EventSubscriber<nint, int> _penumbraRedrawEvent;

    private readonly ApiVersion _penumbraApiVersion;
    private readonly GetCollections _penumbraGetCollections;
    private readonly SetCollectionForObject _penumbraSetCollectionForObject;
    private readonly GetCollectionForObject _penumbraGetCollectionForObject;

    private Penumbra.Api.IpcSubscribers.Legacy.GetCollectionForObject? _pLegacyGetCollectionForObject;
    private Penumbra.Api.IpcSubscribers.Legacy.SetCollectionForObject? _pLegacySetCollectionForObject;
    private Penumbra.Api.IpcSubscribers.Legacy.GetCollections? _pLegacyGetCollections;

    public PenumbraService(DalamudPluginInterface pluginInterface, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;

        _penumbraInitializedSubscriber = Penumbra.Api.IpcSubscribers.Initialized.Subscriber(pluginInterface, RefreshPenumbraStatus);
        _penumbraDisposedSubscriber = Penumbra.Api.IpcSubscribers.Disposed.Subscriber(pluginInterface, RefreshPenumbraStatus);
        _penumbraRedrawEvent = Penumbra.Api.IpcSubscribers.GameObjectRedrawn.Subscriber(pluginInterface, HandlePenumbraRedraw);

        _penumbraGetCollectionForObject = new Penumbra.Api.IpcSubscribers.GetCollectionForObject(_pluginInterface);
        _penumbraSetCollectionForObject = new Penumbra.Api.IpcSubscribers.SetCollectionForObject(_pluginInterface);
        _penumbraGetCollections = new Penumbra.Api.IpcSubscribers.GetCollections(_pluginInterface);
        _penumbraApiVersion = new Penumbra.Api.IpcSubscribers.ApiVersion(_pluginInterface);

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

        bool ConnectToPenumbra()
        {
            try
            {
                bool penumbraInstalled = _pluginInterface.InstalledPlugins.Any(x => x.Name == "Penumbra" && x.IsLoaded == true);
                if(penumbraInstalled == false)
                {
                    Brio.Log.Debug("Penumbra not present");
                    return false;
                }

                try
                {
                    var (major, minor) = _penumbraApiVersion.Invoke();
                    if(major != PenumbraApiMajor || minor < PenumbraApiMinor)
                    {
                        Brio.Log.Warning($"Penumbra API mismatch!, found v{major}.{minor}");
                        return false;
                    }
                }
                catch
                {
                    PenumbraUseLegacyApi = true;
                    LoadLegacy();
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

        void LoadLegacy()
        {
            Brio.Log.Warning("Using Penumbra Legacy API!");

            _pLegacyGetCollectionForObject = new(_pluginInterface);
            _pLegacySetCollectionForObject = new(_pluginInterface);
            _pLegacyGetCollections = new(_pluginInterface);
        }
    }

    public string GetCollectionForObject(GameObject gameObject)
    {
        if(PenumbraUseLegacyApi)
        {
            var (_, _, name) = _pLegacyGetCollectionForObject!.Invoke(gameObject.ObjectIndex);
            return name;
        }

        var (_, _, collection) = _penumbraGetCollectionForObject.Invoke(gameObject.ObjectIndex);
        return collection.Name;
    }
   
    public string LegacySetCollectionForObject(GameObject gameObject, string collectionName)
    {
        Brio.Log.Debug($"Setting GameObject {gameObject.ObjectIndex} collection to {collectionName}");
        var (_, name) = _pLegacySetCollectionForObject!.Invoke(gameObject.ObjectIndex, collectionName.ToString(), true, true);
        return name;
    }
    public Guid SetCollectionForObject(GameObject gameObject, Guid collectionName)
    {
        Brio.Log.Debug($"Setting GameObject {gameObject.ObjectIndex} collection to {collectionName}");
        var (_, oldCollection) = _penumbraSetCollectionForObject.Invoke(gameObject.ObjectIndex, collectionName, true, true);
        return oldCollection!.Value.Id; // TODO Fix null reference
    }

    public IEnumerable<string> LegacyGetCollections()
    {
        return _pLegacyGetCollections!.Invoke();
    }
    public Dictionary<Guid, string> GetCollections()
    {
        return _penumbraGetCollections.Invoke();
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
