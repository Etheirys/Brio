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
    private static readonly IReadOnlyDictionary<string, PoseVariantDefinition> PoseVariants =
        new Dictionary<string, PoseVariantDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["s_pose01_loop.pap"] = new(50, 643, 1),
            ["s_pose02_loop.pap"] = new(50, 3132, 2),
            ["s_pose03_loop.pap"] = new(50, 3134, 3),
            ["s_pose04_loop.pap"] = new(50, 8002, 4),
            ["s_pose05_loop.pap"] = new(50, 8004, 5),
            ["j_pose01_loop.pap"] = new(52, 654, 1),
            ["j_pose02_loop.pap"] = new(52, 3136, 2),
            ["j_pose03_loop.pap"] = new(52, 3138, 3),
            ["j_pose04_loop.pap"] = new(52, 3771, 4),
            ["l_pose01_loop.pap"] = new(13, 3140, 1),
            ["l_pose02_loop.pap"] = new(13, 3142, 2),
            ["l_pose03_loop.pap"] = new(13, 585, 3),
        };

    private readonly PenumbraService _penumbraService;
    private readonly GetEnabledState _getEnabledState;
    private readonly GetCollectionForObject _getCollectionForObject;
    private readonly GetChangedItemsForCollection _getChangedItemsForCollection;
    private readonly CheckCurrentChangedItemFunc _checkCurrentChangedItem;
    private readonly GetGameObjectResourcePaths _getGameObjectResourcePaths;
    private readonly GetModPath _getModPath;

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
            var variantResources = GetVariantResources(actor.ObjectIndex);
            var actions = BuildActions(changedItems, modLookup, variantResources);
            var status = actions.Count == 0
                ? $"No active mod actions were found in {collection.Name}."
                : $"{actions.Count} active mod actions from {collection.Name}.";

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
        IReadOnlyList<VariantResourceHit> variantResources)
    {
        var actions = new List<PenumbraModAction>();
        foreach(var (changedItemName, payload) in changedItems.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            if(payload is not Emote emote || emote.RowId is 0 or > ushort.MaxValue)
                continue;

            var mods = ResolveMods(modLookup(changedItemName));
            if(mods.Count == 0)
                continue;

            var emoteName = emote.Name.ToString().Trim();
            if(string.IsNullOrWhiteSpace(emoteName))
                emoteName = $"Emote {emote.RowId}";

            if(PoseVariants.Values.Any(variant => variant.BaseEmoteId == emote.RowId))
            {
                var (variantActions, detectedMods) = BuildVariantActions(emote, emoteName, mods, variantResources);
                actions.AddRange(variantActions);
                mods = mods.Where(mod => !detectedMods.Contains(mod.ModDirectory)).ToList();
            }

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
            foreach(var actualPath in pathMap.Keys)
            {
                if(string.IsNullOrWhiteSpace(actualPath))
                    continue;

                var fileName = Path.GetFileName(actualPath);
                if(!string.IsNullOrWhiteSpace(fileName) && PoseVariants.TryGetValue(fileName, out var definition))
                    hits.Add(new VariantResourceHit(actualPath, definition));
            }

            return hits;
        }
        catch(Exception ex)
        {
            Brio.Log.Debug(ex, "Failed to inspect Penumbra pose variant resources");
            return [];
        }
    }

    private static (IReadOnlyList<PenumbraModAction> Actions, IReadOnlySet<string> DetectedMods) BuildVariantActions(
        Emote emote,
        string emoteName,
        IReadOnlyList<ResolvedModReference> mods,
        IReadOnlyList<VariantResourceHit> variantResources)
    {
        var actions = new List<PenumbraModAction>();
        var detectedMods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var mod in mods)
        {
            if(string.IsNullOrWhiteSpace(mod.PathPrefix))
                continue;

            foreach(var hit in variantResources
                .Where(hit => hit.Definition.BaseEmoteId == emote.RowId && IsPathInDirectory(hit.ActualPath, mod.PathPrefix))
                .OrderBy(hit => hit.Definition.SortOrder))
            {
                if(!seen.Add($"{mod.ModDirectory}|{hit.Definition.TimelineId}"))
                    continue;

                actions.Add(new PenumbraModAction(
                    emote,
                    $"{emoteName} {hit.Definition.SortOrder}",
                    mod.DisplayName,
                    hit.Definition.TimelineId));
                detectedMods.Add(mod.ModDirectory);
            }
        }

        return (actions, detectedMods);
    }

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
        var signature = string.Join('\n', actions.Select(action => $"{action.Emote.RowId}|{action.ModName}|{action.TimelineId}"));
        if(!string.Equals(_cacheSignature, signature, StringComparison.Ordinal))
        {
            _cachedActions = actions;
            _cacheSignature = signature;
            Version++;
        }

        _statusMessage = status;
    }

    private sealed record ResolvedModReference(string ModDirectory, string DisplayName, string? PathPrefix);
    private sealed record VariantResourceHit(string ActualPath, PoseVariantDefinition Definition);
    private readonly record struct PoseVariantDefinition(uint BaseEmoteId, uint TimelineId, int SortOrder);
}

public sealed record PenumbraModAction(Emote Emote, string EmoteName, string ModName, uint? TimelineId);
