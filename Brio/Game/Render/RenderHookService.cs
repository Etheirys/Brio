using Brio.Config;
using Brio.Core;
using Brio.Game.GPose;
using Dalamud.Hooking;

namespace Brio.Game.Render;

public unsafe class RenderHookService : ServiceBase<RenderHookService>
{
    private delegate long EnforceKindRestrictionsDelegate(void* a1, void* a2);
    private Hook<EnforceKindRestrictionsDelegate> _enforceKindRestrictionsHook = null!;
    private uint _forceNpcHackCount = 0;

    public RenderHookService()
    {
        var enforceKindRestrictionsAddress = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 B0 ?? 48 8B D3 48 8B CD");
        _enforceKindRestrictionsHook = Hook<EnforceKindRestrictionsDelegate>.FromAddress(enforceKindRestrictionsAddress, EnforceKindRestrictionsDetour);
        _enforceKindRestrictionsHook.Enable();
    }

    public void PushForceNpcHack() => ++_forceNpcHackCount;
    public void PopForceNpcHack()
    {
        if(_forceNpcHackCount == 0)
            throw new System.Exception("Invalid _forceNpcHack count (is already 0)");

        --_forceNpcHackCount;
    }

    private long EnforceKindRestrictionsDetour(void* a1, void* a2)
    {
        if(_forceNpcHackCount > 0)
            return 0;

        if(ConfigService.Configuration.ApplyNPCHack == ApplyNPCHack.Always)
            return 0;

        if(ConfigService.Configuration.ApplyNPCHack == ApplyNPCHack.InGPose && GPoseService.Instance.IsInGPose)
            return 0;

        return _enforceKindRestrictionsHook.Original(a1, a2);
    }

    public override void Dispose()
    {
        _enforceKindRestrictionsHook.Dispose();
    }
}
