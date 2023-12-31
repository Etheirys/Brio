using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Brio.Capabilities.Actor;
using Brio.Entities;
using System;

namespace Brio.Game.Actor;

internal unsafe class ActionTimelineService : IDisposable
{
    private delegate bool CalculateAndApplyOverallSpeedDelegate(ActionTimelineManager* a1);
    private readonly Hook<CalculateAndApplyOverallSpeedDelegate> _calculateAndApplyOverallSpeedHook = null!;

    private delegate void SetSlotSpeedDelegate(ActionTimelineDriver* a1, ActionTimelineSlots slot, float speed);
    private readonly Hook<SetSlotSpeedDelegate> _setSpeedSlotHook = null!;

    private readonly EntityManager _entityManager;

    public ActionTimelineService(EntityManager entityManager, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;

        _calculateAndApplyOverallSpeedHook = hooking.HookFromAddress<CalculateAndApplyOverallSpeedDelegate>((nint)ActionTimelineManager.Addresses.CalculateAndApplyOverallSpeed.Value, CalculateAndApplyOverallSpeedDetour);
        _calculateAndApplyOverallSpeedHook.Enable();

        _setSpeedSlotHook = hooking.HookFromAddress<SetSlotSpeedDelegate>((nint)ActionTimelineDriver.Addresses.SetSlotSpeed.Value, SetSlotSpeedDetour);
        _setSpeedSlotHook.Enable();
    }

    private bool CalculateAndApplyOverallSpeedDetour(ActionTimelineManager* a1)
    {
        bool result = _calculateAndApplyOverallSpeedHook.Original(a1);
        if (_entityManager.TryGetEntity(a1->Parent, out var entity))
        {
            if (entity.TryGetCapability<ActionTimelineCapability>(out var atc))
            {
                if (atc.SpeedMultiplierOverride.HasValue)
                {
                    a1->OverallSpeed = atc.SpeedMultiplierOverride.Value;
                    result |= true;
                }

                if (atc.CheckAndResetDirtySlots())
                    result |= true;
            }

        }

        return result;
    }

    private unsafe void SetSlotSpeedDetour(ActionTimelineDriver* a1, ActionTimelineSlots slot, float speed)
    {
        float finalSpeed = speed;

        var owner = a1->Parent;

        if (_entityManager.TryGetEntity(owner, out var entity))
        {
            if (entity.TryGetCapability<ActionTimelineCapability>(out var atc))
            {
                if (atc.HasSlotSpeedOverride(slot))
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
