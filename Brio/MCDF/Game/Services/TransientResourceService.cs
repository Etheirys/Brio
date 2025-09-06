using Brio.Config;
using Brio.Game.Core;
using Brio.IPC;
using Brio.MCDF.API.Data;
using Brio.MCDF.API.Data.Enum;
using Brio.MCDF.Utils;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Brio.MCDF.Game.Services;

public class TransientResourceService : IDisposable
{
    private readonly ConfigurationService _configurationService;
    private readonly DalamudService _dalamudUtil;
    private readonly PenumbraService _penumbraService;
    private readonly IFramework _framework;

    private readonly object _cacheAdditionLock = new();
    private readonly HashSet<string> _cachedHandledPaths = new(StringComparer.Ordinal);

    private readonly string[] _handledFileTypes = ["tmb", "pap", "avfx", "atex", "sklb", "eid", "phyb", "scd", "skp", "shpk"];
    private readonly string[] _handledRecordingFileTypes = ["tex", "mdl", "mtrl"];

    private readonly HashSet<(IGameObject GameObject, ObjectKind ObjectKind)> _playerRelatedPointers = [];
    private ConcurrentDictionary<IntPtr, ObjectKind> _cachedFrameAddresses = [];
    private ConcurrentDictionary<ObjectKind, HashSet<string>>? _semiTransientResources = null;

    private uint _lastClassJobId = uint.MaxValue;

    public bool IsTransientRecording { get; private set; } = false;

    public TransientResourceService(ConfigurationService configurationService, DalamudService dalamudUtil, PenumbraService penumbraService, IFramework framework)
    {
        _configurationService = configurationService;
        _dalamudUtil = dalamudUtil;
        _penumbraService = penumbraService;
        _framework = framework;

        _framework.Update += Framework_Update;
        _penumbraService.OnPenumbraResourceLoaded += Manager_PenumbraResourceLoadEvent;
    }

    private void Framework_Update(IFramework framework)
    {
        DalamudUtil_FrameworkUpdate();
    }

    private TransientConfig.TransientPlayerConfig PlayerConfig
    {
        get
        {
            if(!_configurationService.Configuration.MCDF.TransientConfig.TransientConfigs.TryGetValue(PlayerPersistentDataKey, out var transientConfig))
            {
                _configurationService.Configuration.MCDF.TransientConfig.TransientConfigs[PlayerPersistentDataKey] = transientConfig = new();
            }

            return transientConfig;
        }
    }

    private string PlayerPersistentDataKey => _dalamudUtil.GetPlayerNameAsync().GetAwaiter().GetResult() + "_" + _dalamudUtil.GetHomeWorldIdAsync().GetAwaiter().GetResult();
    private ConcurrentDictionary<ObjectKind, HashSet<string>> SemiTransientResources
    {
        get
        {
            if(_semiTransientResources == null)
            {
                _semiTransientResources = new();
                PlayerConfig.JobSpecificCache.TryGetValue(_dalamudUtil.ClassJobId, out var jobSpecificData);
                _semiTransientResources[ObjectKind.Player] = PlayerConfig.GlobalPersistentCache.Concat(jobSpecificData ?? []).ToHashSet(StringComparer.Ordinal);
                PlayerConfig.JobSpecificPetCache.TryGetValue(_dalamudUtil.ClassJobId, out var petSpecificData);
                _semiTransientResources[ObjectKind.Pet] = [.. petSpecificData ?? []];
            }

            return _semiTransientResources;
        }
    }
    private ConcurrentDictionary<ObjectKind, HashSet<string>> TransientResources { get; } = new();

    public void CleanUpSemiTransientResources(ObjectKind objectKind, List<FileReplacement>? fileReplacement = null)
    {
        if(!SemiTransientResources.TryGetValue(objectKind, out HashSet<string>? value))
            return;

        if(fileReplacement == null)
        {
            value.Clear();
            return;
        }

        int removedPaths = 0;
        foreach(var replacement in fileReplacement.Where(p => !p.HasFileReplacement).SelectMany(p => p.GamePaths).ToList())
        {
            removedPaths += PlayerConfig.RemovePath(replacement, objectKind);
            value.Remove(replacement);
        }

        if(removedPaths > 0)
        {
            Brio.Log.Verbose("Removed {amount} of SemiTransient paths during CleanUp, Saving from {name}", removedPaths, nameof(CleanUpSemiTransientResources));
            // force reload semi transient resources
            _configurationService.Save();
        }
    }

