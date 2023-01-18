using Brio.Game.GPose;
using Dalamud.Hooking;
using System;

namespace Brio.Game.Render;

public unsafe class RenderHooks : IDisposable
{
    public bool ApplyNPCOverride { get; set; } = false;

    private delegate long EnforceKindRestrictionsDelegate(void* a1, void* a2);
    private Hook<EnforceKindRestrictionsDelegate> EnforceKindRestrictionsHook = null!;

    public RenderHooks()
    {
        var enforceKindRestrictionsAddress = Dalamud.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 B0 ?? 48 8B D3 48 8B CD");
        EnforceKindRestrictionsHook = Hook<EnforceKindRestrictionsDelegate>.FromAddress(enforceKindRestrictionsAddress, EnforceKindRestrictionsDetour);

        EnforceKindRestrictionsHook.Enable();

        Brio.GPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;

        GPoseService_OnGPoseStateChange(Brio.GPoseService.GPoseState);
    }

    private void GPoseService_OnGPoseStateChange(GPoseState state)
    {
        var npcOverrideBehavior = Brio.Configuration.ApplyNPCHack;
        if (npcOverrideBehavior == Config.ApplyNPCHack.InGPose)
        {
            ApplyNPCOverride = state == GPoseState.Inside;
        }
    }

    private long EnforceKindRestrictionsDetour(void* a1, void* a2)
    {
        if (ApplyNPCOverride || Brio.Configuration.ApplyNPCHack == Config.ApplyNPCHack.Always)
            return 0;

        return EnforceKindRestrictionsHook.Original(a1, a2);
    }

    public void Dispose()
    {
        EnforceKindRestrictionsHook.Dispose();
    }
}
