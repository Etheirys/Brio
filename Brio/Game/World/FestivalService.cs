using Brio.Core;
using Brio.Game.GPose;
using Brio.Resources;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Data.Files;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Brio.Game.World;

public unsafe class FestivalService : MediatorSubscriberBase
{
    public const int MaxFestivals = 8;

    public readonly IReadOnlyDictionary<uint, FestivalEntry> FestivalList;

    public bool HasMoreSlots => EngineActiveFestivals.Length != 0;
    public bool HasOverride => _originalState != null;
    public bool CanModify => _gPoseService.IsGPosing;


    private readonly IClientState _clientState;
    private readonly IToastGui _toastGui;
    private readonly IFramework _framework;
    private readonly GPoseService _gPoseService;
    private readonly ResourceProvider _resourceProvider;
    private readonly IObjectTable _objectTable;
    private readonly IDataManager _dataManager;

    private readonly Queue<GameMain.Festival[]> _pendingChanges = new();
    private GameMain.Festival[]? _originalState;

    public GameMain.Festival[] EngineActiveFestivals
    {
        get
        {
            GameMain.Festival[] activeFestivals = new GameMain.Festival[MaxFestivals];
            var engineFestivals = GameMain.Instance()->ActiveFestivals;
            for(int i = 0; i < MaxFestivals; i++)
            {
                activeFestivals[i] = engineFestivals[i];
            }

            return activeFestivals;
        }
    }

    public FestivalService(IClientState clientState, IDataManager dataManager, IObjectTable objects, IToastGui toastGui, Mediator mediator, GameDataProvider gameDataProvider, IFramework framework, GPoseService gPoseService, ResourceProvider resourceProvider) : base(mediator)
    {
        _clientState = clientState;
        _toastGui = toastGui;
        _framework = framework;
        _gPoseService = gPoseService;
        _resourceProvider = resourceProvider;
        _objectTable = objects;
        _dataManager = dataManager;

        FestivalList = BuildFestivalList(gameDataProvider);

        mediator.Subscribe<GposeStateChangedMessage>(this, (state) => OnGPoseStateChanged(state.NewState));
        mediator.Subscribe<FrameworkUpdateMessage>(this, (state) => OnFrameworkUpdate(state.Framework));
        mediator.Subscribe<TerritoryChangedMessage>(this, (state) => OnTerritoryChanged(state.TerritoryId));
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var layoutManager = LayoutWorld.Instance()->ActiveLayout;
        if(_pendingChanges.Count > 0 && layoutManager != null && (layoutManager->FestivalStatus == 5 || layoutManager->FestivalStatus == 0))
        {
            var pending = _pendingChanges.Dequeue();
            if(pending != null)
                PublicApply(pending);
        }
    }

    public bool ChangePhase(uint festival, ushort newPhase)
    {
        var active = EngineActiveFestivals;
        var copy = active.ToArray();

        for(int i = 0; i < MaxFestivals; ++i)
        {
            if(active[i].Id == festival)
            {
                Brio.Log.Verbose($"Changing phase of festival {festival} from {active[i].Phase} to {newPhase}");

                SnapshotFestivalsIfNeeded();
                copy[i] = new GameMain.Festival() { Id = (ushort)festival, Phase = newPhase };
                _pendingChanges.Enqueue(copy);

                return true;
            }
        }

        return AddFestival(festival, newPhase);
    }

    public bool AddFestival(uint festival, ushort phase = 1)
    {
        if(!CheckFestivalRestrictions(festival))
            return false;

        var active = EngineActiveFestivals;
        var copy = active.ToArray();

        for(int i = 0; i < MaxFestivals; ++i)
        {
            if(active[i].Id == 0)
            {
                SnapshotFestivalsIfNeeded();
                copy[i] = new GameMain.Festival() { Id = (ushort)festival, Phase = phase };
                _pendingChanges.Enqueue(copy);
                return true;
            }
        }

        return false;
    }

    public bool RemoveFestival(uint festival)
    {
        if(!CheckFestivalRestrictions(festival))
            return false;

        var active = EngineActiveFestivals;
        var copy = active.ToArray();

        for(int i = 0; i < MaxFestivals; ++i)
        {
            if(active[i].Id == festival)
            {
                SnapshotFestivalsIfNeeded();
                copy[i] = new GameMain.Festival() { Id = 0, Phase = 0 };
                _pendingChanges.Enqueue(copy);
                return true;
            }
        }

        return false;
    }

    public unsafe void ResetFestivals(bool tryThisFrame = false)
    {
        if(_originalState != null)
        {
            if(tryThisFrame)
            {
                PublicApply(_originalState, false);
            }
            else
            {
                _pendingChanges.Enqueue([.. _originalState]);
            }

            _originalState = null;
        }
    }

