using System.Collections.Generic;
using Brio.Game.Actor.Appearance;
using Brio.Resources.Extra;
using Brio.Resources.Sheets;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Brio.Resources;

public class GameDataProvider
{
    public static GameDataProvider Instance { get; private set; } = null!;

    public IDataManager DataManager { get; }

    public ISeStringEvaluator SeStringEvaluator { get; }

    public ResourceProvider ResourceProvider { get; }

    public readonly ExcelSheet<TerritoryType> TerritoryTypes;
    public readonly ExcelSheet<Weather> Weathers;
    public readonly ExcelSheet<WeatherRate> WeatherRates;
    public readonly ExcelSheet<Companion> Companions;
    public readonly ExcelSheet<Ornament> Ornaments;
    public readonly ExcelSheet<Mount> Mounts;
    public readonly ExcelSheet<Festival> Festivals;
    public readonly ExcelSheet<Status> Statuses;
    public readonly ExcelSheet<BrioActionTimeline> ActionTimelines;
    public readonly ExcelSheet<Emote> Emotes;
    public readonly ExcelSheet<Action> Actions;
    public readonly ExcelSheet<ENpcBase> ENpcBases;
    public readonly ExcelSheet<ENpcResident> ENpcResidents;
    public readonly ExcelSheet<BNpcBase> BNpcBases;
    public readonly ExcelSheet<BNpcCustomize> BNpcCustomizations;
    public readonly ExcelSheet<BNpcName> BNpcNames;
    public readonly ExcelSheet<NpcEquip> NpcEquips;
    public readonly ExcelSheet<Stain> Stains;
    public readonly ExcelSheet<CharaMakeCustomize> CharaMakeCustomizations;
    public readonly ExcelSheet<BrioCharaMakeType> CharaMakeTypes;
    public readonly ExcelSheet<BrioHairMakeType> HairMakeTypes;
    public readonly ExcelSheet<Item> Items;
    public readonly ExcelSheet<Glasses> Glasses;

    public readonly ModelDatabase ModelDatabase;

    public readonly HumanData HumanData;

    public GameDataProvider(IDataManager dataManager, ISeStringEvaluator seStringEvaluator, ResourceProvider resourceProvider)
    {
        Instance = this;

        DataManager = dataManager;

        SeStringEvaluator = seStringEvaluator;

        ResourceProvider = resourceProvider;

        TerritoryTypes = dataManager.GetExcelSheet<TerritoryType>();

        Weathers = dataManager.GetExcelSheet<Weather>();

        WeatherRates = dataManager.GetExcelSheet<WeatherRate>();

        Companions = dataManager.GetExcelSheet<Companion>();

        Ornaments = dataManager.GetExcelSheet<Ornament>();

        Mounts = dataManager.GetExcelSheet<Mount>();

        Festivals = dataManager.GetExcelSheet<Festival>();

        Statuses = dataManager.GetExcelSheet<Status>();

        ActionTimelines = dataManager.GetExcelSheet<BrioActionTimeline>();

        Emotes = dataManager.GetExcelSheet<Emote>();

        Actions = dataManager.GetExcelSheet<Action>();

        ENpcBases = dataManager.GetExcelSheet<ENpcBase>();

        ENpcResidents = dataManager.GetExcelSheet<ENpcResident>();

        BNpcBases = dataManager.GetExcelSheet<BNpcBase>();

        BNpcCustomizations = dataManager.GetExcelSheet<BNpcCustomize>();

        BNpcNames = dataManager.GetExcelSheet<BNpcName>();

        NpcEquips = dataManager.GetExcelSheet<NpcEquip>();

        Stains = dataManager.GetExcelSheet<Stain>();

        CharaMakeCustomizations = dataManager.GetExcelSheet<CharaMakeCustomize>();

        CharaMakeTypes = dataManager.GetExcelSheet<BrioCharaMakeType>();

        HairMakeTypes = dataManager.GetExcelSheet<BrioHairMakeType>();

        Items = dataManager.GetExcelSheet<Item>();

        Glasses = dataManager.GetExcelSheet<Glasses>();

        HumanData = new HumanData(dataManager.GetFile("chara/xls/charamake/human.cmp")!.Data);

        ModelDatabase = new(resourceProvider, this);
    }

    public string GetENpcName(uint eNpcNameId) => SeStringEvaluator.EvaluateObjStr(ObjectKind.EventNpc, eNpcNameId) is { Length: not 0 } name ? name : ResolveName($"E:{eNpcNameId:D7}") ?? $"ENpc {eNpcNameId}";

    public string GetBNpcName(uint bNpcNameId) => SeStringEvaluator.EvaluateObjStr(ObjectKind.BattleNpc, bNpcNameId) is { Length: not 0 } name ? name : ResolveName($"B:{bNpcNameId:D7}") ?? $"BNpc {bNpcNameId}";

    public string GetCompanionName(uint companionId) => SeStringEvaluator.EvaluateObjStr(ObjectKind.Companion, companionId) is { Length: not 0 } name ? name : $"Companion {companionId}";

    public string GetMountName(uint mountId) => SeStringEvaluator.EvaluateActStr(ActionKind.Mount, mountId) is { Length: not 0 } name ? name : $"Mount {mountId}";

    public string? ResolveName(string name)
    {
        var names = ResourceProvider.GetResourceDocument<IReadOnlyDictionary<string, string>>("Data.NpcNames.json");

        if(names.TryGetValue(name, out var nameOverride))
            name = nameOverride;

        if(name.StartsWith("N:"))
        {
            var nameId = uint.Parse(name.Substring(2));
            var bNpcName = GetBNpcName(nameId);

            if(!string.IsNullOrEmpty(bNpcName))
            {
                return bNpcName;
            }
        }

        return null;
    }
}
