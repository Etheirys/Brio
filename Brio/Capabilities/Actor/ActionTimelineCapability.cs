using Brio.Config;
using Brio.Entities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Posing;
using Brio.Game.Types;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using static Brio.Game.Actor.ActionTimelineService;

namespace Brio.Capabilities.Actor;

internal class ActionTimelineCapability : ActorCharacterCapability
{
    public unsafe float SpeedMultiplier => SpeedMultiplierOverride ?? Character.Native()->Timeline.OverallSpeed;
    public bool HasSpeedMultiplierOverride => SpeedMultiplierOverride.HasValue;
    public float? SpeedMultiplierOverride { get; private set; }

    private readonly Dictionary<ActionTimelineSlots, float> _actionTimelineSlotSpeedOverrides = [];

    public unsafe ushort LipsOverride
    {
        get => Character.Native()->Timeline.LipsOverride;
        set => Character.Native()->Timeline.SetLipsOverrideTimeline(value);
    }

    private bool _slotsDirty = false;

    private OriginalBaseAnimation? _originalBaseAnimation = null;

    public ActionTimelineCapability(ActorEntity parent, PhysicsService physicsService, ConfigurationService configService) : base(parent)
    {
        Widget = new ActionTimelineWidget(this, physicsService, configService);
    }

    public unsafe void SetOverallSpeedOverride(float speed)
    {
        SpeedMultiplierOverride = speed;
        Character.Native()->Timeline.OverallSpeed = speed;

    }

    public void ResetOverallSpeedOverride()
    {
        SpeedMultiplierOverride = null;
    }

    public unsafe ActionTimelineUnion GetSlotAction(ActionTimelineSlots slot)
    {
        var timeline = Character.Native()->Timeline.TimelineSequencer.TimelineIds[(int)slot];
        return new ActionTimelineId(timeline);
    }

    public unsafe float GetSlotSpeed(ActionTimelineSlots slot)
    {
        if(_actionTimelineSlotSpeedOverrides.TryGetValue(slot, out float speed))
            return speed;

        return Character.Native()->Timeline.TimelineSequencer.TimelineSpeeds[(int)slot];
    }

    public unsafe void SetSlotSpeedOverride(ActionTimelineSlots slot, float speed)
    {
        _actionTimelineSlotSpeedOverrides[slot] = speed;
        Character.Native()->Timeline.TimelineSequencer.SetSlotSpeed((uint)slot, speed);
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
            _originalBaseAnimation = new(Character.Native()->Mode, Character.Native()->ModeParam, Character.Native()->Timeline.BaseOverride);

        var chara = Character.Native();

        chara->SetMode(CharacterModes.AnimLock, 0);
        chara->Timeline.BaseOverride = actionTimeline;

        if(interrupt)
            BlendTimeline(actionTimeline);
    }

    public unsafe void ResetBaseOverride()
    {
        if(_originalBaseAnimation == null)
            return;

        var chara = Character.Native();

        chara->Timeline.BaseOverride = _originalBaseAnimation.Value.OriginalTimeline;
        chara->Mode = _originalBaseAnimation.Value.OriginalMode;
        chara->ModeParam = _originalBaseAnimation.Value.OriginalInput;

        _originalBaseAnimation = null;

        BlendTimeline(3);
    }

    public bool HasBaseOverride => _originalBaseAnimation != null;

    public unsafe void BlendTimeline(ushort actionTimeline)
    {
        Character.Native()->Timeline.TimelineSequencer.PlayTimeline(actionTimeline);
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
        if(entity.GameObject is ICharacter)
            return ActivatorUtilities.CreateInstance<ActionTimelineCapability>(provider, entity);

        return null;
    }

    public record struct OriginalBaseAnimation(CharacterModes OriginalMode, byte OriginalInput, ushort OriginalTimeline);
}
