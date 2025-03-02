using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.Posing;
using Brio.Game.Types;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using static Brio.Game.Actor.ActionTimelineService;

namespace Brio.Capabilities.Actor;

public class ActionTimelineCapability : ActorCharacterCapability
{
    private readonly IFramework _framework;

    public unsafe float SpeedMultiplier => SpeedMultiplierOverride ?? Character.Native()->Timeline.OverallSpeed;
    public bool HasSpeedMultiplierOverride => SpeedMultiplierOverride.HasValue;
    public float? SpeedMultiplierOverride { get; private set; }
    public bool IsPaused { get; private set; } = false;

    public bool HasOverride => (SlotedBlendAnimation != 0 || SlotedBaseAnimation != 0) && (HasBaseOverride || HasSpeedMultiplierOverride || DoBaseInterrupt is false || LipsOverride > 0);

    public bool DoBaseInterrupt = true;
    public int SlotedBaseAnimation = 0;
    public int SlotedBlendAnimation = 0;

    public unsafe ushort LipsOverride
    {
        get => Character.Native()->Timeline.LipsOverride;
        set => Character.Native()->Timeline.SetLipsOverrideTimeline(value);
    }

    private readonly Dictionary<ActionTimelineSlots, float> _actionTimelineSlotSpeedOverrides = [];
    private OriginalBaseAnimation? _originalBaseAnimation = null;
    private bool _slotsDirty = false;

    public ActionTimelineCapability(IFramework framework, ActorEntity parent, EntityManager entityManager, PhysicsService physicsService, ConfigurationService configService) : base(parent)
    {
        _framework = framework;

        Widget = new ActionTimelineWidget(this, entityManager, physicsService, configService);
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

    public async void StopSpeedAndResetTimeline(Action? postStopAction = null, bool resetSpeedAfterAction = false)
    {
        Brio.Log.Verbose($"StopSpeedAndResetTimeline {postStopAction is not null} {resetSpeedAfterAction}");

        var oldSpeed = SpeedMultiplier;

        SetOverallSpeedOverride(0);

        Brio.Log.Verbose($"SetOverallSpeedOverride {oldSpeed} {SpeedMultiplier}");

        await _framework.RunOnTick(() =>
        {
            unsafe
            {
                var drawObj = Character.Native()->GameObject.DrawObject;
                if(drawObj == null)
                    return;

                if(drawObj->Object.GetObjectType() != ObjectType.CharacterBase)
                    return;

                var charaBase = (CharacterBase*)drawObj;
                if(charaBase->Skeleton == null)
                    return;

                var skeleton = charaBase->Skeleton;
                for(int p = 0; p < skeleton->PartialSkeletonCount; ++p)
                {
                    var partial = &skeleton->PartialSkeletons[p];

                    var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
                    if(animatedSkele == null)
                        continue;

                    for(int c = 0; c < animatedSkele->AnimationControls.Length; ++c)
                    {
                        var control = animatedSkele->AnimationControls[c].Value;
                        if(control == null)
                            continue;

                        var binding = control->hkaAnimationControl.Binding;
                        if(binding.ptr == null)
                            continue;

                        var anim = binding.ptr->Animation.ptr;
                        if(anim == null)
                            continue;

                        if(control->PlaybackSpeed == 0)
                        {
                            control->hkaAnimationControl.LocalTime = 0;
                            Brio.Log.Verbose($"hkaAnimationControl");
                        }
                    }
                }
            }
        }, delayTicks: 4);

        postStopAction?.Invoke();

        Brio.Log.Verbose($"postStopAction Invoke: {postStopAction is not null}");

        if(resetSpeedAfterAction)
        {
            await _framework.RunOnTick(() =>
            {
                SetOverallSpeedOverride(oldSpeed);

                Brio.Log.Verbose($"SetOverallSpeedOverride {SpeedMultiplier}");
            }, delayTicks: 2);
        }
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

    public void Stop()
    {
        if(HasBaseOverride)
        {
            ResetBaseOverride();
            ResetOverallSpeedOverride();
        }
    }

    public void Reset()
    {
        DoBaseInterrupt = true;

        SlotedBaseAnimation = 0;
        SlotedBlendAnimation = 0;
        LipsOverride = 0;

        IsPaused = false;

        ResetBaseOverride();
        ResetOverallSpeedOverride();
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
