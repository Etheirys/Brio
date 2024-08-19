
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
using Dalamud.Plugin.Services;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Brio.Game.Posing;

internal unsafe partial class PhysicsService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly IFramework _framework;

    //

    const byte NOP = 0x90;
    public bool IsFreezeEnabled { get; private set; } = false;

    //

    private readonly IntPtr freezeSkeletonPhysics1;
    private readonly IntPtr freezeSkeletonPhysics2;

    private readonly byte[] originalFreezeBytes1;
    private readonly byte[] originalFreezeBytes2;

    private readonly byte[] nopFreezeBytes1;
    private readonly byte[] nopFreezeBytes2;

    public PhysicsService(ISigScanner scanner, IFramework framework, GPoseService gPoseService)
    {
        _gPoseService = gPoseService;
        _framework = framework;

        // This signature is from Anamnesis (https://github.com/imchillin/Anamnesis)
        // Found in AddressService.cs on line 159 - SkeletonFreezePhysics (1/2/3)
        string freezePhysicsAddress = "0F 11 48 10 41 0F 10 44 24 ?? 0F 11 40 20 48 8B 46 28";

        var freezePhysics = scanner.ScanText(freezePhysicsAddress);

        freezeSkeletonPhysics1 = freezePhysics;
        freezeSkeletonPhysics2 = freezePhysics - 0x9;

        _framework.Update += OnFrameworkUpdate;

        (originalFreezeBytes1, originalFreezeBytes2) = FreezeReadBytes();

        nopFreezeBytes1 = [
            NOP,
            NOP,
            NOP,
            NOP
        ];

        nopFreezeBytes2 = [
            NOP,
            NOP,
            NOP
        ];
    }

    public (byte[], byte[]) FreezeReadBytes()
    {
        return ([
            Marshal.ReadByte(freezeSkeletonPhysics1, 0),
            Marshal.ReadByte(freezeSkeletonPhysics1, 1),
            Marshal.ReadByte(freezeSkeletonPhysics1, 2),
            Marshal.ReadByte(freezeSkeletonPhysics1, 3)
        ], [
            Marshal.ReadByte(freezeSkeletonPhysics2, 0),
            Marshal.ReadByte(freezeSkeletonPhysics2, 1),
            Marshal.ReadByte(freezeSkeletonPhysics2, 2)
        ]);
    }

    public bool FreezeToggle() => IsFreezeEnabled ? FreezeRevert() : FreezeEnable();

    public bool FreezeRevert()
    {
        IsFreezeEnabled = false;

        try
        {
            using Process currentProcess = Process.GetCurrentProcess();

            WriteProcessMemory(currentProcess.Handle, freezeSkeletonPhysics1, originalFreezeBytes1, originalFreezeBytes1.Length, out _);

            WriteProcessMemory(currentProcess.Handle, freezeSkeletonPhysics2, originalFreezeBytes2, originalFreezeBytes2.Length, out _);
        }
        catch(Exception ex)
        {
            Brio.Log.Fatal($"Brio encountered Fatal Error, FreezeRevert faild: {ex}");
        }
       
        return IsFreezeEnabled;
    }

    public bool FreezeEnable()
    {
        using Process currentProcess = Process.GetCurrentProcess();

        WriteProcessMemory(currentProcess.Handle, freezeSkeletonPhysics1, nopFreezeBytes1, nopFreezeBytes1.Length, out _);

        WriteProcessMemory(currentProcess.Handle, freezeSkeletonPhysics2, nopFreezeBytes2, nopFreezeBytes2.Length, out _);

        return IsFreezeEnabled = true;
    }

    public void Dispose()
    {
        if(IsFreezeEnabled)
            FreezeRevert();

        _framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if(IsFreezeEnabled && _gPoseService.IsGPosing == false)
            FreezeRevert();
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);
}
