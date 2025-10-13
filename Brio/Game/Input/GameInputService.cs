using Brio.Config;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Input;
using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;

namespace Brio.Game.Input;

public class GameInputService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly VirtualCameraManager _virtualCameraService;
    private readonly ConfigurationService _configurationService;

    public bool AllowEscape { get; set; } = true;
    public bool HandleAllKeys { get; set; } = false;
    public bool HandleAllMouse { get; set; } = false;

    private unsafe delegate void HandleInputDelegate(IntPtr arg1, IntPtr arg2, IntPtr arg3, MouseFrame* mouseState, KeyboardFrame* keyboardState);
    private readonly Hook<HandleInputDelegate> _handleInputHook = null!;

    //

    int _freeW = 0;
    int _freeA = 0;
    int _freeS = 0;
    int _freeD = 0;

    int _undo = 0;
    int _redo = 0;

    bool _requireAlt;
    bool _requireCtrl;
    bool _requireShift;

    bool _requireMod => _requireAlt || _requireCtrl || _requireShift;

    //

    public unsafe GameInputService(GPoseService gPoseService, ConfigurationService configurationService, VirtualCameraManager virtualCameraService, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _gPoseService = gPoseService;
        _virtualCameraService = virtualCameraService;
        _configurationService = configurationService;

        var inputHandleSig = "E8 ?? ?? ?? ?? ?? 8B ?? ?? ?? ?? 8B 87 ?? ?? ?? ?? 89 45";
        _handleInputHook = hooking.HookFromAddress<HandleInputDelegate>(scanner.ScanText(inputHandleSig), HandleInputDetour);
        _handleInputHook.Enable();

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;

        UpdateKeys();
    }

    private void OnConfigurationChanged()
    {
        UpdateKeys();
    }

    public void UpdateKeys()
    {
        // This is fine, we don't detect whether or not there are modifier keys pressed,
        // but do we have too? Is anyone really going to use undo/redo without a modifier? (I mean, one will right?..) I dont wanna build that. don't make me
        //

        var keyBindings = _configurationService.Configuration.InputManager.KeyBindings;

        _freeW = (int)keyBindings[InputAction.FreeCamera_Forward].Key;
        _freeA = (int)keyBindings[InputAction.FreeCamera_Backward].Key;
        _freeS = (int)keyBindings[InputAction.FreeCamera_Left].Key;
        _freeD = (int)keyBindings[InputAction.FreeCamera_Right].Key;

        _undo = (int)keyBindings[InputAction.Posing_Undo].Key;
        _redo = (int)keyBindings[InputAction.Posing_Redo].Key;

        _requireShift |= keyBindings[InputAction.Posing_Undo].RequireShift;
        _requireCtrl |= keyBindings[InputAction.Posing_Undo].RequireCtrl;
        _requireAlt |= keyBindings[InputAction.Posing_Undo].RequireAlt;

        _requireShift |= keyBindings[InputAction.Posing_Redo].RequireShift;
        _requireCtrl |= keyBindings[InputAction.Posing_Redo].RequireCtrl;
        _requireAlt |= keyBindings[InputAction.Posing_Redo].RequireAlt;
    }

    public unsafe void HandleInputDetour(IntPtr arg1, IntPtr arg2, IntPtr arg3, MouseFrame* mouseFrame, KeyboardFrame* keyboardFrame)
    {
        // This is a hot path, all of the games input flows through here 

        _handleInputHook.Original(arg1, arg2, arg3, mouseFrame, keyboardFrame);

        if(_gPoseService.IsGPosing is false)
            return;

        if(RaptureAtkModule.Instance()->AtkModule.IsTextInputActive())
            return;

        if(_configurationService.Configuration.InputManager.EnableConsumeAllInput)
        {
            if(_virtualCameraService.CurrentCamera?.IsFreeCamera is true)
                _virtualCameraService.Update(mouseFrame);

            for(int i = 0; i < KeyboardFrame.KeyStateLength; i++)
            {
                if(i == 27) // VirtualKey.ESCAPE
                    continue;
                if(i == 13) // VirtualKey.RETURN
                    continue;

                keyboardFrame->KeyState[i] = 0;
            }
        }
        else if (_configurationService.Configuration.InputManager.Enable)
        {
            if(_requireMod)
            {
                if(_requireCtrl && keyboardFrame->KeyState[17] == 1)            // Ctrl 
                {
                    keyboardFrame->KeyState[_undo] = 0;
                    keyboardFrame->KeyState[_redo] = 0;
                }
                else if(_requireShift && keyboardFrame->KeyState[16] == 1)      // SHIFT
                {
                    keyboardFrame->KeyState[_undo] = 0;
                    keyboardFrame->KeyState[_redo] = 0;
                }
                else if(_requireAlt && keyboardFrame->KeyState[18] == 1)        // Alt
                {
                    keyboardFrame->KeyState[_undo] = 0;
                    keyboardFrame->KeyState[_redo] = 0;
                }
            }

            if(_virtualCameraService.CurrentCamera?.IsFreeCamera is true)
            {
                _virtualCameraService.Update(mouseFrame);

                keyboardFrame->KeyState[_freeW] = 0;
                keyboardFrame->KeyState[_freeA] = 0;
                keyboardFrame->KeyState[_freeS] = 0;
                keyboardFrame->KeyState[_freeD] = 0;

                keyboardFrame->KeyState[32] = 0; // SPACE

                if(_virtualCameraService.CurrentCamera.FreeCamValues.IsMovementEnabled &&
                    _configurationService.Configuration.InputManager.EnableKeyHandlingOnKeyMod)
                {
                    keyboardFrame->KeyState[32] = 0; // SPACE
                    keyboardFrame->KeyState[16] = 0; // SHIFT
                    keyboardFrame->KeyState[17] = 0; // Ctrl
                    keyboardFrame->KeyState[18] = 0; // Alt
                }
            }
        }

        if(AllowEscape is false)
        {
            keyboardFrame->KeyState[27] = 0; // SPACE
        }

        //if(HandleAllMouse)
        //{
        //    // TODO: Implement mouse handling logic
        //}
    }

    public void Dispose()
    {
        _handleInputHook.Dispose();

        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;

        GC.SuppressFinalize(this);
    }
}
