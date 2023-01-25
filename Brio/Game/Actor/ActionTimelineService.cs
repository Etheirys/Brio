using Brio.Core;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;

namespace Brio.Game.Actor;
public class ActionTimelineService : ServiceBase<ActionTimelineService>
{
    private Dictionary<int, BaseOverrideEntry> _actorBaseOverrides = new();
    private Dictionary<nint, SpeedOverrideEntry> _actorSpeedOverrides = new();

    private delegate void SetSlotSpeedDelegate(IntPtr a1, uint slot, float speed);
    private Hook<SetSlotSpeedDelegate> _setSpeedSlotHook = null!;

    public ActionTimelineService()
    {
        _setSpeedSlotHook = Hook<SetSlotSpeedDelegate>.FromAddress((nint)ActionTimelineDriver.Addresses.SetSlotSpeed.Value, SetSlotSpeedDetour);
    }

    public override void Start()
    {
        ActorService.Instance.OnActorDestructing += Instance_OnActorDestructing;
        GPoseService.Instance.OnGPoseStateChange += Instance_OnGPoseStateChange;

        Instance_OnGPoseStateChange(GPoseService.Instance.GPoseState);
        base.Start();
    }

    private void Instance_OnGPoseStateChange(GPoseState gposeState)
    {
        if(gposeState == GPoseState.Inside)
            _setSpeedSlotHook.Enable();

        if(gposeState == GPoseState.Exiting)
            _setSpeedSlotHook.Disable();
    }

    public unsafe void ApplyBaseOverride(Character managedChara, ushort actionTimeline, bool interrupt)
    {
        var chara = managedChara.AsNative();
        var index = chara->GameObject.ObjectIndex;
        
        if(!_actorBaseOverrides.ContainsKey(index))
        {
            _actorBaseOverrides[index] = new BaseOverrideEntry
            {
                OriginalMode = (byte)chara->Mode,
                OriginalModeParam = (byte)chara->ModeParam
            };
        }

        chara->SetMode(FFXIVClientStructs.FFXIV.Client.Game.Character.Character.CharacterModes.AnimLock, 0);
        chara->ActionTimelineManager.BaseOverride = actionTimeline;

        if(interrupt)
            chara->ActionTimelineManager.Driver.PlayTimeline(actionTimeline);
    }

    public bool HasOverride(Character managedChara) => _actorBaseOverrides.ContainsKey(managedChara.GetObjectIndex());

    public unsafe void RemoveOverride(Character managedChara)
    {
        var index = managedChara.GetObjectIndex();
        if(_actorBaseOverrides.TryGetValue(index, out BaseOverrideEntry? entry))
        {
            var chara = managedChara.AsNative();
            chara->ActionTimelineManager.BaseOverride = 0;
            chara->ActionTimelineManager.Driver.TimelineIds[0] = 0;
            chara->SetMode((FFXIVClientStructs.FFXIV.Client.Game.Character.Character.CharacterModes)entry.OriginalMode, entry.OriginalModeParam);
        }
    }

    public unsafe SpeedOverrideEntry GetSpeedOverride(Character managedChara)
    {
        var addr = new IntPtr(&managedChara.AsNative()->ActionTimelineManager.Driver);
        if(!_actorSpeedOverrides.ContainsKey(addr))
        {
            var entry = new SpeedOverrideEntry();

            for(int i = 0; i < ActionTimelineDriver.TimelineSlotCount; ++i)
                entry.SlotModifiers[i] = 1f;

            _actorSpeedOverrides[addr] = entry;

            return entry;

        }

        return _actorSpeedOverrides[addr];
    }

    public unsafe void Blend(Character managedChara, ushort actionTimeline) => managedChara.AsNative()->ActionTimelineManager.Driver.PlayTimeline(actionTimeline);

    public unsafe void UpdateAllSlots(Character chara)
    {
        var addr = new IntPtr(&chara.AsNative()->ActionTimelineManager.Driver);
        if(_actorSpeedOverrides.ContainsKey(addr))
        {
            var entry = _actorSpeedOverrides[addr];
            for(uint i = 0; i < ActionTimelineDriver.TimelineSlotCount; ++i)
            {
                _setSpeedSlotHook.Original(addr, i, entry.SlotModifiers[i] * entry.SpeedMultiplier);
            }

            return;
        }
    }

    private unsafe void Instance_OnActorDestructing(GameObject gameObject)
    {
        _actorBaseOverrides.Remove(gameObject.GetObjectIndex());

        if(gameObject is Character chara)
        {
            var addr = new IntPtr(&chara.AsNative()->ActionTimelineManager.Driver);
            _actorSpeedOverrides.Remove(addr);
        }
    }

    private unsafe void SetSlotSpeedDetour(IntPtr a1, uint slot, float speed)
    {
        if(_actorSpeedOverrides.ContainsKey(a1))
        {
            var entry = _actorSpeedOverrides[a1];
            _setSpeedSlotHook.Original(a1, slot, entry.SlotModifiers[slot] * entry.SpeedMultiplier);

            return;
        }

        _setSpeedSlotHook.Original(a1, slot, speed);
    }

    public override void Stop()
    {
        ActorService.Instance.OnActorDestructing -= Instance_OnActorDestructing;
        GPoseService.Instance.OnGPoseStateChange -= Instance_OnGPoseStateChange;
    }

    public override void Dispose()
    {
        _setSpeedSlotHook.Dispose();
    }

    public class BaseOverrideEntry
    {
        public byte OriginalMode { get; set; }
        public byte OriginalModeParam { get; set;  }
    }

    public class SpeedOverrideEntry
    {
        public float SpeedMultiplier = 1.0f;
        public float[] SlotModifiers = new float[ActionTimelineDriver.TimelineSlotCount];

        public float GetEffectiveSpeed(ActionTimelineSlots slot) => SpeedMultiplier * SlotModifiers[(int)slot];
    }
}
