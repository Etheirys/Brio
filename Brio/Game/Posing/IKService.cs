using Brio.Core;
using Brio.Game.Posing.Skeletons;
using Dalamud.Game;
using FFXIVClientStructs.Havok;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Game.Posing;
internal unsafe class IKService : IDisposable
{
    delegate* unmanaged<hkaCCDSolver*, int, float, void> _ccdSolverCtr;
    delegate* unmanaged<hkaCCDSolver*, byte*, hkArray<IKConstraint>*, hkaPose*, byte*> _ccdSolverSolve;

    private (nint Aligned, nint Unaligned) _solverAddr;
    private (nint Aligned, nint Unaligned) _constraintAddr;

    public IKService(ISigScanner scanner)
    {
        _ccdSolverCtr = (delegate* unmanaged<hkaCCDSolver*, int, float, void>)scanner.ScanText("E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 C7 43");
        _ccdSolverSolve = (delegate* unmanaged<hkaCCDSolver*, byte*, hkArray<IKConstraint>*, hkaPose*, byte*>)scanner.ScanText("E8 ?? ?? ?? ?? 8B 45 ?? 48 8B 75");
    
        _solverAddr = NativeHelpers.AllocateAlignedMemory(sizeof(hkaCCDSolver), 16);
        _constraintAddr = NativeHelpers.AllocateAlignedMemory(sizeof(IKConstraint), 16);
    }

    public void SolveIK(hkaPose* pose, ushort startBone, ushort endBone, Vector3 target, int iterations)
    {
        hkaCCDSolver* ccdSolver = (hkaCCDSolver*)_solverAddr.Aligned;
        _ccdSolverCtr(ccdSolver, iterations, 1f);

        IKConstraint* constraint = (IKConstraint*)_constraintAddr.Aligned;
        constraint->StartBone = startBone;
        constraint->EndBone = endBone;
        constraint->Target.X = target.X;
        constraint->Target.Y = target.Y;
        constraint->Target.Z = target.Z;

        var constraints = new hkArray<IKConstraint>
        {
            Length = 1,
            CapacityAndFlags = 1,
            Data = constraint
        };

        byte notSure = 0;
        _ccdSolverSolve(ccdSolver, &notSure, &constraints, pose);
    }

    public void Dispose()
    {
        NativeHelpers.FreeAlignedMemory(_solverAddr);
        NativeHelpers.FreeAlignedMemory(_constraintAddr);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    private struct hkaCCDSolver
    {
        [FieldOffset(0x0)] public nint vtbl;
        [FieldOffset(0x10)] public uint Iterations;
        [FieldOffset(0x14)] public float Gain;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x2)]
    private struct IKConstraint
    {
        [FieldOffset(0x0)] public ushort StartBone;
        [FieldOffset(0x2)] public ushort EndBone;
        [FieldOffset(0x10)] public hkVector4f Target;
    }

}
