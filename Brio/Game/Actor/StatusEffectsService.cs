using Brio.Core;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;

namespace Brio.Game.Actor;
public class StatusEffectsService : ServiceBase<StatusEffectsService>
{
    private Dictionary<ushort, Status> _statusList = new();

    public ReadOnlyDictionary<ushort, Status> StatusTable => new(_statusList);

    public override void Start()
    {
        UpdateStatusList();
        base.Start();
    }

    private void UpdateStatusList()
    {
        var statusSheet = Dalamud.DataManager.GetExcelSheet<Status>();
        if(statusSheet != null)
            _statusList = statusSheet.Where((i) => !string.IsNullOrEmpty(i.Name)).ToDictionary((i) => (ushort) i.RowId);
    }

    public unsafe List<Status> GetAllEffects(BattleChara* battleChara)
    {
        List<Status> list = new List<Status>();
        const int maxEffects = 30;

        for(var i = 0; i < maxEffects; i++)
        {
            var effect = (ushort)battleChara->StatusManager.GetStatusId(i);
            if(effect != 0 && _statusList.ContainsKey(effect))
                list.Add(_statusList[effect]);
        }

        return list;
    }

    public override void Stop()
    {
        _statusList.Clear();
    }
}
