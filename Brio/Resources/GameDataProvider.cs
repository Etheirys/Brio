using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Brio.Game.Actor.Appearance;
using Brio.Resources.Extra;
using Brio.Resources.Sheets;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using LuminaSupplemental.Excel.Model;
using LuminaSupplemental.Excel.Services;

namespace Brio.Resources;

public class GameDataProvider
{
    public static GameDataProvider Instance { get; private set; } = null!;

    private readonly IDataManager dataManager;
    private readonly ISeStringEvaluator seStringEvaluator;

    private readonly IReadOnlyDictionary<string, string> npcNames;
    private readonly FrozenDictionary<uint, HashSet<uint>> bNpcLinks;

    private readonly Dictionary<uint, string> eNpcNameCache = [];
    private readonly Dictionary<uint, string> bNpcNameCache = [];
    private readonly Dictionary<uint, string> companionNameCache = [];
    private readonly Dictionary<uint, string> mountNameCache = [];
    private readonly Dictionary<uint, string> ornamentNameCache = [];

    public ModelDatabase ModelDatabase { get; }
    public FurnitureDatabase FurnitureDatabase { get; }
    public PathDatabase PathDatabase { get; }
    public HumanData HumanData { get; }

    public ExcelSheet<BrioActionTimeline> ActionTimelines { get; }

    public BNpcBase[] FilteredBNpcBases { get; }
    public ENpcBase[] FilteredENpcBases { get; }
    public Mount[] FilteredMounts { get; }
    public Companion[] FilteredCompanions { get; }
    public Ornament[] FilteredOrnaments { get; }

    public GameDataProvider(IDataManager dataManager, ISeStringEvaluator seStringEvaluator, ResourceProvider resourceProvider)
    {
        Instance = this;

        this.dataManager = dataManager;
        this.seStringEvaluator = seStringEvaluator;

        ActionTimelines = dataManager.GetExcelSheet<BrioActionTimeline>();

        FilteredBNpcBases = [.. dataManager.GetExcelSheet<BNpcBase>().Where(row => row.RowId != 0 && row.ModelChara.RowId != 0)];
        FilteredENpcBases = [.. dataManager.GetExcelSheet<ENpcBase>().Where(row => row.RowId != 0 && row.ModelChara.RowId != 0)];
        FilteredMounts = [.. dataManager.GetExcelSheet<Mount>().Where(row => row.RowId != 0 && row.ModelChara.RowId != 0)];
        FilteredCompanions = [.. dataManager.GetExcelSheet<Companion>().Where(row => row.RowId != 0 && row.Model.RowId != 0)];
        FilteredOrnaments = [.. dataManager.GetExcelSheet<Ornament>().Where(row => row.RowId != 0 && row.Model != 0)];

        npcNames = resourceProvider.GetResourceDocument<IReadOnlyDictionary<string, string>>("Data.NpcNames.json");

        bNpcLinks = CsvLoader.LoadResource<BNpcLink>(CsvLoader.BNpcLinkResourceName, false, out _, out _, dataManager.GameData, dataManager.GameData.Options.DefaultExcelLanguage)
            .GroupBy(link => link.BNpcBaseId)
            .ToFrozenDictionary(group => group.Key, group => group.Reverse().Select(link => link.BNpcNameId).ToHashSet());

        ModelDatabase = new(resourceProvider, this);

        FurnitureDatabase = new(dataManager);

        using var pathStream = resourceProvider.GetRawResourceStream("Data.WorldObjectPaths.json.gz");
        PathDatabase = PathDatabase.LoadFromGz(pathStream, new(), new());

        HumanData = new HumanData(dataManager.GetFile("chara/xls/charamake/human.cmp")!.Data);
    }

    public ExcelSheet<T> GetExcelSheet<T>(ClientLanguage? language = null, string? name = null) where T : struct, IExcelRow<T>
        => dataManager.GetExcelSheet<T>(language, name);

    public string GetENpcName(uint eNpcId)
    {
        TryGetENpcName(eNpcId, out var name);
        return name;
    }

    public bool TryGetENpcName(uint eNpcId, out string name)
    {
        if(eNpcNameCache.TryGetValue(eNpcId, out var cachedName))
        {
            name = cachedName;
            return true;
        }

        if(seStringEvaluator.EvaluateObjStr(ObjectKind.EventNpc, eNpcId) is { Length: not 0 } evaluatedName)
        {
            eNpcNameCache.TryAdd(eNpcId, name = evaluatedName);
            return true;
        }

        if (ResolveName($"E:{eNpcId:D7}") is { Length: not 0 } resolvedName)
        {
            eNpcNameCache.TryAdd(eNpcId, name = resolvedName);
            return true;
        }

        eNpcNameCache.TryAdd(eNpcId, name = $"ENpc {eNpcId}");
        return false;
    }

