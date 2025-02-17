using Brio.Core;
using Brio.Game.GPose;
using Brio.Resources;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Game.World;

public unsafe class FestivalService : IDisposable
{
    public const int MaxFestivals = 4;

    public readonly IReadOnlyDictionary<uint, FestivalEntry> FestivalList;

    public bool HasMoreSlots => EngineActiveFestivals.Any();
    public bool HasOverride => _originalState != null;
    public bool CanModify => _gPoseService.IsGPosing;


    private readonly IClientState _clientState;
    private readonly IToastGui _toastGui;
    private readonly IFramework _framework;
    private readonly GPoseService _gPoseService;
    private readonly ResourceProvider _resourceProvider;

    private readonly Queue<uint[]> _pendingChanges = new();
    private uint[]? _originalState;

    // TODO: Handle festival subid properly

    public uint[] EngineActiveFestivals
    {
        get
        {
            uint[] activeFestivals = new uint[4];
            var engineFestivals = GameMain.Instance()->ActiveFestivals;
            for(int i = 0; i < MaxFestivals; i++)
            {
                fixed(GameMain.Festival* festival = &engineFestivals[i])
                    activeFestivals[i] = *((uint*)festival);

            }

            return activeFestivals;
        }
    }

    public FestivalService(IClientState clientState, IToastGui toastGui, GameDataProvider gameDataProvider, IFramework framework, GPoseService gPoseService, ResourceProvider resourceProvider)
    {
        _clientState = clientState;
        _toastGui = toastGui;
        _framework = framework;
        _gPoseService = gPoseService;
        _resourceProvider = resourceProvider;
        FestivalList = BuildFestivalList(gameDataProvider);

        _framework.Update += OnFrameworkUpdate;
        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
        _clientState.TerritoryChanged += OnTerritoryChanged;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var layoutManager = LayoutWorld.Instance()->ActiveLayout;
        if(_pendingChanges.Count > 0 && layoutManager != null && (layoutManager->FestivalStatus == 5 || layoutManager->FestivalStatus == 0))
        {
            var pending = _pendingChanges.Dequeue();
            if(pending != null)
                publicApply(pending);
        }
    }


    public bool AddFestival(uint festival)
    {
        if(!CheckFestivalRestrictions(festival))
            return false;

        var active = EngineActiveFestivals.ToArray();
        var copy = active.ToArray();

        for(int i = 0; i < MaxFestivals; ++i)
        {
            if(active[i] == 0)
            {
                SnapshotFestivalsIfNeeded();
                copy[i] = festival;
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

        var active = EngineActiveFestivals.ToArray();
        var copy = active.ToArray();

        for(int i = 0; i < MaxFestivals; ++i)
        {
            if(active[i] == festival)
            {
                SnapshotFestivalsIfNeeded();
                copy[i] = 0;
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
                publicApply(_originalState, false);
            }
            else
            {
                _pendingChanges.Enqueue(_originalState.ToArray());
            }

            _originalState = null;
        }
    }

    private void publicApply(uint[] festivals, bool applyNow = true)
    {
        if(applyNow)
        {
            GameMain.Instance()->SetActiveFestivals(festivals[0], festivals[1], festivals[2], festivals[3]);
        }
        else
        {
            GameMain.Instance()->QueueActiveFestivals(festivals[0], festivals[1], festivals[2], festivals[3]);
        }
    }

    private void SnapshotFestivalsIfNeeded()
    {
        if(_originalState == null)
            _originalState = EngineActiveFestivals.ToArray();
    }

    private bool CheckFestivalRestrictions(uint festivalId, bool showError = true)
    {
        if(FestivalList.TryGetValue(festivalId, out var festival))
        {
            if(festival.AreaExclusion != null)
            {
                if(_clientState.TerritoryType == festival.AreaExclusion.TerritoryType)
                {
                    var localPlayer = _clientState.LocalPlayer;
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

    private IReadOnlyDictionary<uint, FestivalEntry> BuildFestivalList(GameDataProvider gameDataProvider)
    {
        var festivals = new Dictionary<uint, FestivalEntry>();

        var knownEntries = _resourceProvider.GetResourceDocument<List<FestivalFileEntry>>("Data.Festivals.json");

        foreach(var (_, row) in gameDataProvider.Festivals)
        {
            if(row.RowId == 0)
                continue;

            var fileEntry = knownEntries.FirstOrDefault(x => x.Id == row.RowId);
            if(fileEntry != null)
            {
                festivals.Add(row.RowId, new FestivalEntry
                {
                    Id = fileEntry.Id,
                    Name = fileEntry.Name,
                    Unsafe = fileEntry.Unsafe,
                    Unknown = false,
                    AreaExclusion = fileEntry.AreaExclusion
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

    private void OnTerritoryChanged(ushort obj)
    {
        _pendingChanges.Clear();
        _originalState = null;
    }


    public void Dispose()
    {
        ResetFestivals(true);
        _framework.Update -= OnFrameworkUpdate;
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;
        _clientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private class FestivalFileEntry
    {
        public uint Id { get; set; }
        public string Name { get; set; } = null!;
        public bool Unsafe { get; set; }
        public FestivalAreaExclusion? AreaExclusion { get; set; }
    }

    public class FestivalAreaExclusion
    {
        public string Reason { get; set; } = string.Empty;
        public ushort TerritoryType { get; set; }
        public FestivalAreaExclusionBoundary[] Polygon { get; set; } = Array.Empty<FestivalAreaExclusionBoundary>();
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
        public string Name { get; set; } = "Unknown";
        public bool Unknown { get; set; }
        public bool Unsafe { get; set; }
        public FestivalAreaExclusion? AreaExclusion { get; set; }

        public override string ToString()
        {
            return $"{(Unsafe ? "(UNSAFE) " : string.Empty)}{Name} ({Id})";
        }
    }
}
