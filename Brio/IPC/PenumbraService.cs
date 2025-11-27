using Brio.Config;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brio.IPC;

public class PenumbraService : BrioIPC
{
    public override string Name { get; } = "Penumbra";

    public override bool IsAvailable
        => PenumbraCheckStatus();

    public override bool AllowIntegration
        => _configurationService.Configuration.IPC.AllowPenumbraIntegration;

    public override int APIMajor => 5;
    public override int APIMinor => 10;

    public override (int Major, int Minor) GetAPIVersion()
        => _penumbraApiVersion.Invoke();

    public override IDalamudPluginInterface GetPluginInterface()
        => _pluginInterface;

    //
    //

    private string? _penumbraModDirectory;
    public string? ModDirectory
    {
        get => _penumbraModDirectory;
        private set
        {
            if(!string.Equals(_penumbraModDirectory, value, StringComparison.Ordinal))
            {
                _penumbraModDirectory = value;
            }
        }
    }

    //

    private readonly ConfigurationService _configurationService;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;

    //private readonly GetEnabledState _penumbraEnabled;
    private readonly ApiVersion _penumbraApiVersion;

    private readonly OpenMainWindow _penumbraOpenMainWindow;

    public delegate void PenumbraRedrawEvent(int gameObjectId);
    public event PenumbraRedrawEvent? OnPenumbraRedraw;

    public delegate void PenumbraResourceLoadedEvent(IntPtr ptr, string arg1, string arg2);
    public event PenumbraResourceLoadedEvent? OnPenumbraResourceLoaded;

    private readonly RedrawObject _penumbraRedraw;

    private readonly EventSubscriber<nint, int> _penumbraRedrawEvent;
    private readonly EventSubscriber _penumbraInitializedSubscriber;
    private readonly EventSubscriber _penumbraDisposedSubscriber;

    private readonly EventSubscriber<nint, string, string> _penumbraGameObjectResourcePathResolved;

    private readonly SetCollectionForObject _penumbraSetCollectionForObject;
    private readonly GetCollectionForObject _penumbraGetCollectionForObject;
    private readonly GetCollections _penumbraGetCollections;

    private readonly CreateTemporaryCollection _penumbraCreateNamedTemporaryCollection;
    private readonly AssignTemporaryCollection _penumbraAssignTemporaryCollection;
    private readonly DeleteTemporaryCollection _penumbraRemoveTemporaryCollection;


    private readonly AddTemporaryMod _penumbraAddTemporaryMod;
    private readonly RemoveTemporaryMod _penumbraRemoveTemporaryMod;

    private readonly GetModDirectory _penumbraResolveModDir;

    private readonly ResolvePlayerPathsAsync _penumbraResolvePaths;
    private readonly GetGameObjectResourcePaths _penumbraResourcePaths;
    private readonly GetPlayerMetaManipulations _penumbraGetMetaManipulations;
    //private readonly ConvertTextureFile _penumbraConvertTextureFile;

    public PenumbraService(IDalamudPluginInterface pluginInterface, IFramework framework, ConfigurationService configurationService)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;
        _framework = framework;

        _penumbraInitializedSubscriber = Initialized.Subscriber(_pluginInterface, OnConfigurationChanged);
        _penumbraDisposedSubscriber = Disposed.Subscriber(_pluginInterface, OnConfigurationChanged);
        _penumbraRedrawEvent = GameObjectRedrawn.Subscriber(_pluginInterface, HandlePenumbraRedraw);
        _penumbraGameObjectResourcePathResolved = GameObjectResourcePathResolved.Subscriber(_pluginInterface, ResourceLoaded);

        _penumbraGetCollectionForObject = new GetCollectionForObject(_pluginInterface);
        _penumbraSetCollectionForObject = new SetCollectionForObject(_pluginInterface);
        _penumbraGetCollections = new GetCollections(_pluginInterface);
        _penumbraOpenMainWindow = new OpenMainWindow(_pluginInterface);
        _penumbraApiVersion = new ApiVersion(_pluginInterface);

