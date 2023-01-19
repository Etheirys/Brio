using Brio.Config;
using Brio.Core;
using Brio.Game.GPose;
using Dalamud.Hooking;
using System;

namespace Brio.Game.Render;

public unsafe class RenderHookService : ServiceBase<RenderHookService>
{
    public bool ApplyNPCOverride { get; set; } = false;

    private delegate long EnforceKindRestrictionsDelegate(void* a1, void* a2);
    private Hook<EnforceKindRestrictionsDelegate> EnforceKindRestrictionsHook = null!;

    public override void Start()
    {
        var enforceKindRestrictionsAddress = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 B0 ?? 48 8B D3 48 8B CD");
        EnforceKindRestrictionsHook = Hook<EnforceKindRestrictionsDelegate>.FromAddress(enforceKindRestrictionsAddress, EnforceKindRestrictionsDetour);

        EnforceKindRestrictionsHook.Enable();

        GPoseService.Instance.OnGPoseStateChange += GPoseService_OnGPoseStateChange;

        GPoseService_OnGPoseStateChange(GPoseService.Instance.GPoseState);

        base.Start();
    }

    private void GPoseService_OnGPoseStateChange(GPoseState state)
    {
        var npcOverrideBehavior = ConfigService.Configuration.ApplyNPCHack;
        if (npcOverrideBehavior == Config.ApplyNPCHack.InGPose)
        {
            ApplyNPCOverride = state == GPoseState.Inside;
        }
    }

    private long EnforceKindRestrictionsDetour(void* a1, void* a2)
    {
        if (ApplyNPCOverride || ConfigService.Configuration.ApplyNPCHack == Config.ApplyNPCHack.Always)
            return 0;

        return EnforceKindRestrictionsHook.Original(a1, a2);
    }

    public override void Dispose()
    {
        EnforceKindRestrictionsHook.Dispose();
    }
}
