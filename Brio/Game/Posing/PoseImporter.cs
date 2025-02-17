using Brio.Core;
using Brio.Files;
using Brio.Game.Posing.Skeletons;

namespace Brio.Game.Posing;

public class PoseImporter(PoseFile poseFile, PoseImporterOptions options, bool expressionPhase = false)
{
    public void ApplyBone(Bone bone, BonePoseInfo poseInfo)
    {
        if(expressionPhase)
        {
            if(poseInfo.Name == "j_kao")
            {
                if(poseFile.Bones.TryGetValue(bone.Name, out var fileBone))
                {
                    poseInfo.Apply(fileBone, bone.LastRawTransform, TransformComponents.All, TransformComponents.Position, BoneIKInfo.Disabled, PoseMirrorMode.None, true);
                }

                return;
            }
            else
            {
                return;
            }
        }

        if(poseInfo.Slot == PoseInfoSlot.Character)
        {
            var isAllowed = options.BoneFilter.IsBoneValid(bone, poseInfo.Slot, true);
            if(isAllowed == true)
            {
                if(poseFile.Bones.TryGetValue(bone.Name, out var fileBone))
                {
                    poseInfo.Apply(fileBone, bone.LastRawTransform, TransformComponents.All, options.TransformComponents, BoneIKInfo.Disabled, PoseMirrorMode.None, true);
                }
            }
        }

        if(poseInfo.Slot == PoseInfoSlot.MainHand)
        {
            var isAllowed = options.BoneFilter.WeaponsAllowed;
            if(isAllowed == true)
            {
                if(poseFile.MainHand.TryGetValue(bone.Name, out var fileBone))
                {
                    poseInfo.Apply(fileBone, bone.LastRawTransform, TransformComponents.All, options.TransformComponents, BoneIKInfo.Disabled, PoseMirrorMode.None, true);
                }
            }
        }

        if(poseInfo.Slot == PoseInfoSlot.OffHand)
        {
            var isAllowed = options.BoneFilter.WeaponsAllowed;
            if(isAllowed == true)
            {
                if(poseFile.OffHand.TryGetValue(bone.Name, out var fileBone))
                {
                    poseInfo.Apply(fileBone, bone.LastRawTransform, TransformComponents.All, options.TransformComponents, BoneIKInfo.Disabled, PoseMirrorMode.None, true);
                }
            }
        }
    }
}

public class PoseImporterOptions(BoneFilter filter, TransformComponents transformComponents, bool applyModelTransform)
{
    public BoneFilter BoneFilter { get; set; } = filter;
    public TransformComponents TransformComponents { get; set; } = transformComponents;
    public bool ApplyModelTransform { get; set; } = applyModelTransform;
}
