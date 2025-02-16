using Brio.Capabilities.Actor;
using Brio.Entities;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;

namespace Brio.Game.Actor;

public unsafe class ActionTimelineService : IDisposable
{
    public enum ActionTimelineSlots : int
    {
        Base = 0,
        UpperBody = 1,
        Facial = 2,
        Add = 3,
        // 4-6 unknown purpose
        Lips = 7,
        Parts1 = 8,
        Parts2 = 9,
        Parts3 = 10,
        Parts4 = 11,
        Overlay = 12
    }

    private delegate bool CalculateAndApplyOverallSpeedDelegate(TimelineContainer* a1);
    private readonly Hook<CalculateAndApplyOverallSpeedDelegate> _calculateAndApplyOverallSpeedHook = null!;

    private delegate void SetSlotSpeedDelegate(ActionTimelineSequencer* a1, ActionTimelineSlots slot, float speed);
    private readonly Hook<SetSlotSpeedDelegate> _setSpeedSlotHook = null!;

    private readonly EntityManager _entityManager;

    public ActionTimelineService(EntityManager entityManager, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;

        var calculateAndApplyAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 8B 01 FF 50 ?? 48 8D 8B ?? ?? ?? ?? 48 8B 01 FF 50 ?? F6 83");
        _calculateAndApplyOverallSpeedHook = hooking.HookFromAddress<CalculateAndApplyOverallSpeedDelegate>(calculateAndApplyAddress, CalculateAndApplyOverallSpeedDetour);
        _calculateAndApplyOverallSpeedHook.Enable();

        _setSpeedSlotHook = hooking.HookFromAddress<SetSlotSpeedDelegate>(ActionTimelineSequencer.Addresses.SetSlotSpeed.Value, SetSlotSpeedDetour);
        _setSpeedSlotHook.Enable();
    }

    private bool CalculateAndApplyOverallSpeedDetour(TimelineContainer* a1)
    {
        bool result = _calculateAndApplyOverallSpeedHook.Original(a1);
        if(_entityManager.TryGetEntity(a1->OwnerObject, out var entity))
        {
            if(entity.TryGetCapability<ActionTimelineCapability>(out var atc))
            {
                if(atc.SpeedMultiplierOverride.HasValue)
                {
                    a1->OverallSpeed = atc.SpeedMultiplierOverride.Value;
                    result |= true;
                }

                if(atc.CheckAndResetDirtySlots())
                    result |= true;
            }

        }

        return result;
    }

    private unsafe void SetSlotSpeedDetour(ActionTimelineSequencer* a1, ActionTimelineSlots slot, float speed)
    {
        float finalSpeed = speed;

        var owner = a1->Parent;

        if(_entityManager.TryGetEntity(owner, out var entity))
        {
            if(entity.TryGetCapability<ActionTimelineCapability>(out var atc))
            {
                if(atc.HasSlotSpeedOverride(slot))
                {
                    finalSpeed = atc.GetSlotSpeed(slot);
                }
            }

        }
        _setSpeedSlotHook.Original(a1, slot, finalSpeed);
    }

    public void Dispose()
    {
        _calculateAndApplyOverallSpeedHook.Dispose();
        _setSpeedSlotHook.Dispose();
    }
}
