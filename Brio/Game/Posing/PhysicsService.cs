
//
// Code found in this file is from and is inspired by,
// Anamnesis (https://github.com/imchillin/Anamnesis)
// And SimpleTweaks (https://github.com/Caraxi/SimpleTweaksPlugin/tree/main) 
//

//
// Thank you Winter!
//

using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.Posing;

public unsafe partial class PhysicsService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly IFramework _framework;

    //private unsafe delegate void HandlePhysicsDelegate(IntPtr arg1, short arg2, IntPtr arg3, char arg4, char arg5);
    //private readonly Hook<HandlePhysicsDelegate> _handlePhysicsDelegate = null!;

    public bool IsFreezeEnabled { get; private set; } = false;

    private readonly nint _freezePhysicsAddress;
    private byte[] _originalPhysicsBytes1 = [];
    private byte[] _originalPhysicsBytes2 = [];

    public PhysicsService(ISigScanner scanner, IFramework framework, GPoseService gPoseService, IGameInteropProvider hooking)
    {
        _gPoseService = gPoseService;
        _framework = framework;

        _framework.Update += OnFrameworkUpdate;

        // This signature is from Anamnesis (https://github.com/imchillin/Anamnesis)
        // Found in AddressService.cs on line 159 - SkeletonFreezePhysics (1/2/3)
        var freezePhysicsAddress = "0F 11 48 10 41 0F 10 44 24 ?? 0F 11 40 20 48 8B 46 28";
        if(!scanner.TryScanText(freezePhysicsAddress, out this._freezePhysicsAddress))
            this._freezePhysicsAddress = 0;

        _originalPhysicsBytes1 = MemoryHelper.ReadRaw(_freezePhysicsAddress, 4);
        _originalPhysicsBytes2 = MemoryHelper.ReadRaw(_freezePhysicsAddress - 0x9, 3);

        //var handlePhysicsSig = "E9 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 41 ?? ?? ?? 4c ?? ?? 30 ?? ?? ?? 41"; // e9 2d e0 09 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 41 0f b6 c0 4c 8b d1 45 8b c8 48 69 c8 20 02 00 00
        //_handlePhysicsDelegate = hooking.HookFromAddress<HandlePhysicsDelegate>(scanner.ScanText(handlePhysicsSig), HandlePhysicsDetour);
        //_handlePhysicsDelegate.Enable();
    }

    //public unsafe void HandlePhysicsDetour(IntPtr arg1, short arg2, IntPtr arg3, char arg4, char arg5)
    //{
    //    if(IsFreezeEnabled)
    //    {
    //        //return;
    //    }

    //    _handlePhysicsDelegate.Original(arg1, arg2, arg3, arg4, arg5);
    //}

    public bool FreezeToggle() => IsFreezeEnabled ? FreezeRevert() : FreezeEnable();

    public bool FreezeRevert()
    {
        ReplaceRaw(_freezePhysicsAddress, _originalPhysicsBytes1);
        ReplaceRaw(_freezePhysicsAddress - 0x9, _originalPhysicsBytes2);

        return IsFreezeEnabled = false;
    }

    public bool FreezeEnable()
    {
        _originalPhysicsBytes1 = ReplaceRaw(_freezePhysicsAddress, [0x90, 0x90, 0x90, 0x90]);
        _originalPhysicsBytes2 = ReplaceRaw(_freezePhysicsAddress - 0x9, [0x90, 0x90, 0x90]);

        return IsFreezeEnabled = true;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if(IsFreezeEnabled && _gPoseService.IsGPosing == false)
        {
            FreezeRevert();
        }
    }

    // From SimpleTweaks (https://github.com/Caraxi/SimpleTweaksPlugin/blob/124523ca0ddbeadec86fd7bea323b66870e1a474/Tweaks/HighResScreenshots.cs)
    private static byte[] ReplaceRaw(nint address, byte[] data)
    {
        var originalBytes = MemoryHelper.ReadRaw(address, data.Length);
        var oldProtection = MemoryHelper.ChangePermission(address, data.Length, MemoryProtection.ExecuteReadWrite);
        MemoryHelper.WriteRaw(address, data);
        MemoryHelper.ChangePermission(address, data.Length, oldProtection);
        return originalBytes;
    }

    public void Dispose()
    {
        if(IsFreezeEnabled)
        {
            FreezeRevert();
        }

        _framework.Update -= OnFrameworkUpdate;
        //_handlePhysicsDelegate.Dispose();
    }
}
