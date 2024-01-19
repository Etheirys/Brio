using Brio.Entities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Types;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Capabilities.Actor;

internal class ActionTimelineCapability : ActorCharacterCapability
{
    public unsafe float SpeedMultiplier => SpeedMultiplierOverride ?? Character.Native()->ActionTimelineManager.OverallSpeed;
    public bool HasSpeedMultiplierOverride => SpeedMultiplierOverride.HasValue;
    public float? SpeedMultiplierOverride { get; private set; }

    private readonly Dictionary<ActionTimelineSlots, float> _actionTimelineSlotSpeedOverrides = [];

    public unsafe ushort LipsOverride
    {
        get => Character.Native()->ActionTimelineManager.LipsOverride;
        set => Character.Native()->ActionTimelineManager.SetLipsOverrideTimeline(value);
    }

    private bool _slotsDirty = false;

    private OriginalBaseAnimation? _originalBaseAnimation = null;

    public ActionTimelineCapability(ActorEntity parent) : base(parent)
    {
        Widget = new ActionTimelineWidget(this);
    }

    public void SetOverallSpeedOverride(float speed)
    {
        SpeedMultiplierOverride = speed;
    }

    public void ResetOverallSpeedOverride()
    {
        SpeedMultiplierOverride = null;
    }

    public unsafe ActionTimelineUnion GetSlotAction(ActionTimelineSlots slot)
    {
        var timeline = Character.Native()->ActionTimelineManager.Driver.TimelineIds[(int)slot];
        return new ActionTimelineId(timeline);
    }

    public unsafe float GetSlotSpeed(ActionTimelineSlots slot)
    {
        if(_actionTimelineSlotSpeedOverrides.TryGetValue(slot, out float speed))
            return speed;

        return Character.Native()->ActionTimelineManager.Driver.TimelineSpeeds[(int)slot];
    }

    public void SetSlotSpeedOverride(ActionTimelineSlots slot, float speed)
    {
        _actionTimelineSlotSpeedOverrides[slot] = speed;
        _slotsDirty = true;
    }

    public bool HasSlotSpeedOverride(ActionTimelineSlots slot)
    {
        return _actionTimelineSlotSpeedOverrides.ContainsKey(slot);
    }

    public void ResetSlotSpeedOverride(ActionTimelineSlots slot)
    {
        _actionTimelineSlotSpeedOverrides.Remove(slot);
        _slotsDirty = true;
    }

    public bool CheckAndResetDirtySlots() => _slotsDirty && !(_slotsDirty = false);

    public unsafe void ApplyBaseOverride(ushort actionTimeline, bool interrupt)
    {
        if(_originalBaseAnimation == null)
            _originalBaseAnimation = new(Character.Native()->EventState, Character.Native()->ModeParam, Character.Native()->ActionTimelineManager.BaseOverride);

        var chara = Character.Native();

        chara->SetMode(CharacterModes.AnimLock, 0);
        chara->ActionTimelineManager.BaseOverride = actionTimeline;

        if(interrupt)
            BlendTimeline(actionTimeline);
    }

    public unsafe void ResetBaseOverride()
    {
        if(_originalBaseAnimation == null)
            return;

        var chara = Character.Native();

        chara->ActionTimelineManager.BaseOverride = _originalBaseAnimation.Value.OriginalTimeline;
        chara->EventState = _originalBaseAnimation.Value.OriginalMode;
        chara->ModeParam = _originalBaseAnimation.Value.OriginalInput;

        _originalBaseAnimation = null;

        BlendTimeline(3);
    }

    public bool HasBaseOverride => _originalBaseAnimation != null;

    public unsafe void BlendTimeline(ushort actionTimeline)
    {
        Character.Native()->ActionTimelineManager.Driver.PlayTimeline(actionTimeline);
    }

    public override void Dispose()
    {
        SpeedMultiplierOverride = null;
        _actionTimelineSlotSpeedOverrides.Clear();
        ResetBaseOverride();

        base.Dispose();
    }

    public static ActionTimelineCapability? CreateIfEligible(IServiceProvider provider, ActorEntity entity)
    {
        if(entity.GameObject is Character)
            return ActivatorUtilities.CreateInstance<ActionTimelineCapability>(provider, entity);

        return null;
    }

    public record struct OriginalBaseAnimation(byte OriginalMode, byte OriginalInput, ushort OriginalTimeline);
}
