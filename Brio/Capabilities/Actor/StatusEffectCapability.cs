﻿using Brio.Entities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Resources;
using Brio.UI.Widgets.Actor;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Brio.Capabilities.Actor;

public class StatusEffectCapability : ActorCapability
{
    public IBattleChara Character { get; }

    public StatusEffectCapability(ActorEntity parent, IBattleChara chara) : base(parent)
    {
        Character = chara;

        Widget = new StatusEffectsWidget(this);
    }

    public static StatusEffectCapability? CreateIfEligible(IServiceProvider provider, ActorEntity entity)
    {
        if(entity.GameObject is IBattleChara character)
            return ActivatorUtilities.CreateInstance<StatusEffectCapability>(provider, entity, character);

        return null;
    }

    public unsafe IEnumerable<Status> ActiveStatuses => Character.GetStatusManager()->GetAllStatuses();

    public unsafe void RemoveStatus(Status status) => RemoveStatus((ushort)status.RowId);

    public unsafe void RemoveStatus(ushort status)
    {
        var statusManager = Character.GetStatusManager();
        var idx = statusManager->GetStatusIndex(status);
        if(idx != -1)
            statusManager->RemoveStatus(idx);
    }

    public unsafe void AddStatus(Status status) => AddStatus((ushort)status.RowId);

    public unsafe void AddStatus(ushort status)
    {
        Character.GetStatusManager()->AddStatus(status);
    }

    public Status? GetStatus(uint rowId)
    {
        return GameDataProvider.Instance.Statuses.TryGetValue(rowId, out var status) ? status : null;
    }
}
