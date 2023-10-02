using Brio.Core;
using Brio.Game.GPose;
using Brio.Game.World.Interop;
using Brio.Utils;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;

namespace Brio.Game.World;
public class FestivalService : ServiceBase<FestivalService>
{
    public const int MaxFestivals = 4;

    public ReadOnlyCollection<FestivalEntry> FestivalEntries => new(_festivalEntries);

    public ReadOnlyCollection<FestivalEntry> ActiveFestivals
    {
        get
        {
            var result = new List<FestivalEntry>();
            var active = _festivalInterop.GetActiveFestivals();
            for(int idx = 0; idx < MaxFestivals; ++idx)
            {
                if(active[idx] != 0)
                {
                    var entry = _festivalEntries.FirstOrDefault(i => i.Id == active[idx]);
                    if(entry != null)
                    {
                        result.Add(entry);
                    }
                    else
                    {
                        result.Add(new FestivalEntry { Id = active[idx], Unknown = true, Name = "Unknown" });
                    }
                }
            }
            return new(result);
        }
    }

    public bool HasMoreSlots => _festivalInterop.GetActiveFestivals().Count(i => i == 0) > 0;
    public bool IsOverridden => _originalState != null;

    private List<FestivalEntry> _festivalEntries = new();

    private LayoutManagerInterop _festivalInterop = new();
    private GameMainInterop _gameMainInterop = new();


    private Queue<uint[]?> _pendingChanges = new();
    private uint[]? _originalState;

    public override void Start()
    {
        UpdateFestivalList();
        GPoseService.Instance.OnGPoseStateChange += Instance_OnGPoseStateChange;
        Dalamud.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        base.Start();
    }

    public bool AddFestival(uint festival)
    {
        if(!CheckFestivalRestrictions(festival))
            return false;

        var active = _festivalInterop.GetActiveFestivals();
        var copy = active.ToArray();

        for(int i = 0; i < MaxFestivals; ++i)
        {
            if(active[i] == 0)
            {
                SnapshotFestivals();
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

        var active = _festivalInterop.GetActiveFestivals();
        var copy = active.ToArray();

        for(int i = 0; i < MaxFestivals; ++i)
        {
            if(active[i] == festival)
            {
                SnapshotFestivals();
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
                InternalApply(_originalState, false);
            }
            else
            {
                _pendingChanges.Enqueue(_originalState.ToArray());
            }

            _originalState = null;
        }
    }

    public unsafe override void Tick(float delta)
    {
        if(_pendingChanges.Count > 0 && !_festivalInterop.IsBusy)
        {
            var pending = _pendingChanges.Dequeue();
            if(pending != null)
                InternalApply(pending);
        }
    }

    private void SnapshotFestivals()
    {
        if(_originalState == null)
            _originalState = _festivalInterop.GetActiveFestivals().ToArray();
    }

    private unsafe void InternalApply(uint[] festivals, bool applyNow = true)
    {
        if(applyNow)
        {
            _gameMainInterop.SetActiveFestivals(festivals[0], festivals[1], festivals[2], festivals[3]);
        }
        else
        {
            _gameMainInterop.QueueActiveFestivals(festivals[0], festivals[1], festivals[2], festivals[3]);
        }
    }

    private void UpdateFestivalList()
    {
        _festivalEntries.Clear();

        List<FestivalFileEntry> knownEntries = new();

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Brio.Resources.Festivals.json");
        if(stream != null)
        {
            using var reader = new StreamReader(stream);
            var txt = reader.ReadToEnd();
            knownEntries = JsonSerializer.Deserialize<List<FestivalFileEntry>>(txt) ?? knownEntries;
        }


        var festivalSheet = Dalamud.DataManager.GetExcelSheet<Festival>();
        if(festivalSheet == null)
            return;

        foreach(var festival in festivalSheet)
        {
            var knownEntry = knownEntries.SingleOrDefault((i) => i.Id == festival.RowId);
            if(knownEntry != null)
            {
                _festivalEntries.Add(new FestivalEntry
                {
                    Id = knownEntry.Id,
                    Name = knownEntry.Name,
                    Unsafe = knownEntry.Unsafe,
                    AreaExclusion = knownEntry.AreaExclusion,
                    Unknown = false
                });
            }
            else
            {
                _festivalEntries.Add(new FestivalEntry
                {
                    Id = (ushort)festival.RowId,
                    Name = "Unknown",
                    Unsafe = false,
                    Unknown = true
                });
            }
        }
    }


    private bool CheckFestivalRestrictions(uint festivalId)
    {
        var festival = _festivalEntries.SingleOrDefault(i => i.Id == festivalId);
        if(festival == null) return true;

        if(festival.AreaExclusion != null)
        {
            if(Dalamud.ClientState.TerritoryType == festival.AreaExclusion.TerritoryType)
            {
                var localPlayer = Dalamud.ClientState.LocalPlayer;
                if(localPlayer == null)
                    return false;

                var playerPosition = new Vector2(localPlayer.Position.X, localPlayer.Position.Z);
                var polygon = festival.AreaExclusion.Polygon.Select(i => i.AsVector2()).ToArray();
                if(playerPosition.IsPointInPolygon(polygon))
                {
                    Dalamud.ToastGui.ShowError($"Unable to apply festival here.\nReason: {festival.AreaExclusion.Reason}");
                    return false;
                }

            }
        }

        return true;
    }

    private void Instance_OnGPoseStateChange(GPoseState gposeState)
    {
        if(gposeState == GPoseState.Exiting)
        {
            ResetFestivals();
        }
    }

    private void ClientState_TerritoryChanged(ushort e)
    {
        _pendingChanges.Clear();
        _originalState = null;
    }

    public override void Stop()
    {
        ResetFestivals(true);
        GPoseService.Instance.OnGPoseStateChange -= Instance_OnGPoseStateChange;
        Dalamud.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
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
        public FestivalAreaExclusionBoundary[] Polygon { get; set; } = new FestivalAreaExclusionBoundary[0];
    }

    public class FestivalAreaExclusionBoundary
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2 AsVector2() => new Vector2(X, Y);
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