        _penumbraResolveModDir = new GetModDirectory(_pluginInterface);
        _penumbraRedraw = new RedrawObject(_pluginInterface);
        _penumbraRemoveTemporaryMod = new RemoveTemporaryMod(_pluginInterface);
        _penumbraAddTemporaryMod = new AddTemporaryMod(_pluginInterface);
        _penumbraCreateNamedTemporaryCollection = new CreateTemporaryCollection(_pluginInterface);
        _penumbraRemoveTemporaryCollection = new DeleteTemporaryCollection(_pluginInterface);
        _penumbraAssignTemporaryCollection = new AssignTemporaryCollection(_pluginInterface);
        //_penumbraEnabled = new GetEnabledState(_pluginInterface);

        _penumbraResolvePaths = new ResolvePlayerPathsAsync(_pluginInterface);
        _penumbraResourcePaths = new GetGameObjectResourcePaths(_pluginInterface);
        _penumbraGetMetaManipulations = new GetPlayerMetaManipulations(_pluginInterface);
        //_penumbraConvertTextureFile = new ConvertTextureFile(_pluginInterface);

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;

        OnConfigurationChanged();
        PenumbraCheckStatus();
    }

    public bool PenumbraCheckStatus()
    {
        var status = CheckStatus() == IPCStatus.Available;

        checkModDirectory(status);

        return status;
    }
    void checkModDirectory(bool available)
    {
        if(!available)
        {
            ModDirectory = string.Empty;
        }
        else
        {
            ModDirectory = _penumbraResolveModDir!.Invoke().ToLowerInvariant();
        }
    }

    public void OpenPenumbra()
    {
        if(IsAvailable == false)
            return;

        _penumbraOpenMainWindow.Invoke(Penumbra.Api.Enums.TabType.Mods);
    }

    public string GetCollectionForObject(IGameObject gameObject)
    {
        if(IsAvailable == false || gameObject is null)
            return string.Empty;

        var (_, _, collection) = _penumbraGetCollectionForObject.Invoke(gameObject.ObjectIndex);
        return collection.Name;
    }

    public Guid? SetCollectionForObject(IGameObject gameObject, Guid collectionName)
    {
        if(IsAvailable == false || gameObject is null)
            return Guid.Empty;

        Brio.Log.Debug($"Setting GameObject {gameObject.ObjectIndex} collection to {collectionName}");

        var (_, oldCollection) = _penumbraSetCollectionForObject.Invoke(gameObject.ObjectIndex, collectionName, true, true);

        if(oldCollection is null)
            return null;

        return oldCollection.Value.Id;
    }

    public Dictionary<Guid, string> GetCollections()
    {
        if(IsAvailable == false)
            return null!;

        return _penumbraGetCollections.Invoke();
    }

    private void ResourceLoaded(IntPtr ptr, string arg1, string arg2)
    {
        if(ptr != IntPtr.Zero && string.Compare(arg1, arg2, ignoreCase: true, System.Globalization.CultureInfo.InvariantCulture) != 0)
        {
            OnPenumbraResourceLoaded?.Invoke(ptr, arg1, arg2);
        }
    }

    public async Task<(string[] forward, string[][] reverse)> ResolvePathsAsync(string[] forward, string[] reverse)
    {
        return await _penumbraResolvePaths.Invoke(forward, reverse).ConfigureAwait(false);
    }

    public async Task<Dictionary<string, HashSet<string>>?> GetCharacterData(IGameObject gameObject)
    {
        if(IsAvailable == false) return null;

        return await _framework.RunOnFrameworkThread(() =>
        {
            Brio.Log.Debug("Calling On IPC: Penumbra.GetGameObjectResourcePaths");
            var idx = gameObject?.ObjectIndex;
            if(idx == null) return null;
            return _penumbraResourcePaths.Invoke(idx.Value)[0];
        }).ConfigureAwait(false);
    }

    public string GetMetaManipulations()
    {
        if(IsAvailable == false) return string.Empty;

        Brio.Log.Debug("Calling On IPC: Penumbra.GetMetaManipulations");

        return _penumbraGetMetaManipulations.Invoke();
    }

    public async Task RemoveTemporaryCollectionAsync(Guid applicationId, Guid collId)
    {
        if(!IsAvailable) return;
        await _framework.RunOnFrameworkThread(() =>
        {
            Brio.Log.Debug("[{applicationId}] Removing temp collection for {collId}", applicationId, collId);
            var ret2 = _penumbraRemoveTemporaryCollection.Invoke(collId);
            Brio.Log.Debug("[{applicationId}] RemoveTemporaryCollection: {ret2}", applicationId, ret2);
        }).ConfigureAwait(false);
    }

    public async Task AssignTemporaryCollectionAsync(Guid collName, int idx)
    {
        if(!IsAvailable) return;

        await _framework.RunOnFrameworkThread(() =>
        {
            var retAssign = _penumbraAssignTemporaryCollection.Invoke(collName, idx, forceAssignment: true);
            Brio.Log.Debug("Assigning Temp Collection {collName} to index {idx}, Success: {ret}", collName, idx, retAssign);
            return collName;
        }).ConfigureAwait(false);
    }

    public async Task<Guid> CreateTemporaryCollectionAsync(string uid)
    {
        if(!IsAvailable) return Guid.Empty;

        return await _framework.RunOnFrameworkThread(() =>
        {
            var collName = "Brio_" + uid;
            _penumbraCreateNamedTemporaryCollection.Invoke("Brio", collName, out var collId);
            Brio.Log.Debug("Creating Temp Collection {collName}, GUID: {collId}", collName, collId);
            return collId;

        }).ConfigureAwait(false);
    }

    public async Task SetTemporaryModsAsync(Guid applicationId, Guid collId, Dictionary<string, string> modPaths)
    {
        if(!IsAvailable) return;

        await _framework.RunOnFrameworkThread(() =>
        {
            foreach(var mod in modPaths)
            {
                Brio.Log.Debug("[{applicationId}] Change: {from} => {to}", applicationId, mod.Key, mod.Value);
            }
            var retRemove = _penumbraRemoveTemporaryMod.Invoke("BrioChara_Files", collId, 0);
            Brio.Log.Debug("[{applicationId}] Removing temp files mod for {collId}, Success: {ret}", applicationId, collId, retRemove);
            var retAdd = _penumbraAddTemporaryMod.Invoke("BrioChara_Files", collId, modPaths, string.Empty, 0);
            Brio.Log.Debug("[{applicationId}] Setting temp files mod for {collId}, Success: {ret}", applicationId, collId, retAdd);
        }).ConfigureAwait(false);
    }

    public async Task SetManipulationDataAsync(Guid applicationId, Guid collId, string manipulationData)
    {
        if(!IsAvailable) return;

        await _framework.RunOnFrameworkThread(() =>
        {
            Brio.Log.Debug("[{applicationId}] Manip: {data}", applicationId, manipulationData);
            var retAdd = _penumbraAddTemporaryMod.Invoke("BrioChara_Meta", collId, [], manipulationData, 0);
            Brio.Log.Debug("[{applicationId}] Setting temp meta mod for {collId}, Success: {ret}", applicationId, collId, retAdd);
        }).ConfigureAwait(false);
    }

    public async Task Redraw(IGameObject gameObject, bool afterGPose = false)
    {
        if(!IsAvailable) return;

        var redrawType = RedrawType.Redraw;
        if(afterGPose) redrawType = RedrawType.AfterGPose;

        await _framework.RunOnFrameworkThread(() =>
        {
            _penumbraRedraw!.Invoke(gameObject.ObjectIndex, setting: redrawType);
        });
    }

    private void HandlePenumbraRedraw(nint arg1, int arg2)
    {
        Brio.Log.Debug("Penumbra redraw event received.");
        OnPenumbraRedraw?.Invoke(arg2);
    }

    private void OnConfigurationChanged()
        => CheckStatus();

    public override void Dispose()
    {
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;

        _penumbraGameObjectResourcePathResolved.Dispose();

        _penumbraInitializedSubscriber.Dispose();
        _penumbraDisposedSubscriber.Dispose();
        _penumbraRedrawEvent.Dispose();

        GC.SuppressFinalize(this);
    }
}
