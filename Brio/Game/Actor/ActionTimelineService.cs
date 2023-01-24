using Brio.Core;
using Brio.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using System.Collections.Generic;

namespace Brio.Game.Actor;
public class ActionTimelineService : ServiceBase<ActionTimelineService>
{
    private Dictionary<int, OverrideEntry> _actorOverrides = new();

    public override void Start()
    {
        ActorService.Instance.OnActorDestructing += Instance_OnActorDestructing;
        base.Start();
    }

    public unsafe void ApplyBaseOverride(Character managedChara, ushort actionTimeline, bool interrupt)
    {
        var chara = managedChara.AsNative();
        var index = chara->GameObject.ObjectIndex;
        
        if(!_actorOverrides.ContainsKey(index))
        {
            _actorOverrides[index] = new OverrideEntry
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

    public bool HasOverride(Character managedChara) => _actorOverrides.ContainsKey(managedChara.GetObjectIndex());

    public unsafe void RemoveOverride(Character managedChara)
    {
        var index = managedChara.GetObjectIndex();
        if(_actorOverrides.TryGetValue(index, out OverrideEntry? entry))
        {
            var chara = managedChara.AsNative();
            chara->ActionTimelineManager.BaseOverride = 0;
            chara->ActionTimelineManager.Driver.TimelineIds[0] = 0;
            chara->SetMode((FFXIVClientStructs.FFXIV.Client.Game.Character.Character.CharacterModes)entry.OriginalMode, entry.OriginalModeParam);
        }
    }

    public unsafe void Blend(Character managedChara, ushort actionTimeline) => managedChara.AsNative()->ActionTimelineManager.Driver.PlayTimeline(actionTimeline);

    private void Instance_OnActorDestructing(GameObject gameObject)
    {
        _actorOverrides.Remove(gameObject.GetObjectIndex());
    }

    public override void Stop()
    {
        ActorService.Instance.OnActorDestructing -= Instance_OnActorDestructing;
    }

    public class OverrideEntry
    {
        public byte OriginalMode { get; set; }
        public byte OriginalModeParam { get; set;  }
    }
}
