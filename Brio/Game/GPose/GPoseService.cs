using Brio.Core;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace Brio.Game.GPose;

public class GPoseService : ServiceBase<GPoseService>
{
    public GPoseState GPoseState { get; private set; }
    public bool IsInGPose => GPoseState == GPoseState.Inside;
    public bool FakeGPose
    {
        get => _fakeGPose;

        set
        {
            if(value != _fakeGPose)
            {
                if(!value)
                {
                    _fakeGPose = false;
                    HandleGPoseChange(GPoseState.Exiting);
                    HandleGPoseChange(GPoseState.Outside);
                }
                else
                {
                    HandleGPoseChange(GPoseState.Inside);
                    _fakeGPose = true;
                }
            }
        }
    }

    private bool _fakeGPose = false;

    public delegate void OnGPoseStateDelegate(GPoseState gposeState);
    public event OnGPoseStateDelegate? OnGPoseStateChange;

    private delegate void ExitGPoseDelegate(IntPtr addr);
    private Hook<ExitGPoseDelegate>? _exitGPoseHook = null;

    private delegate bool EnterGPoseDelegate(IntPtr addr);
    private Hook<EnterGPoseDelegate>? _enterGPoseHook = null;

    public override unsafe void Start()
    {
        GPoseState = Dalamud.ClientState.IsGPosing ? GPoseState.Inside : GPoseState.Outside;

        UIModule* uiModule = Framework.Instance()->GetUiModule();
        var enterGPoseAddress = (nint)uiModule->VTable->EnterGPose;
        var exitGPoseAddress = (nint)uiModule->VTable->ExitGPose;

        _enterGPoseHook = Dalamud.GameInteropProvider.HookFromAddress<EnterGPoseDelegate>(enterGPoseAddress, EnteringGPoseDetour);
        _enterGPoseHook.Enable();

        _exitGPoseHook = Dalamud.GameInteropProvider.HookFromAddress< ExitGPoseDelegate>(exitGPoseAddress, ExitingGPoseDetour);
        _exitGPoseHook.Enable();

        base.Start();
    }

    private void ExitingGPoseDetour(IntPtr addr)
    {
        if(HandleGPoseChange(GPoseState.AttemptExit))
        {
            HandleGPoseChange(GPoseState.Exiting);
            _exitGPoseHook!.Original.Invoke(addr);
            HandleGPoseChange(GPoseState.Outside);
        }
    }

    private bool EnteringGPoseDetour(IntPtr addr)
    {
        bool didEnter = _enterGPoseHook!.Original.Invoke(addr);
        if(didEnter)
        {
            _fakeGPose = false;
            HandleGPoseChange(GPoseState.Inside);
        }

        return didEnter;
    }

    private bool HandleGPoseChange(GPoseState state)
    {
        if(state == GPoseState || _fakeGPose)
            return true;

        GPoseState = state;

        try
        {
            OnGPoseStateChange?.Invoke(state);
        }
        catch(Exception e)
        {
            Dalamud.ToastGui.ShowError($"Brio GPose transition error.\n Reason: {e.Message}");
            return false;
        }

        return true;
    }

    public override void Tick(float delta)
    {
        if(!Dalamud.ClientState.IsGPosing && IsInGPose)
        {
            HandleGPoseChange(GPoseState.Exiting);
            HandleGPoseChange(GPoseState.Outside);
        }
    }

    public override void Dispose()
    {
        _exitGPoseHook?.Dispose();
        _enterGPoseHook?.Dispose();
    }
}

public enum GPoseState
{
    Inside,
    AttemptExit,
    Exiting,
    Outside
}
