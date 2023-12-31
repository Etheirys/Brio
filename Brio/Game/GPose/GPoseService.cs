using Brio.Config;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Game.GPose;

internal unsafe class GPoseService : IDisposable
{
    public bool IsGPosing => _isInFakeGPose || _isInGPose;

    public delegate void OnGPoseStateDelegate(bool newState);
    public event OnGPoseStateDelegate? OnGPoseStateChange;

    public bool FakeGPose
    {
        get => _isInFakeGPose;
        set
        {
            if (_isInFakeGPose == value)
                return;

            _isInFakeGPose = value;

            TriggerGPoseChange();
        }
    }

    private bool _isInGPose = false;
    private bool _isInFakeGPose = false;

    private delegate bool GPoseEnterExitDelegate(UIModule* uiModule);
    private delegate void ExitGPoseDelegate(UIModule* uiModule);
    private readonly Hook<GPoseEnterExitDelegate> _enterGPoseHook;
    private readonly Hook<ExitGPoseDelegate> _exitGPoseHook;

    private delegate nint MouseHoverDelegate(nint a1, nint a2, nint a3);
    private readonly Hook<MouseHoverDelegate> _mouseHoverHook = null!;

    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly ConfigurationService _configService;

    public GPoseService(IFramework framework, IClientState clientState, ConfigurationService configService, IGameInteropProvider interopProvider, ISigScanner scanner)
    {
        _framework = framework;
        _clientState = clientState;
        _configService = configService;

        _isInGPose = _clientState.IsGPosing;

        UIModule* uiModule = Framework.Instance()->GetUiModule();
        var enterGPoseAddress = (nint)uiModule->VTable->EnterGPose;
        var exitGPoseAddress = (nint)uiModule->VTable->ExitGPose;

        _enterGPoseHook = interopProvider.HookFromAddress<GPoseEnterExitDelegate>(enterGPoseAddress, EnteringGPoseDetour);
        _enterGPoseHook.Enable();

        _exitGPoseHook = interopProvider.HookFromAddress<ExitGPoseDelegate>(exitGPoseAddress, ExitingGPoseDetour);
        _exitGPoseHook.Enable();

        var mouseHoverAddr = "40 57 48 83 EC ?? 48 89 6C 24 ?? 48 8B F9 48 89 74 24 ?? 49 8B E8 4C 89 74 24";
        _mouseHoverHook = interopProvider.HookFromAddress<MouseHoverDelegate>(scanner.ScanText(mouseHoverAddr), GPoseMouseEventDetour);

        _framework.Update += OnFrameworkUpdate;

        UpdateDynamicHooks();
    }

    public void TriggerGPoseChange()
    {
        var gposing = IsGPosing;
        Brio.Log.Verbose($"GPose state changed to {gposing}");
        OnGPoseStateChange?.Invoke(gposing);
    }

    public void AddCharacterToGPose(Character chara) => AddCharacterToGPose((NativeCharacter*)chara.Address);

    public void AddCharacterToGPose(NativeCharacter* chara)
    {
        if (!IsGPosing)
            return;

        var ef = EventFramework.Instance();
        if (ef == null)
            return;

        ef->EventSceneModule.EventGPoseController.AddCharacterToGPose(chara);

    }

    private void ExitingGPoseDetour(UIModule* uiModule)
    {
        _exitGPoseHook.Original.Invoke(uiModule);
        HandleGPoseStateChange(false);
    }

    private bool EnteringGPoseDetour(UIModule* uiModule)
    {
        bool didEnter = _enterGPoseHook.Original.Invoke(uiModule);

        HandleGPoseStateChange(didEnter);

        return didEnter;
    }

    private nint GPoseMouseEventDetour(nint a1, nint a2, nint a3)
    {
        if (_configService.Configuration.Posing.DisableGPoseMouseSelect)
            return 0;

        return _mouseHoverHook.Original(a1, a2, a3);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        // Only detect if we got snapped out
        if (!_clientState.IsGPosing && _isInGPose)
            HandleGPoseStateChange(_clientState.IsGPosing);
    }

    private void HandleGPoseStateChange(bool newState)
    {
        if (IsGPosing == newState || _isInFakeGPose)
            return;

        _isInGPose = newState;

        UpdateDynamicHooks();

        TriggerGPoseChange();
    }

    private void UpdateDynamicHooks()
    {
        if (IsGPosing)
        {
            if (!_mouseHoverHook.IsEnabled)
                _mouseHoverHook.Enable();

        }
        else
        {
            if (_mouseHoverHook.IsEnabled)
                _mouseHoverHook.Disable();
        }
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;

        _enterGPoseHook.Dispose();
        _exitGPoseHook.Dispose();
        _mouseHoverHook.Dispose();
    }
}