    public HashSet<string> GetSemiTransientResources(ObjectKind objectKind)
    {
        SemiTransientResources.TryGetValue(objectKind, out var result);

        return result ?? new HashSet<string>(StringComparer.Ordinal);
    }

    public void PersistTransientResources(ObjectKind objectKind)
    {
        if(!SemiTransientResources.TryGetValue(objectKind, out HashSet<string>? semiTransientResources))
        {
            SemiTransientResources[objectKind] = semiTransientResources = new(StringComparer.Ordinal);
        }

        if(!TransientResources.TryGetValue(objectKind, out var resources))
        {
            return;
        }

        var transientResources = resources.ToList();
        Brio.Log.Verbose("Persisting {count} transient resources", transientResources.Count);
        List<string> newlyAddedGamePaths = resources.Except(semiTransientResources, StringComparer.Ordinal).ToList();
        foreach(var gamePath in transientResources)
        {
            semiTransientResources.Add(gamePath);
        }

        bool saveConfig = false;
        if(objectKind == ObjectKind.Player && newlyAddedGamePaths.Count != 0)
        {
            saveConfig = true;
            foreach(var item in newlyAddedGamePaths.Where(f => !string.IsNullOrEmpty(f)))
            {
                PlayerConfig.AddOrElevate(_dalamudUtil.ClassJobId, item);
            }
        }
        else if(objectKind == ObjectKind.Pet && newlyAddedGamePaths.Count != 0)
        {
            saveConfig = true;

            if(!PlayerConfig.JobSpecificPetCache.TryGetValue(_dalamudUtil.ClassJobId, out var petPerma))
            {
                PlayerConfig.JobSpecificPetCache[_dalamudUtil.ClassJobId] = petPerma = [];
            }

            foreach(var item in newlyAddedGamePaths.Where(f => !string.IsNullOrEmpty(f)))
            {
                petPerma.Add(item);
            }
        }

        if(saveConfig)
        {
            Brio.Log.Verbose("Saving transient.json from {method}", nameof(PersistTransientResources));
            _configurationService.Save();
        }

        TransientResources[objectKind].Clear();
    }

    public void RemoveTransientResource(ObjectKind objectKind, string path)
    {
        if(SemiTransientResources.TryGetValue(objectKind, out var resources))
        {
            resources.RemoveWhere(f => string.Equals(path, f, StringComparison.Ordinal));
            if(objectKind == ObjectKind.Player)
            {
                PlayerConfig.RemovePath(path, objectKind);
                Brio.Log.Verbose("Saving transient.json from {method}", nameof(RemoveTransientResource));
                _configurationService.Save();
            }
        }
    }

    internal bool AddTransientResource(ObjectKind objectKind, string item)
    {
        if(SemiTransientResources.TryGetValue(objectKind, out var semiTransient) && semiTransient != null && semiTransient.Contains(item))
            return false;

        if(!TransientResources.TryGetValue(objectKind, out HashSet<string>? transientResource))
        {
            transientResource = new HashSet<string>(StringComparer.Ordinal);
            TransientResources[objectKind] = transientResource;
        }

        return transientResource.Add(item.ToLowerInvariant());
    }

