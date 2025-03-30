using Brio.Game.Camera;
using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.Input;

public class GameInputService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly VirtualCameraManager _virtualCameraService;

    public bool AllowEscape { get; set; } = true;
    public bool HandleAllKeys { get; set; } = false;
    public bool HandleAllMouse { get; set; } = false;

    private unsafe delegate void HandleInputDelegate(IntPtr arg1, IntPtr arg2, IntPtr arg3, MouseFrame* mouseState, KeyboardFrame* keyboardState);
    private readonly Hook<HandleInputDelegate> _handleInputHook = null!;

    public unsafe GameInputService(GPoseService gPoseService, VirtualCameraManager virtualCameraService, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _gPoseService = gPoseService;
        _virtualCameraService = virtualCameraService;

        var inputHandleSig = "E8 ?? ?? ?? ?? 8B ?? ?? ?? ?? ?? 89 87 6C 35";
        _handleInputHook = hooking.HookFromAddress<HandleInputDelegate>(scanner.ScanText(inputHandleSig), HandleInputDetour);
        _handleInputHook.Enable();
    }

    public unsafe void HandleInputDetour(IntPtr arg1, IntPtr arg2, IntPtr arg3, MouseFrame* mouseFrame, KeyboardFrame* keyboardFrame)
    {
        _handleInputHook.Original(arg1, arg2, arg3, mouseFrame, keyboardFrame);

        if(_gPoseService.IsGPosing == false)
            return;

        if(_virtualCameraService.CurrentCamera?.IsFreeCamera == true)
        {
            _virtualCameraService.Update(mouseFrame);
        }

        if(HandleAllKeys)
        {
            keyboardFrame->HandleAllKeys();
        }
        else if(AllowEscape is false)
        {
            keyboardFrame->HandleKey(VirtualKey.ESCAPE);
        }

        if(HandleAllMouse)
        {
            // TODO: Implement mouse handling logic
        }
    }

    public void Dispose()
    {
        _handleInputHook.Dispose();
    }
}
