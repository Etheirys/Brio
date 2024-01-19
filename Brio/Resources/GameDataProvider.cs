using Brio.Game.Actor.Appearance;
using Brio.Resources.Extra;
using Brio.Resources.Sheets;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Resources;

internal class GameDataProvider
{
    public static GameDataProvider Instance { get; private set; } = null!;

    public readonly IReadOnlyDictionary<uint, TerritoryType> TerritoryTypes;
    public readonly IReadOnlyDictionary<uint, Weather> Weathers;
    public readonly IReadOnlyDictionary<uint, WeatherRate> WeatherRates;
    public readonly IReadOnlyDictionary<uint, Companion> Companions;
    public readonly IReadOnlyDictionary<uint, Ornament> Ornaments;
    public readonly IReadOnlyDictionary<uint, Mount> Mounts;
    public readonly IReadOnlyDictionary<uint, Festival> Festivals;
    public readonly IReadOnlyDictionary<uint, Status> Statuses;
    public readonly IReadOnlyDictionary<uint, ActionTimeline> ActionTimelines;
    public readonly IReadOnlyDictionary<uint, Emote> Emotes;
    public readonly IReadOnlyDictionary<uint, Action> Actions;
    public readonly IReadOnlyDictionary<uint, ENpcBase> ENpcBases;
    public readonly IReadOnlyDictionary<uint, ENpcResident> ENpcResidents;
    public readonly IReadOnlyDictionary<uint, BNpcBase> BNpcBases;
    public readonly IReadOnlyDictionary<uint, BNpcCustomize> BNpcCustomizations;
    public readonly IReadOnlyDictionary<uint, BNpcName> BNpcNames;
    public readonly IReadOnlyDictionary<uint, NpcEquip> NpcEquips;
    public readonly IReadOnlyDictionary<uint, Stain> Stains;
    public readonly IReadOnlyDictionary<uint, CharaMakeCustomize> CharaMakeCustomizations;
    public readonly IReadOnlyDictionary<uint, BrioCharaMakeType> CharaMakeTypes;
    public readonly IReadOnlyDictionary<uint, BrioHairMakeType> HairMakeTypes;
    public readonly IReadOnlyDictionary<uint, Item> Items;
    public readonly ModelDatabase ModelDatabase;

    public readonly HumanData HumanData;

    public GameDataProvider(IDataManager dataManager)
    {
        Instance = this;

        TerritoryTypes = dataManager.GetExcelSheet<TerritoryType>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Weathers = dataManager.GetExcelSheet<Weather>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        WeatherRates = dataManager.GetExcelSheet<WeatherRate>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Companions = dataManager.GetExcelSheet<Companion>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Ornaments = dataManager.GetExcelSheet<Ornament>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Mounts = dataManager.GetExcelSheet<Mount>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Festivals = dataManager.GetExcelSheet<Festival>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Statuses = dataManager.GetExcelSheet<Status>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        ActionTimelines = dataManager.GetExcelSheet<ActionTimeline>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Emotes = dataManager.GetExcelSheet<Emote>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Actions = dataManager.GetExcelSheet<Action>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        ENpcBases = dataManager.GetExcelSheet<ENpcBase>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        ENpcResidents = dataManager.GetExcelSheet<ENpcResident>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        BNpcBases = dataManager.GetExcelSheet<BNpcBase>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        BNpcCustomizations = dataManager.GetExcelSheet<BNpcCustomize>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        BNpcNames = dataManager.GetExcelSheet<BNpcName>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        NpcEquips = dataManager.GetExcelSheet<NpcEquip>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Stains = dataManager.GetExcelSheet<Stain>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        CharaMakeCustomizations = dataManager.GetExcelSheet<CharaMakeCustomize>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        CharaMakeTypes = dataManager.GetExcelSheet<BrioCharaMakeType>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        HairMakeTypes = dataManager.GetExcelSheet<BrioHairMakeType>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        Items = dataManager.GetExcelSheet<Item>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

        HumanData = new HumanData(dataManager.GetFile("chara/xls/charamake/human.cmp")!.Data);

        ModelDatabase = new();
    }
}
