using Brio.Config;
using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.World;

public class WorldRenderingService : IDisposable
{

    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;

    public bool IsWaterFrozen
    {
        get => _updateWaterRendererHook.IsEnabled;
        set
        {
            if(value != IsWaterFrozen)
            {
                if(value)
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

    public WorldRenderingService(ISigScanner scanner, IGameInteropProvider hooking, GPoseService gPoseService, ConfigurationService configurationService)
    {
        _gPoseService = gPoseService;
        _configurationService = configurationService;

        var uwrAddress = scanner.ScanText("48 8B C4 48 89 58 ?? 57 48 81 EC ?? ?? ?? ?? 0F B6 B9");
        _updateWaterRendererHook = hooking.HookFromAddress<UpdateWaterRendererDelegate>(uwrAddress, UpdateWaterRenderer);


        _gPoseService.OnGPoseStateChange += OnGPoseStateChanged;
    }

    private void OnGPoseStateChanged(bool newState)
    {
        if(!newState && _configurationService.Configuration.Environment.ResetWaterOnGPoseExit)
        {
            IsWaterFrozen = false;
        }
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChanged;
        _updateWaterRendererHook?.Dispose();
    }

    public nint UpdateWaterRenderer(IntPtr a1)
    {
        return 0;
    }
}
