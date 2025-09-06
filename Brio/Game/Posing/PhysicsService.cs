
//
// Code found in this file is from and is inspired by,
// Anamnesis (https://github.com/imchillin/Anamnesis)
//

// This file is licensed under the MIT license.
// © Anamnesis, Brio & Contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.Posing;

public unsafe partial class PhysicsService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly IFramework _framework;

    private unsafe delegate void HandlePhysicsDelegate(IntPtr arg1, short arg2, IntPtr arg3, char arg4, char arg5);
    private readonly Hook<HandlePhysicsDelegate> _handlePhysicsDelegate = null!;

    public bool IsFreezeEnabled { get; private set; } = false;

    public PhysicsService(ISigScanner scanner, IFramework framework, GPoseService gPoseService, IGameInteropProvider hooking)
    {
        _gPoseService = gPoseService;
        _framework = framework;

        _framework.Update += OnFrameworkUpdate;

        // This signature is from Anamnesis (https://github.com/imchillin/Anamnesis)
        // Found in AddressService.cs on line 159 - SkeletonFreezePhysics (1/2/3)
        //var oldFreezePhysicsAddress = "0F 11 48 10 41 0F 10 44 24 ?? 0F 11 40 20 48 8B 46 28";

        // Thank you Winter!
        var handlePhysicsSig = "E9 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 41 ?? ?? ?? 4c ?? ?? 30 ?? ?? ?? 41"; // e9 2d e0 09 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 41 0f b6 c0 4c 8b d1 45 8b c8 48 69 c8 20 02 00 00
        _handlePhysicsDelegate = hooking.HookFromAddress<HandlePhysicsDelegate>(scanner.ScanText(handlePhysicsSig), HandlePhysicsDetour);
        _handlePhysicsDelegate.Enable();
    }

    public unsafe void HandlePhysicsDetour(IntPtr arg1, short arg2, IntPtr arg3, char arg4, char arg5)
    {
        if(IsFreezeEnabled)
        {
            return;
        }

        _handlePhysicsDelegate.Original(arg1, arg2, arg3, arg4, arg5);
    }

    public bool FreezeToggle() => IsFreezeEnabled ? FreezeRevert() : FreezeEnable();

    public bool FreezeRevert() => IsFreezeEnabled = false;
    public bool FreezeEnable() => IsFreezeEnabled = true;

    private void OnFrameworkUpdate(IFramework framework)
    {
        if(IsFreezeEnabled && _gPoseService.IsGPosing == false)
        {
            FreezeRevert();
        }
    }

    public void Dispose()
    {
        if(IsFreezeEnabled)
        {
            FreezeRevert();
        }

        _framework.Update -= OnFrameworkUpdate;
        _handlePhysicsDelegate.Dispose();
    }
}
