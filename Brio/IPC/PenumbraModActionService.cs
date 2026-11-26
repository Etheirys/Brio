using Brio.Resources;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Lumina.Excel.Sheets;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Brio.IPC;

public sealed class PenumbraModActionService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly PenumbraService _penumbraService;
    private readonly GetEnabledState _getEnabledState;
    private readonly GetCollectionForObject _getCollectionForObject;
    private readonly GetChangedItemsForCollection _getChangedItemsForCollection;
    private readonly CheckCurrentChangedItemFunc _checkCurrentChangedItem;
    private readonly GetGameObjectResourcePaths _getGameObjectResourcePaths;
    private readonly GetModPath _getModPath;
    private readonly ResolveGameObjectPath _resolveGameObjectPath;

    private IReadOnlyList<PenumbraModAction> _cachedActions = [];
    private string _cacheSignature = string.Empty;
    private string _statusMessage = "Penumbra mod actions have not been scanned yet.";
    private DateTime _nextRefreshAtUtc = DateTime.MinValue;
    private ushort _cachedObjectIndex = ushort.MaxValue;

    public int Version { get; private set; }
    public string StatusMessage => _statusMessage;

    public PenumbraModActionService(IDalamudPluginInterface pluginInterface, PenumbraService penumbraService)
    {
        _penumbraService = penumbraService;
        _getEnabledState = new GetEnabledState(pluginInterface);
        _getCollectionForObject = new GetCollectionForObject(pluginInterface);
        _getChangedItemsForCollection = new GetChangedItemsForCollection(pluginInterface);
        _checkCurrentChangedItem = new CheckCurrentChangedItemFunc(pluginInterface);
        _getGameObjectResourcePaths = new GetGameObjectResourcePaths(pluginInterface);
        _getModPath = new GetModPath(pluginInterface);
        _resolveGameObjectPath = new ResolveGameObjectPath(pluginInterface);
    }

    public IReadOnlyList<PenumbraModAction> GetActiveActions(IGameObject actor)
    {
        var objectChanged = _cachedObjectIndex != actor.ObjectIndex;
        if(!objectChanged && DateTime.UtcNow < _nextRefreshAtUtc)
            return _cachedActions;

        _cachedObjectIndex = actor.ObjectIndex;
        _nextRefreshAtUtc = DateTime.UtcNow + RefreshInterval;

        Refresh(actor);
        return _cachedActions;
    }

    private void Refresh(IGameObject actor)
    {
        try
        {
            if(!_penumbraService.AllowIntegration || !_penumbraService.IsAvailable)
            {
                SetCache([], "Penumbra integration is unavailable or disabled.");
                return;
            }

            if(!_getEnabledState.Invoke())
            {
                SetCache([], "Penumbra is currently disabled.");
                return;
            }

            var (objectValid, _, collection) = _getCollectionForObject.Invoke(actor.ObjectIndex);
            if(!objectValid)
            {
                SetCache([], "The actor's Penumbra collection could not be resolved.");
                return;
            }

            var changedItems = _getChangedItemsForCollection.Invoke(collection.Id);
            var modLookup = _checkCurrentChangedItem.Invoke();
            var actions = BuildActions(changedItems, modLookup, actor.ObjectIndex);
            var status = actions.Count == 0
                ? string.Format("No active mod actions were found in {0}.", collection.Name)
                : string.Format("{0} active mod actions from {1}.", actions.Count, collection.Name);

            SetCache(actions, status);
        }
        catch(Exception ex)
        {
            Brio.Log.Warning(ex, "Failed to scan Penumbra mod actions");
            SetCache([], "Failed to scan Penumbra mod actions.");
        }
    }

    private IReadOnlyList<PenumbraModAction> BuildActions(
        IReadOnlyDictionary<string, object?> changedItems,
        Func<string, (string ModDirectory, string ModName)[]> modLookup,
        ushort objectIndex)
    {
        var modsByChangedItem = new Dictionary<string, IReadOnlyList<ResolvedModReference>>(StringComparer.OrdinalIgnoreCase);
        foreach(var changedItemName in changedItems.Keys)
        {
            var resolved = ResolveMods(modLookup(changedItemName));
            if(resolved.Count > 0)
                modsByChangedItem[changedItemName] = resolved;
        }

        var allMods = modsByChangedItem.Values
            .SelectMany(mods => mods)
            .GroupBy(mod => mod.ModDirectory, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var variantResources = GetVariantResources(objectIndex);
        var (variantActions, detectedModEmotes) = BuildVariantActions(allMods, variantResources, changedItems.Values.OfType<Emote>());
        var actions = new List<PenumbraModAction>(variantActions);

        foreach(var (changedItemName, payload) in changedItems.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            if(payload is not Emote emote || emote.RowId is 0 or > ushort.MaxValue)
                continue;

            if(!modsByChangedItem.TryGetValue(changedItemName, out var resolvedMods))
                continue;

            var mods = resolvedMods.ToList();

            var emoteName = emote.Name.ToString().Trim();
            if(string.IsNullOrWhiteSpace(emoteName))
                emoteName = $"Emote {emote.RowId}";

            if(CommonPoseCatalog.All.Any(variant => variant.BaseEmoteId == emote.RowId))
                mods = mods.Where(mod => !detectedModEmotes.Contains(VariantDetectionKey(mod.ModDirectory, emote.RowId))).ToList();

            if(mods.Count > 0)
                actions.Add(new PenumbraModAction(emote, emoteName, BuildModLabel(mods.Select(mod => mod.DisplayName).ToArray()), null));
        }

        return actions;
    }

    private IReadOnlyList<ResolvedModReference> ResolveMods((string ModDirectory, string ModName)[] modPairs)
    {
        var mods = new List<ResolvedModReference>(modPairs.Length);
        foreach(var (modDirectory, modName) in modPairs)
        {
            var displayName = string.IsNullOrWhiteSpace(modName) ? modDirectory : modName;
            if(string.IsNullOrWhiteSpace(displayName)
                || mods.Any(mod => string.Equals(mod.ModDirectory, modDirectory, StringComparison.OrdinalIgnoreCase)))
                continue;

            string? pathPrefix = null;
            try
            {
                var (result, fullPath, _, _) = _getModPath.Invoke(modDirectory, modName);
                if(result == PenumbraApiEc.Success && !string.IsNullOrWhiteSpace(fullPath))
                    pathPrefix = NormalizeDirectoryPrefix(fullPath);
            }
            catch(Exception ex)
            {
                Brio.Log.Debug(ex, "Failed to resolve the Penumbra path for {ModDirectory}", modDirectory);
            }

            mods.Add(new ResolvedModReference(modDirectory, displayName, pathPrefix));
        }

        return mods.OrderBy(mod => mod.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private IReadOnlyList<VariantResourceHit> GetVariantResources(ushort objectIndex)
    {
        try
        {
            var resources = _getGameObjectResourcePaths.Invoke(objectIndex);
            if(resources.Length == 0 || resources[0] is not { } pathMap)
                return [];

            var hits = new List<VariantResourceHit>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var variantsByFileName = new Dictionary<string, CommonPoseDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach(var definition in CommonPoseCatalog.All)
            {
                if(GameDataProvider.Instance.ActionTimelines.TryGetRow(definition.TimelineId, out var timeline))
                    variantsByFileName[$"{timeline.Key}.pap"] = definition;
            }

            foreach(var actualPath in pathMap.Keys)
            {
                if(string.IsNullOrWhiteSpace(actualPath))
                    continue;

                var fileName = Path.GetFileName(actualPath);
                if(!string.IsNullOrWhiteSpace(fileName) && variantsByFileName.TryGetValue(fileName, out var definition)
                    && seen.Add($"{actualPath}|{definition.TimelineId}"))
                    hits.Add(new VariantResourceHit(actualPath, definition));
            }

            var residentDirectories = pathMap.Values
                .SelectMany(paths => paths)
                .Select(NormalizeGamePath)
                .Where(path => path.Contains("/bt_common/resident/", StringComparison.OrdinalIgnoreCase))
                .Select(path => path[..path.LastIndexOf('/')])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach(var residentDirectory in residentDirectories)
            {
                foreach(var (fileName, definition) in variantsByFileName)
                {
                    var gamePath = $"{residentDirectory}/{fileName}";
                    var resolvedPath = _resolveGameObjectPath.Invoke(gamePath, objectIndex);
                    if(string.IsNullOrWhiteSpace(resolvedPath)
                        || string.Equals(NormalizeGamePath(resolvedPath), gamePath, StringComparison.OrdinalIgnoreCase)
                        || !seen.Add($"{resolvedPath}|{definition.TimelineId}"))
                        continue;

                    hits.Add(new VariantResourceHit(resolvedPath, definition));
                }
            }

            return hits;
        }
        catch(Exception ex)
        {
            Brio.Log.Debug(ex, "Failed to inspect Penumbra pose variant resources");
            return [];
        }
    }

    private static (IReadOnlyList<PenumbraModAction> Actions, IReadOnlySet<string> DetectedModEmotes) BuildVariantActions(
        IReadOnlyList<ResolvedModReference> mods,
        IReadOnlyList<VariantResourceHit> variantResources,
        IEnumerable<Emote> emotes)
    {
        var actions = new List<PenumbraModAction>();
        var detectedModEmotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var emotesById = emotes
            .GroupBy(emote => emote.RowId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach(var mod in mods)
        {
            if(string.IsNullOrWhiteSpace(mod.PathPrefix))
                continue;

            foreach(var hit in variantResources
                .Where(hit => IsPathInDirectory(hit.ActualPath, mod.PathPrefix))
                .OrderBy(hit => hit.Definition.Kind)
                .ThenBy(hit => hit.Definition.VariantIndex))
            {
                if(!seen.Add($"{mod.ModDirectory}|{hit.Definition.TimelineId}"))
                    continue;

                Emote? emote = null;
                if(hit.Definition.BaseEmoteId != 0 && emotesById.TryGetValue(hit.Definition.BaseEmoteId, out var baseEmote))
                    emote = baseEmote;

                actions.Add(new PenumbraModAction(
                    emote,
                    hit.Definition.DisplayName,
                    mod.DisplayName,
                    hit.Definition.TimelineId));

                if(hit.Definition.BaseEmoteId != 0)
                    detectedModEmotes.Add(VariantDetectionKey(mod.ModDirectory, hit.Definition.BaseEmoteId));
            }
        }

        return (actions, detectedModEmotes);
    }

    private static string NormalizeGamePath(string path)
        => path.Replace('\\', '/');

    private static string VariantDetectionKey(string modDirectory, uint baseEmoteId)
        => $"{modDirectory}|{baseEmoteId}";

    private static string BuildModLabel(IReadOnlyList<string> names)
        => names.Count switch
        {
            0 => string.Empty,
            1 => names[0],
            2 => $"{names[0]} / {names[1]}",
            _ => $"{names[0]} +{names.Count - 1}",
        };

    private static string NormalizeDirectoryPrefix(string path)
    {
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);
        return $"{normalized}{Path.DirectorySeparatorChar}";
    }

    private static bool IsPathInDirectory(string actualPath, string pathPrefix)
    {
        var normalizedPath = actualPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        return normalizedPath.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private void SetCache(IReadOnlyList<PenumbraModAction> actions, string status)
    {
        var signature = string.Join('\n', actions.Select(action => $"{action.Emote?.RowId ?? 0}|{action.ModName}|{action.TimelineId}"));
        if(!string.Equals(_cacheSignature, signature, StringComparison.Ordinal))
        {
            _cachedActions = actions;
            _cacheSignature = signature;
            Version++;
        }

        _statusMessage = status;
    }

    private sealed record ResolvedModReference(string ModDirectory, string DisplayName, string? PathPrefix);
    private sealed record VariantResourceHit(string ActualPath, CommonPoseDefinition Definition);
}

public sealed record PenumbraModAction(Emote? Emote, string EmoteName, string ModName, uint? TimelineId);
