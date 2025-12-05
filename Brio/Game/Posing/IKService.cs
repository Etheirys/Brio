using Brio.Core;
using Brio.Game.Posing.Skeletons;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Container.Array;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Game.Posing;

public unsafe class IKService : IDisposable
{
    delegate* unmanaged<hkaCCDSolver*, int, float, void> _ccdSolverCtr;
    delegate* unmanaged<hkaCCDSolver*, byte*, hkArray<CCDIKConstraint>*, hkaPose*, byte*> _ccdSolverSolve;
    delegate* unmanaged<byte*, TwoJointIKSetup*, hkaPose*, byte*> _twoJointSolverSolve;

    private (nint Aligned, nint Unaligned) _solverAddr;
    private (nint Aligned, nint Unaligned) _ccdConstraintCtrAddr;
    private (nint Aligned, nint Unaligned) _twoJointSetupAddr;

    public IKService(ISigScanner scanner)
    {
        _ccdSolverCtr = (delegate* unmanaged<hkaCCDSolver*, int, float, void>)scanner.ScanText("E8 ?? ?? ?? ?? 48 8D 43 ?? 48 C7 43");
        _ccdSolverSolve = (delegate* unmanaged<hkaCCDSolver*, byte*, hkArray<CCDIKConstraint>*, hkaPose*, byte*>)scanner.ScanText("E8 ?? ?? ?? ?? 8B 44 24 ?? 48 8B 5C 24 ?? 48 3B 5C 24");
        _twoJointSolverSolve = (delegate* unmanaged<byte*, TwoJointIKSetup*, hkaPose*, byte*>)scanner.ScanText("E8 ?? ?? ?? ?? 0F 28 55 ?? 41 0F 28 D8");

        _solverAddr = NativeHelpers.AllocateAlignedMemory(sizeof(hkaCCDSolver), 16);
        _ccdConstraintCtrAddr = NativeHelpers.AllocateAlignedMemory(sizeof(CCDIKConstraint), 16);
        _twoJointSetupAddr = NativeHelpers.AllocateAlignedMemory(sizeof(TwoJointIKSetup), 16);

        TwoJointIKSetup* setup = (TwoJointIKSetup*)_twoJointSetupAddr.Aligned;
        *setup = new TwoJointIKSetup();
    }

    public void SolveIK(hkaPose* pose, BoneIKInfo ikInfo, Bone bone, Vector3 target)
    {
        ikInfo.SolverOptions.Switch(
            ccd =>
            {
                var boneList = bone.GetBonesToDepth(ccd.Depth, true);
                if(boneList.Count <= 1)
                    return;

                var startBone = (short)boneList.Last().Index;
                var endBone = (short)boneList.First().Index;

                hkaCCDSolver* ccdSolver = (hkaCCDSolver*)_solverAddr.Aligned;
                _ccdSolverCtr(ccdSolver, ccd.Iterations, 1f);

                CCDIKConstraint* constraint = (CCDIKConstraint*)_ccdConstraintCtrAddr.Aligned;
                constraint->StartBone = startBone;
                constraint->EndBone = endBone;
                constraint->Target.X = target.X;
                constraint->Target.Y = target.Y;
                constraint->Target.Z = target.Z;

                var constraints = new hkArray<CCDIKConstraint>
                {
                    Length = 1,
                    CapacityAndFlags = 1,
                    Data = constraint
                };

                byte notSure = 0;
                _ccdSolverSolve(ccdSolver, &notSure, &constraints, pose);
            },
            twoJoint =>
            {
                var boneList = bone.GetBonesToDepth(twoJoint.FirstBone, true);

                if(boneList.Count < twoJoint.FirstBone)
                    return;

                TwoJointIKSetup* setup = (TwoJointIKSetup*)_twoJointSetupAddr.Aligned;
                setup->FirstJointIdx = (short)boneList[twoJoint.FirstBone].Index;
                setup->SecondJointIdx = (short)boneList[twoJoint.SecondBone].Index;
                setup->EndBoneIdx = (short)boneList[twoJoint.EndBone].Index;
                setup->EndTargetMS = new Vector4(target, 0);
                setup->HingeAxisLS = new Vector4(twoJoint.RotationAxis, 0);

                byte notSure = 0;
                _twoJointSolverSolve(&notSure, setup, pose);
            }
        );
    }

    public void Dispose()
    {
        NativeHelpers.FreeAlignedMemory(_solverAddr);
        NativeHelpers.FreeAlignedMemory(_ccdConstraintCtrAddr);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    private struct hkaCCDSolver
    {
        [FieldOffset(0x0)] public nint vtbl;
        [FieldOffset(0x10)] public uint Iterations;
        [FieldOffset(0x14)] public float Gain;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x2)]
    private struct CCDIKConstraint
    {
        [FieldOffset(0x0)] public short StartBone;
        [FieldOffset(0x2)] public short EndBone;
        [FieldOffset(0x10)] public hkVector4f Target;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x82)]
    private struct TwoJointIKSetup
    {
        [FieldOffset(0x00)] public short FirstJointIdx = -1;
        [FieldOffset(0x02)] public short SecondJointIdx = -1;
        [FieldOffset(0x04)] public short EndBoneIdx = -1;
        [FieldOffset(0x06)] public short FirstJointTwistIdx = -1;
        [FieldOffset(0x08)] public short SecondJointTwistIdx = -1;
        [FieldOffset(0x10)] public Vector4 HingeAxisLS = Vector4.Zero;
        [FieldOffset(0x20)] public float CosineMaxHingeAngle = -1f;
        [FieldOffset(0x24)] public float CosineMinHingeAngle = 1f;
        [FieldOffset(0x28)] public float FirstJointIkGain = 1f;
        [FieldOffset(0x2C)] public float SecondJointIkGain = 1f;
        [FieldOffset(0x30)] public float EndJointIkGain = 1f;
        [FieldOffset(0x40)] public Vector4 EndTargetMS = Vector4.Zero;
        [FieldOffset(0x50)] public Quaternion EndTargetRotationMS = Quaternion.Identity;
        [FieldOffset(0x60)] public Vector4 EndBoneOffsetLS = Vector4.Zero;
        [FieldOffset(0x70)] public Quaternion EndBoneRotationOffsetLS = Quaternion.Identity;
        [FieldOffset(0x80)] public bool EnforceEndPosition = true;
        [FieldOffset(0x81)] public bool EnforceEndRotation = false;

        public TwoJointIKSetup() { }
    }

}