    private void PublicApply(GameMain.Festival[] festivals, bool applyNow = true)
    {
        if(applyNow)
        {
            GameMain.Instance()->SetActiveFestivals(
                festivals[0], festivals[4],
                festivals[1], festivals[5],
                festivals[2], festivals[6],
                festivals[3], festivals[7]);
        }
        else
        {
            GameMain.Instance()->QueueActiveFestivals(
                festivals[0], festivals[4],
                festivals[1], festivals[5],
                festivals[2], festivals[6],
                festivals[3], festivals[7]);
        }
    }

    private void SnapshotFestivalsIfNeeded()
    {
        _originalState ??= [.. EngineActiveFestivals];
    }

    private bool CheckFestivalRestrictions(uint festivalId, bool showError = true)
    {
        if(FestivalList.TryGetValue(festivalId, out var festival))
        {
            if(festival.AreaExclusion != null)
            {
                if(_clientState.TerritoryType == festival.AreaExclusion.TerritoryType)
                {
                    var localPlayer = _objectTable.LocalPlayer;
                    if(localPlayer == null)
                        return false;

                    var playerPosition = new Vector2(localPlayer.Position.X, localPlayer.Position.Z);
                    var polygon = festival.AreaExclusion.Polygon.Select(i => i.AsVector2()).ToArray();
                    if(playerPosition.IsPointInPolygon(polygon))
                    {
                        if(showError)
                            _toastGui.ShowError($"Unable to apply festival here.\nReason: {festival.AreaExclusion.Reason}");

                        return false;
                    }
                }
            }
        }

        return true;
    }

    public Dictionary<string, (string name, HashSet<uint> ids)> BGPathToTerritoryType = [];
    public readonly Dictionary<string, List<(int Id, List<int> Phases)>> ZoneFestivalCache = [];

    readonly string[] FestivalLgbNames = ["bg", "planevent"];

    public TerritoryType? Get(uint ID) => _dataManager.GetExcelSheet<TerritoryType>().GetRowOrDefault(ID);
    public string? GetBG(uint ID) => Get(ID)?.Bg.ToString();

    private ReadOnlyDictionary<uint, FestivalEntry> BuildFestivalList(GameDataProvider gameDataProvider)
    {
        var festivals = new Dictionary<uint, FestivalEntry>();

        //foreach(var x in _dataManager.GetExcelSheet<TerritoryType>())
        //{
        //    var bg = ((TerritoryType?)x).Value.Bg.ToString();
        //    if(!bg.IsNullOrEmpty())
        //    {
        //        if(!BGPathToTerritoryType.TryGetValue(bg, out var list))
        //        {
        //            list.name = x.PlaceName.Value.Name.ToString() ?? "";
        //            list.ids = [];

        //            BGPathToTerritoryType[bg] = list;
        //        }
        //        list.ids.Add(x.RowId);
        //    }
        //}

        //foreach(var lvbPath in BGPathToTerritoryType)
        //{
        //    var typs = string.Join(", ", lvbPath.Value.ids);
        //    Brio.Log.Verbose($"[{lvbPath.Value.name}] Path {lvbPath.Key} has territory types {typs}");

        //    var zoneFests = GetZoneFestivals(lvbPath.Key);

        //    if(zoneFests.Count == 0)
        //    {
        //        Brio.Log.Verbose($"No festivals found for bg [{lvbPath.Value.name}] (Territories: {typs})");
        //    }

        //    foreach(var (festivalId, festivalPhases) in zoneFests)
        //    {
        //        Brio.Log.Warning($"[{lvbPath.Value.name}] ({lvbPath.Key})\n -- Festival {festivalId} with phases {string.Join(", ", festivalPhases)}");
        //    }
        //}


        StringBuilder sb = new StringBuilder();


        // vfx/common/eff/%s.avfx
        sb.AppendLine("Loaded VFX locations:");
        foreach(var item in _dataManager.GetExcelSheet<Lumina.Excel.Sheets.VFX>())
        {
            //sb.AppendLine($"ID: {item.RowId} Location: {item.Location}");
        }

        // vfx/channeling/eff/%s.avfx
        // vfx/channeling/eff/chn_y6d3_launch0g2.avfx
        sb.AppendLine("Loaded Channeling locations:");
        foreach(var item in _dataManager.GetExcelSheet<Channeling>())
        {
            //sb.AppendLine($"ID: {item.RowId} File: {item.File} - {item.Unknown0} - {item.Unknown1} - {item.Unknown2} - {item.Unknown_70}");
        }

        sb.AppendLine("Loaded Lockon locations:");
        foreach(var item in _dataManager.GetExcelSheet<Lockon>())
        {
            //sb.AppendLine($"ID: {item.RowId} IconName: {item.IconName.ToString()} - {item.Unknown1} ");
        }

        // 
        // vfx/omen/eff/%s.avfx
        // vfx/omen/eff/general_trialaser_o0p.avfx
        sb.AppendLine("Loaded Lockon Omen:");
        foreach(var item in _dataManager.GetExcelSheet<Omen>())
        {
            //sb.AppendLine($"ID: {item.RowId} Path: {item.Path} [{item.PathAlly}]- {item.Type} - u0 {item.Unknown0} - ls {item.LargeScale} - rs {item.RestrictYScale} ");
        }

        //Brio.Log.Warning(sb.ToString());

        var knownEntries = _resourceProvider.GetResourceDocument<List<FestivalFileEntry>>("Data.Festivals.json");

        foreach(var (_, row) in gameDataProvider.Festivals)
        {
            if(row.RowId == 0)
                continue;

            //LgbFile
            unsafe
            {
                //EventFramework.Instance()->GetContentDirector();
                //ExdModule.Addresses.GetEnabledZoneSharedGroupRequirementIndex
                //ZoneSharedGroup
                // ZoneSharedGroupManager
            }

            Brio.Log.Verbose($"Processing festival {row.RowId} {row.Name} {row.Unknown1} {row.Unknown0}");

            var fileEntry = knownEntries.FirstOrDefault(x => x.Id == row.RowId);
            if(fileEntry != null)
            {
                festivals.Add(row.RowId, new FestivalEntry
                {
                    Id = fileEntry.Id,
                    Name = fileEntry.Name,
                    Unsafe = fileEntry.Unsafe,
                    Unknown = false,
                    AreaExclusion = fileEntry.AreaExclusion,
                    KnownPhases = fileEntry.Phases
                });
            }
            else
            {
                festivals.Add(row.RowId, new FestivalEntry
                {
                    Id = row.RowId,
                    Unsafe = false,
                    Unknown = true,
                    AreaExclusion = null
                });
            }
        }

        return festivals.AsReadOnly();
    }