    internal void ClearTransientPaths(ObjectKind objectKind, List<string> list)
    {
        // ignore all recording only datatypes
        int recordingOnlyRemoved = list.RemoveAll(entry => _handledRecordingFileTypes.Any(ext => entry.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
        if(recordingOnlyRemoved > 0)
        {
            Brio.Log.Verbose("Ignored {0} game paths when clearing transients", recordingOnlyRemoved);
        }

        if(TransientResources.TryGetValue(objectKind, out var set))
        {
            foreach(var file in set.Where(p => list.Contains(p, StringComparer.OrdinalIgnoreCase)))
            {
                Brio.Log.Verbose("Removing From Transient: {file}", file);
            }

            int removed = set.RemoveWhere(p => list.Contains(p, StringComparer.OrdinalIgnoreCase));
            Brio.Log.Verbose("Removed {removed} previously existing transient paths", removed);
        }

        bool reloadSemiTransient = false;
        if(objectKind == ObjectKind.Player && SemiTransientResources.TryGetValue(objectKind, out var semiset))
        {
            foreach(var file in semiset.Where(p => list.Contains(p, StringComparer.OrdinalIgnoreCase)))
            {
                Brio.Log.Verbose("Removing From SemiTransient: {file}", file);
                PlayerConfig.RemovePath(file, objectKind);
            }

            int removed = semiset.RemoveWhere(p => list.Contains(p, StringComparer.OrdinalIgnoreCase));
            Brio.Log.Verbose("Removed {removed} previously existing semi transient paths", removed);
            if(removed > 0)
            {
                reloadSemiTransient = true;
                Brio.Log.Verbose("Saving transient.json from {method}", nameof(ClearTransientPaths));
                _configurationService.Save();
            }
        }

        if(reloadSemiTransient)
            _semiTransientResources = null;
    }

    public void Dispose()
    {
        _penumbraService.OnPenumbraResourceLoaded -= Manager_PenumbraResourceLoadEvent;
        Framework.Update -= Framework_Update;

        TransientResources.Clear();
        SemiTransientResources.Clear();
    }

    private void DalamudUtil_FrameworkUpdate()
    {
        _cachedFrameAddresses = new(_playerRelatedPointers.Where(k => k.GameObject.Address != nint.Zero).ToDictionary(c => c.GameObject.Address, c => c.ObjectKind));
        lock(_cacheAdditionLock)
        {
            _cachedHandledPaths.Clear();
        }

        if(_lastClassJobId != _dalamudUtil.ClassJobId)
        {
            _lastClassJobId = _dalamudUtil.ClassJobId;
            if(SemiTransientResources.TryGetValue(ObjectKind.Pet, out HashSet<string>? value))
            {
                value?.Clear();
            }

            // reload config for current new classjob
            PlayerConfig.JobSpecificCache.TryGetValue(_dalamudUtil.ClassJobId, out var jobSpecificData);
            SemiTransientResources[ObjectKind.Player] = PlayerConfig.GlobalPersistentCache.Concat(jobSpecificData ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
            PlayerConfig.JobSpecificPetCache.TryGetValue(_dalamudUtil.ClassJobId, out var petSpecificData);
            SemiTransientResources[ObjectKind.Pet] = [.. petSpecificData ?? []];
        }

        foreach(var kind in Enum.GetValues(typeof(ObjectKind)))
        {
            if(!_cachedFrameAddresses.Any(k => k.Value == (ObjectKind)kind) && TransientResources.Remove((ObjectKind)kind, out _))
            {
                Brio.Log.Verbose("Object not present anymore: {kind}", kind);
            }
        }
    }

    public void RebuildSemiTransientResources()
    {
        _semiTransientResources = null;
    }

    private void Manager_PenumbraResourceLoadEvent(IntPtr GameObject, string GamePath, string FilePath)
    {
        (IntPtr GameObject, string GamePath, string FilePath) msg = (GameObject, GamePath, FilePath);

        var gamePath = msg.GamePath.ToLowerInvariant();
        var gameObjectAddress = msg.GameObject;
        var filePath = msg.FilePath;

        // ignore files already processed this frame
        if(_cachedHandledPaths.Contains(gamePath)) return;

        lock(_cacheAdditionLock)
        {
            _cachedHandledPaths.Add(gamePath);
        }

        // replace individual mtrl stuff
        if(filePath.StartsWith("|", StringComparison.OrdinalIgnoreCase))
        {
            filePath = filePath.Split("|")[2];
        }
        // replace filepath
        filePath = filePath.ToLowerInvariant().Replace("\\", "/", StringComparison.OrdinalIgnoreCase);

        // ignore files that are the same
        string replacedGamePath = gamePath.ToLowerInvariant().Replace("\\", "/", StringComparison.OrdinalIgnoreCase);
        if(string.Equals(filePath, replacedGamePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // ignore files to not handle
        var handledTypes = IsTransientRecording ? _handledRecordingFileTypes.Concat(_handledFileTypes) : _handledFileTypes;
        if(!handledTypes.Any(type => gamePath.EndsWith(type, StringComparison.OrdinalIgnoreCase)))
        {
            lock(_cacheAdditionLock)
            {
                _cachedHandledPaths.Add(gamePath);
            }
            return;
        }

        // ignore files not belonging to anything player related
        if(!_cachedFrameAddresses.TryGetValue(gameObjectAddress, out var objectKind))
        {
            lock(_cacheAdditionLock)
            {
                _cachedHandledPaths.Add(gamePath);
            }
            return;
        }

        // ^ all of the code above is just to sanitize the data

        if(!TransientResources.TryGetValue(objectKind, out HashSet<string>? transientResources))
        {
            transientResources = new(StringComparer.OrdinalIgnoreCase);
            TransientResources[objectKind] = transientResources;
        }

        var owner = _playerRelatedPointers.FirstOrDefault(f => f.GameObject.Address == gameObjectAddress).GameObject;
        bool alreadyTransient = false;

        bool transientContains = transientResources.Contains(replacedGamePath);
        bool semiTransientContains = SemiTransientResources.SelectMany(k => k.Value).Any(f => string.Equals(f, gamePath, StringComparison.OrdinalIgnoreCase));
        if(transientContains || semiTransientContains)
        {
            if(!IsTransientRecording)
                Brio.Log.Verbose("Not adding {replacedPath} => {filePath}, Reason: Transient: {contains}, SemiTransient: {contains2}", replacedGamePath, filePath,
                    transientContains, semiTransientContains);
            alreadyTransient = true;
        }
        else
        {
            if(!IsTransientRecording)
            {
                bool isAdded = transientResources.Add(replacedGamePath);
                if(isAdded)
                {
                    Brio.Log.Verbose("Adding {replacedGamePath} for {gameObject} ({filePath})", replacedGamePath, owner?.ToString() ?? gameObjectAddress.ToString("X"), filePath);
                    //SendTransients(gameObjectAddress, objectKind);
                }
            }
        }

        if(owner != null && IsTransientRecording)
        {
            _recordedTransients.Add(new TransientRecord(owner, replacedGamePath, filePath, alreadyTransient) { AddTransient = !alreadyTransient });
        }
    }

    private void SendTransients(nint gameObject, ObjectKind objectKind)
    {
        _ = Task.Run(async () =>
        {
            _sendTransientCts?.Cancel();
            _sendTransientCts?.Dispose();
            _sendTransientCts = new();
            var token = _sendTransientCts.Token;
            await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            foreach(var kvp in TransientResources)
            {
                if(TransientResources.TryGetValue(objectKind, out var values) && values.Any())
                {
                    Brio.Log.Verbose("Sending Transients for {kind}", objectKind);
                    //Mediator.Publish(new TransientResourceChangedMessage(gameObject));
                }
            }
        });
    }

    public void StartRecording(CancellationToken token)
    {
        if(IsTransientRecording) return;
        _recordedTransients.Clear();
        IsTransientRecording = true;
        RecordTimeRemaining.Value = TimeSpan.FromSeconds(150);
        _ = Task.Run(async () =>
        {
            try
            {
                while(RecordTimeRemaining.Value > TimeSpan.Zero && !token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                    RecordTimeRemaining.Value = RecordTimeRemaining.Value.Subtract(TimeSpan.FromSeconds(1));
                }
            }
            finally
            {
                IsTransientRecording = false;
            }
        }, token);
    }

    public async Task WaitForRecording(CancellationToken token)
    {
        while(IsTransientRecording)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        }
    }

    internal void SaveRecording()
    {
        //HashSet<nint> addedTransients = [];
        //foreach(var item in _recordedTransients)
        //{
        //    if(!item.AddTransient || item.AlreadyTransient) continue;
        //    if(!TransientResources.TryGetValue(item.Owner.ObjectKind, out var transient))
        //    {
        //        TransientResources[item.Owner.ObjectKind] = transient = [];
        //    }

        //    Brio.Log.Verbose("Adding recorded: {gamePath} => {filePath}", item.GamePath, item.FilePath);

        //    transient.Add(item.GamePath);
        //    addedTransients.Add(item.Owner.Address);
        //}

        //_recordedTransients.Clear();

        //foreach(var item in addedTransients)
        //{
        //    Mediator.Publish(new TransientResourceChangedMessage(item));
        //}
    }

    private readonly HashSet<TransientRecord> _recordedTransients = [];
    public IReadOnlySet<TransientRecord> RecordedTransients => _recordedTransients;

    public ValueProgress<TimeSpan> RecordTimeRemaining { get; } = new();

    public IFramework Framework => _framework;

    private CancellationTokenSource _sendTransientCts = new();

    public record TransientRecord(IGameObject Owner, string GamePath, string FilePath, bool AlreadyTransient)
    {
        public bool AddTransient { get; set; }
    }
}
