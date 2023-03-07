using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using System;
using System.Runtime.InteropServices;

namespace Brio.Game.World.Interop;

// TODO: Move into ClientStructs
// Tracking: https://github.com/aers/FFXIVClientStructs/pull/310 & https://github.com/aers/FFXIVClientStructs/pull/311

[StructLayout(LayoutKind.Explicit, Size = 0x98)]
public unsafe struct LayoutManagerStruct
{
    [FieldOffset(0x38)] public uint FestivalStatus; // SetActiveFestivals will not allow a change when not 5 or 0
    [FieldOffset(0x40)] public fixed uint ActiveFestivals[4];
}

public unsafe class LayoutManagerInterop
{
    private delegate void SetActiveFestivalsDelegate(LayoutManagerStruct* instance, uint* festivalArray);

    [Signature("E8 ?? ?? ?? ?? 8B C5 EB 6A", ScanType = ScanType.Text)]
    private SetActiveFestivalsDelegate _setActiveFestivals = null!;

    public LayoutManagerInterop()
    {
        SignatureHelper.Initialise(this);
    }

    public unsafe void SetActiveFestivals(uint* festivalArray)
    {
        var world = LayoutWorld.Instance();
        if(world != null)
        {
            var manager = (LayoutManagerStruct*)world->ActiveLayout;
            if(manager != null)
            {
                _setActiveFestivals(manager, festivalArray);
            }
        }
    }

    public uint[] GetActiveFestivals()
    {
        var result = new uint[4];

        var world = LayoutWorld.Instance();
        if(world != null)
        {
            var manager = (LayoutManagerStruct*)world->ActiveLayout;
            if(manager != null)
            {
                for(int i = 0; i < 4; ++i)
                {
                    result[i] = manager->ActiveFestivals[i];
                }
            }
        }

        return result;
    }

    public bool IsBusy
    {
        get
        {
            var world = LayoutWorld.Instance();
            if(world != null)
            {
                var manager = (LayoutManagerStruct*)world->ActiveLayout;
                if(manager != null)
                {
                    return manager->FestivalStatus != 0 && manager->FestivalStatus != 5;
                }
            }

            return true;
        }
    }
}

public unsafe class GameMainInterop
{
    private delegate void SetActiveFestivalsDelegate(GameMain* instance, uint festival1, uint festival2, uint festival3, uint festival4);

    [Signature("E8 ?? ?? ?? ?? 80 63 50 FE", ScanType = ScanType.Text)]
    private SetActiveFestivalsDelegate _setActiveFestivals = null!;
    public void SetActiveFestivals(uint festival1, uint festival2, uint festival3, uint festival4) => _setActiveFestivals(GameMain.Instance(), festival1, festival2, festival3, festival4);

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 8B 44 24 60 48 8D 8D", ScanType = ScanType.Text)]
    private SetActiveFestivalsDelegate _queueActiveFestivals = null!;
    public void QueueActiveFestivals(uint festival1, uint festival2, uint festival3, uint festival4) => _queueActiveFestivals(GameMain.Instance(), festival1, festival2, festival3, festival4);


    public GameMainInterop()
    {
        SignatureHelper.Initialise(this);
    }
}