    private void OnGPoseStateChanged(bool newState)
    {
        if(!newState)
        {
            ResetFestivals(false);
        }
    }

    public List<(int Id, List<int> PhaseIds)> GetZoneFestivals(string bg)
    {
        if(bg.IsNullOrEmpty()) return [];
        if(!ZoneFestivalCache.TryGetValue(bg, out var cached))
        {
            var festIdToPhaseId = new SortedDictionary<int, SortedSet<int>>();
            try
            {
                var slash = bg.LastIndexOf('/');
                var dir = slash >= 0 ? bg[..slash] : bg;

                foreach(var name in FestivalLgbNames)
                {
                    var lgb = _dataManager.GetFile<LgbFile>($"bg/{dir}/{name}.lgb");

                    if(lgb == null) continue;
                   foreach(var layer in lgb.Layers)
                    {
                        if(layer.FestivalID == 0) continue;

                        if(!festIdToPhaseId.TryGetValue(layer.FestivalID, out var phaseIds))
                            festIdToPhaseId[layer.FestivalID] = phaseIds = [];

                        phaseIds.Add(layer.FestivalPhaseID);
                    }
                }

            }
            catch(Exception e)
            {
                Brio.Log.Error(e, $"Error loading festival data for bg {bg}");
            }
            ZoneFestivalCache[bg] = cached = [.. festIdToPhaseId.Select(kv => (kv.Key, kv.Value.ToList()))];
        }
        return cached;
    }


    private void OnTerritoryChanged(uint obj)
    {
        _pendingChanges.Clear();
        _originalState = null;

        if(false)
        {
            var bg = GetBG(_clientState.TerritoryType);
            var zoneFests = GetZoneFestivals(bg);

            if(zoneFests.Count == 0)
            {
                Brio.Log.Warning($"No festivals found for bg {bg} (TerritoryType {_clientState.TerritoryType})");

            }
            foreach(var (festivalId, festivalPhases) in zoneFests)
            {
                Brio.Log.Information($"Festival {festivalId} with phases {string.Join(", ", festivalPhases)} for bg {bg}");
            }
        }
    }

    public override void Dispose()
    {
        ResetFestivals(true);

        base.Dispose();

        GC.SuppressFinalize(this);
    }

    private class FestivalFileEntry
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Unsafe { get; set; }

        public List<FestivalPhase>? Phases { get; set; } = null;
        public FestivalAreaExclusion? AreaExclusion { get; set; }
    }

    public class FestivalPhase
    {
        public int Id;
        public string Name = "";
    }

    public class FestivalAreaExclusion
    {
        public string Reason { get; set; } = string.Empty;
        public ushort TerritoryType { get; set; }
        public FestivalAreaExclusionBoundary[] Polygon { get; set; } = [];
    }

    public class FestivalAreaExclusionBoundary
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2 AsVector2() => new(X, Y);
    }

    public class FestivalEntry
    {
        public uint Id { get; set; }
        public uint Phase { get; set; }
        public string Name { get; set; } = "Unknown";
        public bool Unknown { get; set; }
        public bool Unsafe { get; set; }

        public List<FestivalPhase>? KnownPhases = [];
        public FestivalAreaExclusion? AreaExclusion { get; set; }

        public override string ToString()
        {
            return $"{(Unsafe ? "(UNSAFE) " : string.Empty)}{Name}";
        }
    }
}