    public string GetBNpcNameByBase(uint bNpcBaseId)
    {
        TryGetBNpcNameByBase(bNpcBaseId, out var name);
        return name;
    }

    public bool TryGetBNpcNameByBase(uint bNpcBaseId, out string name)
    {
        if(!bNpcLinks.TryGetValue(bNpcBaseId, out var bNpcNameIds) || bNpcNameIds.Count == 0)
        {
            name = $"BNpc {bNpcBaseId}";
            return false;
        }

        foreach (var bNpcNameId in bNpcNameIds)
        {
            if(bNpcNameCache.TryGetValue(bNpcNameId, out var cachedName))
            {
                name = string.IsNullOrEmpty(cachedName) ? $"BNpc {bNpcBaseId}" : cachedName;
                return true;
            }

            if(seStringEvaluator.EvaluateObjStr(ObjectKind.BattleNpc, bNpcNameId) is { Length: not 0 } evaluatedName)
            {
                bNpcNameCache.TryAdd(bNpcNameId, name = evaluatedName);
                return true;
            }

            if(ResolveName($"B:{bNpcBaseId:D7}") is { Length: not 0 } resolvedName)
            {
                bNpcNameCache.TryAdd(bNpcNameId, name = resolvedName);
                return true;
            }
        }

        name = $"BNpc {bNpcBaseId}";
        return false;
    }

    public string GetCompanionName(uint companionId)
    {
        TryGetCompanionName(companionId, out var name);
        return name;
    }

    public bool TryGetCompanionName(uint companionId, out string name)
    {
        if(companionNameCache.TryGetValue(companionId, out var cachedName))
        {
            name = cachedName;
            return true;
        }

        if(seStringEvaluator.EvaluateObjStr(ObjectKind.Companion, companionId) is { Length: not 0 } evaluatedName)
        {
            companionNameCache.TryAdd(companionId, name = evaluatedName);
            return true;
        }

        companionNameCache.TryAdd(companionId, name = $"Companion {companionId}");
        return false;
    }

    public string GetMountName(uint mountId)
    {
        TryGetMountName(mountId, out var name);
        return name;
    }

    public bool TryGetMountName(uint mountId, out string name)
    {
        if(mountNameCache.TryGetValue(mountId, out var cachedName))
        {
            name = cachedName;
            return true;
        }

        if(seStringEvaluator.EvaluateActStr(ActionKind.Mount, mountId) is { Length: not 0 } evaluatedName)
        {
            mountNameCache.TryAdd(mountId, name = evaluatedName);
            return true;
        }

        mountNameCache.TryAdd(mountId, name = $"Mount {mountId}");
        return false;
    }

    public string GetOrnamentName(uint ornamentId)
    {
        TryGetOrnamentName(ornamentId, out var name);
        return name;
    }

    public bool TryGetOrnamentName(uint ornamentId, out string name)
    {
        if(ornamentNameCache.TryGetValue(ornamentId, out var cachedName))
        {
            name = cachedName;
            return true;
        }

        if(seStringEvaluator.EvaluateActStr(ActionKind.Ornament, ornamentId) is { Length: not 0 } evaluatedName)
        {
            ornamentNameCache.TryAdd(ornamentId, name = evaluatedName);
            return true;
        }

        ornamentNameCache.TryAdd(ornamentId, name = $"Ornament {ornamentId}");
        return false;
    }

    public string? ResolveName(string name)
    {
        if(!npcNames.TryGetValue(name, out var nameOverride))
            return null;

        if(!nameOverride.StartsWith("N:"))
            return nameOverride;

        if(!uint.TryParse(nameOverride.AsSpan(2), out var bNpcNameId))
            return null;

        if(bNpcNameCache.TryGetValue(bNpcNameId, out var cachedName))
            return !string.IsNullOrEmpty(cachedName) ? null : cachedName;

        if(seStringEvaluator.EvaluateObjStr(ObjectKind.BattleNpc, bNpcNameId) is { Length: not 0 } evaluatedName)
        {
            bNpcNameCache.TryAdd(bNpcNameId, evaluatedName);
            return evaluatedName;
        }

        return null;
    }
}
