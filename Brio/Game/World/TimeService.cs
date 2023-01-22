using Brio.Core;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;

namespace Brio.Game.World;
public class TimeService : ServiceBase<TimeService>
{
    public bool TimeOverrideEnabled
    {
        get => _updateEorzeaTimeHook.IsEnabled;
        set {

            if(value != TimeOverrideEnabled)
            {
                if(value)
                {
                    _updateEorzeaTimeHook.Enable();
                }
                else
                {
                    _updateEorzeaTimeHook.Disable();
                }
            }
        }
    }

    public unsafe long EorzeaTime { 
        get
        {
            var framework = Framework.Instance();
            if(framework == null) return 0;
            return framework->IsEorzeaTimeOverridden ? framework->EorzeaTimeOverride : framework->EorzeaTime;
        }    

        set
        {
            var framework = Framework.Instance();
            if(framework == null) return;
            framework->EorzeaTime = value;
            if(framework->IsEorzeaTimeOverridden)
                framework->EorzeaTimeOverride = value;
        }
    }

    private delegate void UpdateEorzeaTimeDelegate(IntPtr a1, IntPtr a2);
    private Hook<UpdateEorzeaTimeDelegate> _updateEorzeaTimeHook = null!;

    public TimeService()
    {
        var etAddress = Dalamud.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B F9 48 8B DA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C");
        _updateEorzeaTimeHook = Hook<UpdateEorzeaTimeDelegate>.FromAddress(etAddress, UpdateEorzeaTime);
    }

    public override void Dispose()
    {
        _updateEorzeaTimeHook?.Dispose();
        base.Dispose();
    }


    internal unsafe static void UpdateEorzeaTime(IntPtr a1, IntPtr a2)
    {
        // DO NOTHING
        // UpdateEorzeaTimeHook.Original(a1, a2);
    }
}
