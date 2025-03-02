using Brio.Resources;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using StatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager;

namespace Brio.Game.Actor.Extensions;

public static class StatusManagerExtensions
{
    public static List<Status> GetAllStatuses(this ref StatusManager sm)
    {
        List<Status> list = [];

        for(var i = 0; i < sm.Status.Length; i++)
        {
            var effect = (ushort)sm.GetStatusId(i);
            if(effect != 0)
                if(GameDataProvider.Instance.Statuses.TryGetValue(effect, out var status))
                    list.Add(status);


        }
        return list;
    }

    public unsafe static StatusManager* GetStatusManager(this IBattleChara battleChara) => battleChara.Native()->Character.GetStatusManager();
}
