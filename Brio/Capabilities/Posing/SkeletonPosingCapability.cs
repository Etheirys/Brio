using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Posing;
using Brio.Game.Posing.Skeletons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Capabilities.Posing
{

    internal class SkeletonPosingCapability : ActorCharacterCapability
    {
        private readonly SkeletonService _skeletonService;
        private readonly PosingService _posingService;


        public Skeleton? CharacterSkeleton { get; private set; }
        public Skeleton? MainHandSkeleton { get; private set; }
        public Skeleton? OffHandSkeleton { get; private set; }



        public IReadOnlyList<(Skeleton Skeleton, PoseInfoSlot Slot)> Skeletons => new[] { (CharacterSkeleton, PoseInfoSlot.Character), (MainHandSkeleton, PoseInfoSlot.MainHand), (OffHandSkeleton, PoseInfoSlot.OffHand) }.Where(s => s.Item1 != null).Cast<(Skeleton Skeleton, PoseInfoSlot Slot)>().ToList();

        public PoseInfo PoseInfo { get; set; } = new PoseInfo();

        private readonly List<Action<Bone, BonePoseInfo>> _transitiveActions = [];


        public SkeletonPosingCapability(ActorEntity parent, SkeletonService skeletonService, PosingService posingService) : base(parent)
        {
            _skeletonService = skeletonService;
            _posingService = posingService;

            _skeletonService.SkeletonUpdateStart += OnSkeletonUpdateStart;
            _skeletonService.SkeletonUpdateEnd += OnSkeletonUpdateEnd;

        }

        public void ResetPose()
        {
            PoseInfo.Clear();
        }

        public void RegisterTransitiveAction(Action<Bone, BonePoseInfo> action)
        {
            _transitiveActions.Add(action);
        }

        public void ExecuteTransitiveActions(Bone bone, BonePoseInfo poseInfo)
        {
            _transitiveActions.ForEach(a => a(bone, poseInfo));
        }

        public void ImportSkeletonPose(PoseFile poseFile, PoseImporterOptions options)
        {
            var importer = new PoseImporter(poseFile, options);
            RegisterTransitiveAction(importer.ApplyBone);
        }

        public void ExportSkeletonPose(PoseFile poseFile)
        {
            var skeleton = CharacterSkeleton;
            if (skeleton != null)
            {
                foreach (var bone in CharacterSkeleton!.Bones)
                {
                    if (bone.IsPartialRoot && !bone.IsSkeletonRoot)
                        continue;

                    poseFile.Bones[bone.Name] = bone.LastRawTransform;
                }
            }

            var mainHandSkeleton = MainHandSkeleton;
            if (mainHandSkeleton != null)
            {
                foreach (var bone in mainHandSkeleton!.Bones)
                {
                    if (bone.IsPartialRoot && !bone.IsSkeletonRoot)
                        continue;

                    poseFile.MainHand[bone.Name] = bone.LastRawTransform;
                }
            }

            var offHandSkeleton = OffHandSkeleton;
            if (offHandSkeleton != null)
            {
                foreach (var bone in offHandSkeleton!.Bones)
                {
                    if (bone.IsPartialRoot && !bone.IsSkeletonRoot)
                        continue;

                    poseFile.OffHand[bone.Name] = bone.LastRawTransform;
                }
            }
        }

        public unsafe BonePoseInfo GetBonePose(BonePoseInfoId bone)
        {
            return PoseInfo.GetPoseInfo(bone);
        }

        public unsafe BonePoseInfo GetBonePose(Bone bone)
        {
            if (CharacterSkeleton != null && CharacterSkeleton == bone.Skeleton)
            {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Character);
            }

            if (MainHandSkeleton != null && MainHandSkeleton == bone.Skeleton)
            {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.MainHand);
            }

            if (OffHandSkeleton != null && OffHandSkeleton == bone.Skeleton)
            {
                return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.OffHand);
            }

            return PoseInfo.GetPoseInfo(bone, PoseInfoSlot.Unknown);
        }

        public Bone? GetBone(BonePoseInfoId? id)
        {
            if (id == null)
                return null;

            return id.Value.Slot switch
            {
                PoseInfoSlot.Character => CharacterSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
                PoseInfoSlot.MainHand => MainHandSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
                PoseInfoSlot.OffHand => OffHandSkeleton?.Partials.ElementAtOrDefault(id.Value.Partial)?.GetBone(id.Value.BoneName),
                _ => null,
            };
        }

        public Bone? GetBone(string name, PoseInfoSlot slot)
        {
            return slot switch
            {
                PoseInfoSlot.Character => CharacterSkeleton?.GetFirstVisibleBone(name),
                PoseInfoSlot.MainHand => MainHandSkeleton?.GetFirstVisibleBone(name),
                PoseInfoSlot.OffHand => OffHandSkeleton?.GetFirstVisibleBone(name),
                _ => null,
            };
        }

        private unsafe void UpdateCache()
        {
            CharacterSkeleton = _skeletonService.GetSkeleton(Character.GetCharacterBase());
            MainHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.MainHand));
            OffHandSkeleton = _skeletonService.GetSkeleton(Character.GetWeaponCharacterBase(ActorEquipSlot.OffHand));

            _skeletonService.RegisterForFrameUpdate(CharacterSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(MainHandSkeleton, this);
            _skeletonService.RegisterForFrameUpdate(OffHandSkeleton, this);
        }

        private void OnSkeletonUpdateStart()
        {
            UpdateCache();
        }

        private void OnSkeletonUpdateEnd()
        {
            _transitiveActions.Clear();
        }

        public override void Dispose()
        {
            _skeletonService.SkeletonUpdateStart -= OnSkeletonUpdateStart;
            _skeletonService.SkeletonUpdateEnd -= OnSkeletonUpdateEnd;

            _transitiveActions.Clear();

            PoseInfo.Clear();
            base.Dispose();
        }


    }
}
