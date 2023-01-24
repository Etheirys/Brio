using StatusManager = FFXIVClientStructs.FFXIV.Client.Game.StatusManager;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace Brio.Game.Actor.Extensions;
public static class StatusManagerExtensions
{
    public static List<Status> GetAllStatuses(this ref StatusManager sm)
    {
        List<Status> list = new List<Status>();
        const int maxEffects = 30;

        var statusSheet = Dalamud.DataManager.GetExcelSheet<Status>();

        for(var i = 0; i < maxEffects; i++)
        {
            var effect = (ushort)sm.GetStatusId(i);
            if(effect != 0)
            {
                var statusEntry = statusSheet?.GetRow(effect);
                if(statusEntry != null)
                    list.Add(statusEntry);
            }
        }
        return list;
    }
}
