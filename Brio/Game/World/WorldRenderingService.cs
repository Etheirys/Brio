using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.World;

internal class WorldRenderingService : IDisposable
{
    
    private readonly GPoseService _gPoseService;
    
    public bool IsWaterFrozen
    {
        get => _updateWaterRendererHook.IsEnabled;
        set
        {
            if (value != IsWaterFrozen)
            {
                if (value)
                {
                    _updateWaterRendererHook.Enable();
                }
                else
                {
                    _updateWaterRendererHook.Disable();
                }
            }
        }
    }

    private delegate nint UpdateWaterRendererDelegate(nint a1);
    private readonly Hook<UpdateWaterRendererDelegate> _updateWaterRendererHook = null!;
    
    public WorldRenderingService(ISigScanner scanner, IGameInteropProvider hooking, GPoseService gPoseService)
    {
        _gPoseService = gPoseService;
        
        var uwrAddress = scanner.ScanText("48 8B C4 48 89 58 18 57 48 81 EC ?? ?? ?? ?? 0F 29 70 E8 48 8B D9");
        _updateWaterRendererHook = hooking.HookFromAddress<UpdateWaterRendererDelegate>(uwrAddress, UpdateWaterRenderer);
        
        
        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
    }

    private void OnGPoseStateChanged(bool newState)
    {
        if (!newState)
        {
            IsWaterFrozen = false;
        }
    }
    
    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;
        _updateWaterRendererHook?.Dispose();
    }
    
    internal nint UpdateWaterRenderer(IntPtr a1)
    {
        return 0;
    }
}
