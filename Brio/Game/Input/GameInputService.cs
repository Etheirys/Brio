using Brio.Config;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.Input;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

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

    //private unsafe delegate void inputDeviceDelegate(nint arg1);
    //private readonly Hook<inputDeviceDelegate> _inputHook = null!;

    //

    int _freeW = 0;
    int _freeA = 0;
    int _freeS = 0;
    int _freeD = 0;

    int _undo = 0;
    int _redo = 0;

    //

    public unsafe GameInputService(GPoseService gPoseService, ConfigurationService configurationService, VirtualCameraManager virtualCameraService, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _gPoseService = gPoseService;
        _virtualCameraService = virtualCameraService;
        _configurationService = configurationService;

        // This sometimes still leaks input to the game, as the game can still capture input in certain situations, especially modifier keys 
        var inputHandleSig = "E8 ?? ?? ?? ?? ?? 8B ?? ?? ?? ?? 8B 87 ?? ?? ?? ?? 89 45";
        _handleInputHook = hooking.HookFromAddress<HandleInputDelegate>(scanner.ScanText(inputHandleSig), HandleInputDetour);
        _handleInputHook.Enable();

        // This crashes as there are two matches 
        //var inputDeviceSig = "40 57 48 83 ec 50 48 89 6c 24 68 48 8b f9 48 89 74 24 70 ba 1e 00 00 00 4c 89 74 24 48 4c 89 7c 24 40";
        //_inputHook = hooking.HookFromAddress<inputDeviceDelegate>(scanner.ScanText(inputHandleSig), InputDetour);

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;

        UpdateKeys();
    }

    private void OnConfigurationChanged()
    {
        UpdateKeys();
    }

    public void UpdateKeys()
    {
        var keyBindings = _configurationService.Configuration.InputManager.KeyBindings;

        _freeW = (int)keyBindings[InputAction.FreeCamera_Forward].Key;
        _freeA = (int)keyBindings[InputAction.FreeCamera_Backward].Key;
        _freeS = (int)keyBindings[InputAction.FreeCamera_Left].Key;
        _freeD = (int)keyBindings[InputAction.FreeCamera_Right].Key;

        _undo = (int)keyBindings[InputAction.Posing_Undo].Key;
        _redo = (int)keyBindings[InputAction.Posing_Redo].Key;
    }

    //public unsafe void InputDetour(nint arg1)
    //{
    //    _inputHook.Original(arg1);
    //}

    public unsafe void HandleInputDetour(IntPtr arg1, IntPtr arg2, IntPtr arg3, MouseFrame* mouseFrame, KeyboardFrame* keyboardFrame)
    {
        // This is a hot path, all of the games input flows through here 

        _handleInputHook.Original(arg1, arg2, arg3, mouseFrame, keyboardFrame);

        if(_gPoseService.IsGPosing is true && !RaptureAtkModule.Instance()->AtkModule.IsTextInputActive())
        {
            bool ctrlPressed = keyboardFrame->KeyState[17] == 1;
            bool shiftPressed = keyboardFrame->KeyState[16] == 1;
            bool altPressed = keyboardFrame->KeyState[18] == 1;

            bool anyModPressed = ctrlPressed || shiftPressed || altPressed;

            if(_configurationService.Configuration.InputManager.EnableConsumeAllInput)
            {
                for(int i = 0; i < KeyboardFrame.KeyStateLength; i++)
                {
                    if(i == 27) // VirtualKey.ESCAPE
                        continue;
                    if(i == 13) // VirtualKey.RETURN
                        continue;

                    keyboardFrame->KeyState[i] = 0;
                }
            }

            if(_configurationService.Configuration.InputManager.Enable)
            {
                if(anyModPressed)
                {
                    foreach(var binding in _configurationService.Configuration.InputManager.KeyBindings.Values)
                    {
                        if(binding.Key == Dalamud.Game.ClientState.Keys.VirtualKey.NO_KEY)
                            continue;

                        if(binding.RequireCtrl || binding.RequireShift || binding.RequireAlt)
                        {
                            keyboardFrame->KeyState[(int)binding.Key] = 0;
                        }
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
                        keyboardFrame->KeyState[81] = 0; // VirtualKey.Q
                        keyboardFrame->KeyState[69] = 0; // VirtualKey.E

                        keyboardFrame->KeyState[32] = 0; // SPACE
                        keyboardFrame->KeyState[16] = 0; // SHIFT
                        keyboardFrame->KeyState[17] = 0; // Ctrl
                        keyboardFrame->KeyState[18] = 0; // Alt
                    }
                }
            }

            if(AllowEscape is false)
            {
                keyboardFrame->KeyState[27] = 0; // ESCAPE
            }
        }
    }

    public void Dispose()
    {
        _handleInputHook.Dispose();
        //_inputHook.Dispose();

        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;

        GC.SuppressFinalize(this);
    }
}
