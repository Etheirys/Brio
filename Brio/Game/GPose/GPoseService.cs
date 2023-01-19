using Brio.Config;
using Brio.Core;
using Brio.UI;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;

namespace Brio.Game.GPose;

public class GPoseService : ServiceBase<GPoseService>
{
    public GPoseState GPoseState { get; private set; }
    public bool IsInGPose => GPoseState == GPoseState.Inside || FakeGPose;
    public bool FakeGPose { get; set; } = false;

    public delegate void OnGPoseStateDelegate(GPoseState gposeState);
    public event OnGPoseStateDelegate? OnGPoseStateChange;

    private delegate void ExitGPoseDelegate(IntPtr addr);
    private Hook<ExitGPoseDelegate> ExitGPoseHook = null!;

    private delegate bool EnterGPoseDelegate(IntPtr addr);
    private Hook<EnterGPoseDelegate> EnterGPoseHook = null!;

    public override unsafe void Start()
    {
        GPoseState = Dalamud.PluginInterface.UiBuilder.GposeActive ? GPoseState.Inside: GPoseState.Outside;

        var framework = Framework.Instance();

        EnterGPoseHook = Hook<EnterGPoseDelegate>.FromAddress((nint)framework->UIModule->vfunc[75], EnteringGPoseDetour);
        EnterGPoseHook.Enable();

        ExitGPoseHook = Hook<ExitGPoseDelegate>.FromAddress((nint)framework->UIModule->vfunc[76], ExitingGPoseDetour);
        ExitGPoseHook.Enable();

        base.Start();
    }

    private void ExitingGPoseDetour(IntPtr addr)
    {
        HandleGPoseChange(GPoseState.Exiting);
        ExitGPoseHook.Original.Invoke(addr);
        HandleGPoseChange(GPoseState.Outside);
    }

    private bool EnteringGPoseDetour(IntPtr addr)
    {
        bool didEnter = EnterGPoseHook.Original.Invoke(addr);
        if(didEnter)
            HandleGPoseChange(GPoseState.Inside);

        return didEnter;
    }

    private void HandleGPoseChange(GPoseState state)
    {
        GPoseState = state;
        OnGPoseStateChange?.Invoke(state);

        switch(state)
        {
            case GPoseState.Inside:
            case GPoseState.Outside:
                if (ConfigService.Configuration.OpenBrioBehavior == Config.OpenBrioBehavior.OnGPoseEnter)
                    UIService.Instance.MainWindow.IsOpen = state == GPoseState.Inside;
                break;
        }
    }

    public override void Dispose()
    {
        ExitGPoseHook.Dispose();
        EnterGPoseHook.Dispose();
    }
}

public enum GPoseState
{
    Inside,
    Exiting,
    Outside
}
