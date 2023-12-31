using Brio.Core;
using Brio.Files;
using Brio.Game.Posing.Skeletons;
using System;

namespace Brio.Game.Posing;

internal class PoseImporter(PoseFile poseFile, PoseImporterOptions options)
{
    public void ApplyBone(Bone bone, BonePoseInfo poseInfo)
    {
        if (poseInfo.Slot == PoseInfoSlot.Character)
        {
            var isAllowed = options.BoneFilter.IsBoneValid(bone, poseInfo.Slot, true);
            if (isAllowed == true)
            {
                if (poseFile.Bones.TryGetValue(bone.Name, out var fileBone))
                {
                    poseInfo.Apply(fileBone, bone.LastTransform, TransformComponents.All, options.TransformComponents, PoseMirrorMode.None, true);
                }
            }
        }

        if (poseInfo.Slot == PoseInfoSlot.MainHand)
        {
            var isAllowed = options.BoneFilter.WeaponsAllowed;
            if (isAllowed == true)
            {
                if (poseFile.MainHand.TryGetValue(bone.Name, out var fileBone))
                {
                    poseInfo.Apply(fileBone, bone.LastTransform, TransformComponents.All, options.TransformComponents, PoseMirrorMode.None, true);
                }
            }
        }

        if (poseInfo.Slot == PoseInfoSlot.OffHand)
        {
            var isAllowed = options.BoneFilter.WeaponsAllowed;
            if (isAllowed == true)
            {
                if (poseFile.OffHand.TryGetValue(bone.Name, out var fileBone))
                {
                    poseInfo.Apply(fileBone, bone.LastTransform, TransformComponents.All, options.TransformComponents, PoseMirrorMode.None, true);
                }
            }
        }
    }
}

internal class PoseImporterOptions(BoneFilter filter, TransformComponents transformComponents, bool applyModelTransform)
{
    public BoneFilter BoneFilter { get; set; } = filter;
    public TransformComponents TransformComponents { get; set; } = transformComponents;
    public bool ApplyModelTransform { get; set; } = applyModelTransform;
}

[Flags]
internal enum PoseImportCategories
{
    None = 0,
    Body = 1 << 0,
    Head = 1 << 1,
    Weapons = 1 << 2,

    All = Body | Head | Weapons,
}
