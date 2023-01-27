using Brio.Core;
using Brio.Game.GPose;
using Brio.Utils;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Brio.Game.World;
public class FestivalService : ServiceBase<FestivalService>
{
    // TODO: Submit the layout changes back to ClientStructs

    public ReadOnlyCollection<FestivalEntry> FestivalEntries => new(_festivalEntries);
    public bool IsOverriden => _originalFestival != null;

    private List<FestivalEntry> _festivalEntries = new();

    private Queue<ushort> _queuedTransitions = new();
    private ushort? _originalFestival;

    private unsafe delegate* unmanaged<LayoutManager*, FestivalArgs, void> _updateFestival;

    public FestivalEntry CurrentFestival
    {
        get
        {
            var currentId = CurrentFestivalId;
            return _festivalEntries.First(i => i.Id == currentId);
        }
    }

    public unsafe ushort CurrentFestivalId
    {
        get
        {
            var layoutWorld = LayoutWorld.Instance();
            if(layoutWorld != null)
            {
                var layoutManager = layoutWorld->ActiveLayout;

                if(layoutManager != null)
                {
                    return *(ushort*)(((nint)layoutManager) + 0x40);
                }
            }

            return 0;
        }
    }

    public unsafe FestivalService()
    {
        var updateFestivalAddress = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B C5 EB 6A");
        _updateFestival = (delegate* unmanaged<LayoutManager*, FestivalArgs, void>)updateFestivalAddress;
    }

    public override void Start()
    {
        UpdateFestivalList();
        GPoseService.Instance.OnGPoseStateChange += Instance_OnGPoseStateChange;
        Dalamud.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        base.Start();
    }

    public unsafe override void Tick()
    {
        if(_queuedTransitions.Count == 0)
            return;

        var layoutWorld = LayoutWorld.Instance();
        if(layoutWorld != null )
        {
            var layoutManager = layoutWorld->ActiveLayout;

            if( layoutManager != null )
            {
                byte status = *(byte*)(((nint)layoutManager) + 0x38);
                if(status != 0 && status != 5)
                    return;

                var next = _queuedTransitions.Dequeue();
                SetFestivalImmediately(next);
            }
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

    public void SetFestivalOverride(ushort festivalId)
    {
        if(!CheckFestivalRestrictions(festivalId))
            return;

        if(_originalFestival == null)
            _originalFestival = CurrentFestivalId;

        _queuedTransitions.Enqueue(festivalId);
    }

    public void ResetFestivalOverride()
    {
        if(_originalFestival != null)
        {
            SetFestivalOverride((ushort) _originalFestival);
            _originalFestival = null;
        }
    }

    private unsafe void SetFestivalImmediately(ushort festivalId)
    {
        var layoutWorld = LayoutWorld.Instance();
        if(layoutWorld != null)
        {
            var layoutManager = layoutWorld->ActiveLayout;

            if(layoutManager != null)
            {
                var layout = new FestivalArgs
                {
                    FestivalId = festivalId
                };


                _updateFestival(layoutManager, layout);
            }
        }
    }

    private bool CheckFestivalRestrictions(ushort festivalId)
    {
        var festival = _festivalEntries.SingleOrDefault(i => i.Id == festivalId);
        if(festival == null) return false;

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
            ResetFestivalOverride();
        }
    }

    private void ClientState_TerritoryChanged(object? sender, ushort e)
    {
        _queuedTransitions.Clear();
        _originalFestival = null;
    }

    public override void Stop()
    {
        GPoseService.Instance.OnGPoseStateChange -= Instance_OnGPoseStateChange;
        Dalamud.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;

        if(_originalFestival != null && CurrentFestivalId != _originalFestival)
            SetFestivalImmediately((ushort) _originalFestival);

    }

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    private struct FestivalArgs
    {
        [FieldOffset(0x0)] public uint FestivalId;
        [FieldOffset(0x4)] public uint a1;
        [FieldOffset(0x8)] public uint a2;
        [FieldOffset(0xC)] public uint a3;
    }

    private class FestivalFileEntry
    {
        public ushort Id { get; set; }
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
        public ushort Id { get; set; }
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
