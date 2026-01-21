using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Input;
using Brio.Game.Posing;
using Brio.Game.Posing.Skeletons;
using Brio.Resources;
using Brio.Services;
using Brio.UI.Widgets.Posing;
using Brio.UI.Windows.Specialized;
using Dalamud.Plugin.Services;
using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.Capabilities.Posing;

public class PosingCapability : ActorCharacterCapability
{
    public PosingSelectionType Selected { get; set; } = new None();
    public PosingSelectionType Hover { get; set; } = new None();
    public PosingSelectionType LastHover { get; set; } = new None();

    public List<BonePoseInfoId> SelectedBones { get; set; } = [];
    public bool IsMultiSelecting => SelectedBones.Count > 1;

    public SkeletonPosingCapability SkeletonPosing => Entity.GetCapability<SkeletonPosingCapability>();
    public ModelPosingCapability ModelPosing => Entity.GetCapability<ModelPosingCapability>();

    public PosingService PosingService => _posingService;
    public ConfigurationService ConfigurationService => _configurationService;

    public bool HasOverride
    {
        get
        {
            if(Entity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosing))
                if(skeletonPosing.PoseInfo.IsOverridden)
                    return true;

            if(Entity.TryGetCapability<ModelPosingCapability>(out var modelPosing))
                if(modelPosing.HasOverride)
                    return true;

            return false;
        }
    }

    public bool CanResetBone(Bone? bone) => ModelPosing.HasOverride == false || !(bone is not null && !SkeletonPosing.PoseInfo.GetPoseInfo(bone).HasStacks);

    public bool CanUndo => _undoStack.Count is not 0 and not 1 || _groupedUndoService.CanUndo;
    public bool CanRedo => _redoStack.Count > 0 || _groupedUndoService.CanRedo;
    public bool HasIKApplied => SkeletonPosing.PoseInfo.HasIKStacks;

    private Stack<PoseStack> _undoStack = [];
    private Stack<PoseStack> _redoStack = [];

    public bool OverlayOpen
    {
        get => _overlayWindow.IsOpen;
        set
        {
            _overlayWindow.IsOpen = value;
            if(value == false)
                _gameInputService.AllowEscape = true;
        }
    }

    public bool TransformWindowOpen
    {
        get => _overlayTransformWindow.IsOpen;
        set => _overlayTransformWindow.IsOpen = value;
    }

    private readonly PosingOverlayWindow _overlayWindow;
    private readonly PosingService _posingService;
    private readonly ConfigurationService _configurationService;
    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly IFramework _framework;
    private readonly GameInputService _gameInputService;
    private readonly HistoryService _groupedUndoService;
    private readonly EntityManager _entityManager;

    public PosingCapability(
        ActorEntity parent,
        PosingOverlayWindow window,
        HistoryService groupedUndoService,
        PosingService posingService,
        EntityManager entityManager,
        ConfigurationService configurationService,
        PosingTransformWindow overlayTransformWindow,
        IFramework framework,
        GameInputService gameInputService)
        : base(parent)
    {
        Widget = new PosingWidget(this);

        _overlayWindow = window;
        _posingService = posingService;
        _configurationService = configurationService;
        _overlayTransformWindow = overlayTransformWindow;
        _entityManager = entityManager;
        _framework = framework;
        _groupedUndoService = groupedUndoService;
        _gameInputService = gameInputService;
    }

    public void LoadResourcesPose(string resourcesPath, bool freezeOnLoad = false, bool asBody = false)
    {
        var option = _posingService.SceneImporterOptions;
        TransformComponents? tfc = null;
        if(asBody)
        {
            option = _posingService.BodyOptions;
            tfc = TransformComponents.Rotation;
        }

        ImportPose(JsonSerializer.Deserialize<PoseFile>(ResourceProvider.Instance.GetRawResourceString(resourcesPath)), option, freezeOnLoad: freezeOnLoad, asBody: asBody, transformComponents: tfc);
    }

    public void ImportPose(string path, PoseImporterOptions? options = null)
    {
        try
        {
            if(path.EndsWith(".cmp"))
            {
                ImportPose(ResourceProvider.Instance.GetFileDocument<CMToolPoseFile>(path), options);
                return;
            }

            ImportPose(ResourceProvider.Instance.GetFileDocument<PoseFile>(path), options);
        }
        catch
        {
            Brio.NotifyError("Invalid pose file.");
        }
    }

    public void ImportPose(OneOf<PoseFile, CMToolPoseFile> rawPoseFile, PoseImporterOptions? options = null, bool asExpression = false, bool asScene = false, bool asIPCpose = false, bool asBody = false,
        bool freezeOnLoad = false, bool asProp = false, TransformComponents? transformComponents = null, bool? applyModelTransformOverride = null)
    {
        if(Actor.TryGetCapability<ActionTimelineCapability>(out var actionTimeline))
        {
            Brio.Log.Verbose($"Importing Pose... {asExpression} {asScene} {asIPCpose} {asBody} {freezeOnLoad}");

            actionTimeline.StopSpeedAndResetTimeline(() =>
            {
                ImportPose_Internal(rawPoseFile, options, reset: false, reconcile: false, asExpression: asExpression, asScene: asScene,
                    asIPCpose: asIPCpose, asBody: asBody, asProp: asProp, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);

            }, !(ConfigurationService.Instance.Configuration.Posing.FreezeActorOnPoseImport || freezeOnLoad));
        }
        else
        {
            Brio.Log.Warning($"Actor did not have ActionTimelineCapability while Importing a Pose... {asExpression} {asScene} {asIPCpose} {asBody} {freezeOnLoad}");
        }
    }

    // TODO change this boolean hell into flags after Scenes are added
    PoseFile? tempPose;
    internal void ImportPose_Internal(OneOf<PoseFile, CMToolPoseFile> rawPoseFile, PoseImporterOptions? options = null, bool generateSnapshot = true, bool reset = true, bool reconcile = true,
        bool asExpression = false, bool expressionPhase2 = false, bool asScene = false, bool asIPCpose = false, bool asBody = false, bool asProp = false,
        TransformComponents? transformComponents = null, bool? applyModelTransformOverride = null)
    {
        var poseFile = rawPoseFile.Match(
                poseFile => poseFile,
                cmToolPoseFile => cmToolPoseFile.Upgrade()
            );

        if(poseFile.Bones.Count == 0 && poseFile.MainHand.Count == 0 && poseFile.OffHand.Count == 0)
        {
            Brio.NotifyError("Invalid pose file.");
            Brio.Log.Info($"Invalid pose file. {reconcile} {reset} {generateSnapshot} {asExpression} {expressionPhase2} {asScene} {asIPCpose} {asBody}");
            return;
        }

        poseFile.SanitizeBoneNames();

        bool applyModelTransform = false;
        if(asExpression)
        {
            Brio.Log.Debug("Loading as Expression");

            options = _posingService.ExpressionOptions;
            tempPose = GeneratePoseFile();
        }
        else if(asBody)
        {
            options = _posingService.BodyOptions;
        }
        else if(asScene)
        {
            options = _posingService.SceneImporterOptions;

            applyModelTransform |= ConfigurationService.Instance.Configuration.Import.ApplyModelTransform;
        }
        else if(asIPCpose)
        {
            options = _posingService.DefaultIPCImporterOptions;
        }
        else
        {
            options ??= _posingService.DefaultImporterOptions;
        }

        if(asScene == false)
        {
            applyModelTransform |= options.ApplyModelTransform;

            if(transformComponents.HasValue)
            {
                options.TransformComponents = transformComponents.Value;
            }

            if(applyModelTransformOverride.HasValue)
            {
                applyModelTransform = applyModelTransformOverride.Value;
            }
        }

        if(applyModelTransform && reset)
            ModelPosing.ResetTransform();

        SkeletonPosing.ImportSkeletonPose(poseFile, options, expressionPhase2);

        if(asExpression == false)
            ModelPosing.ImportModelPose(poseFile, options, asScene, applyModelTransform);

        if(expressionPhase2)
        {
            var bone = SkeletonPosing.GetBone("j_kao", PoseInfoSlot.Character);
            if(bone != null)
            {
                var poseInfo = SkeletonPosing.PoseInfo.GetPoseInfo(bone);
                if(poseInfo.HasStacks)
                    poseInfo.RemoveLastStack();
            }
        }

        if(generateSnapshot)
            _framework.RunOnTick(() => Snapshot(reset, reconcile, asExpression: asExpression), delayTicks: 4);
    }

    public PoseFile ExportPose()
    {
        return GeneratePoseFile();
    }
    public void ExportSavePose(string path)
    {
        var poseFile = ExportPose();
        ResourceProvider.Instance.SaveFileDocument(path, poseFile);
    }

    public void Snapshot(bool reset = true, bool reconcile = true, bool asExpression = false)
    {
        var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
        if(undoStackSize <= 0)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            return;
        }

        _redoStack.Clear();

        if(asExpression == true)
        {
            ImportPose_Internal(tempPose!, new PoseImporterOptions(new BoneFilter(_posingService), TransformComponents.All, false),
            generateSnapshot: true, expressionPhase2: true);

            return;
        }

        if(_undoStack.Count == 0)
            _undoStack.Push(new PoseStack(new PoseInfo(), ModelPosing.OriginalTransform));

        _undoStack.Push(new PoseStack(SkeletonPosing.PoseInfo.Clone(), ModelPosing.Transform));
        _undoStack = _undoStack.Trim(undoStackSize + 1);


        // Holy hell, This took me so long to fix and it stil breaks IK
        var bone = SkeletonPosing.GetBone("j_kao", PoseInfoSlot.Character);
        if(bone != null)
        {
            var face = SkeletonPosing.PoseInfo.GetPoseInfo(bone);

            // Check if j_kao or any of its parent bones are overridden
            bool hasOverriddenParent = false;
            var currentBone = bone.Parent;
            while(currentBone != null)
            {
                var parentPoseInfo = SkeletonPosing.PoseInfo.GetPoseInfo(currentBone);
                if(parentPoseInfo.HasStacks)
                {
                    hasOverriddenParent = true;
                    break;
                }
                currentBone = currentBone.Parent;
            }

            if(face.HasStacks || hasOverriddenParent)
            {
                // Reconcile ONLY j_kao and its descendants to fix gizmo without affecting limbs
                ReconcileChildren(bone);
                return;
            }
        }

        if(reconcile)
            Reconcile(reset);
    }

    public void Redo()
    {
        if(_entityManager.SelectedEntityIds.Count > 1)
        {
            _groupedUndoService.Redo();
            return;
        }

        if(_redoStack.TryPop(out var redoStack))
        {
            _undoStack.Push(redoStack);
            SkeletonPosing.PoseInfo = redoStack.Info.Clone();
            ModelPosing.Transform = redoStack.ModelTransform;
        }
    }

    public void Undo()
    {
        if(_entityManager.SelectedEntityIds.Count > 1)
        {
            _groupedUndoService.Undo();
            return;
        }

        if(_undoStack.TryPop(out var undoStack))
            _redoStack.Push(undoStack);

        if(_undoStack.TryPeek(out var applicable))
        {
            SkeletonPosing.PoseInfo = applicable.Info.Clone();
            ModelPosing.Transform = applicable.ModelTransform;
        }
    }

    public void Reset(bool generateSnapshot = true, bool reset = true, bool clearHistStack = true)
    {
        if(Actor.IsProp == false)
            SkeletonPosing.ResetPose();
        ModelPosing.ResetTransform();

        if(clearHistStack)
            _redoStack.Clear();

        if(generateSnapshot)
            Snapshot(reset);
    }

    private void ReconcileChildren(Bone bone)
    {
        // We create a partial pose so we can properly reconcile,
        // This was designed to work with j_kao and descendant, but it might work with other bones too
        var partialPoseFile = new PoseFile();

        ExportFaceBone(bone);
        ClearFaceStacks(bone);

        var options = new PoseImporterOptions(new BoneFilter(_posingService), TransformComponents.All, true);
        SkeletonPosing.ImportSkeletonPose(partialPoseFile, options, false);

        void ExportFaceBone(Bone bone)
        {
            partialPoseFile.Bones[bone.Name] = bone.LastRawTransform;
            foreach(var child in bone.Children)
            {
                ExportFaceBone(child);
            }
        }

        void ClearFaceStacks(Bone bone)
        {
            var poseInfo = SkeletonPosing.PoseInfo.GetPoseInfo(bone);
            poseInfo.ClearStacks();
            foreach(var child in bone.Children)
            {
                ClearFaceStacks(child);
            }
        }
    }

    private void Reconcile(bool reset = true, bool generateSnapshot = true)
    {
        _framework.RunOnTick(() =>
        {
            var all = new PoseImporterOptions(new BoneFilter(_posingService), TransformComponents.All, true);
            var poseFile = GeneratePoseFile();
            if(reset)
            {
                Reset(generateSnapshot, false);
            }
            ImportPose_Internal(poseFile, options: all, generateSnapshot: false);
        }, delayTicks: 2);
    }

    public PoseFile GeneratePoseFile()
    {
        var poseFile = new PoseFile();
        SkeletonPosing.ExportSkeletonPose(poseFile);
        ModelPosing.ExportModelPose(poseFile);
        return poseFile;
    }

    public BonePoseInfoId? IsSelectedBone()
    {
        Bone? realBone = null;
        return Selected.Match<BonePoseInfoId?>(
            bone =>
            {
                realBone = SkeletonPosing.GetBone(bone);
                if(realBone != null && realBone.Skeleton.IsValid)
                    return bone;
                return null;
            },
            _ => null,
            _ => null
        );
    }

    public static void FlipBone(Bone bone, BonePoseInfo poseInfo)
    {
        var newBoneTransform = bone.LastTransform;

        // Convert to Euler (like the Gizmo)
        var boneRotationEuler = bone.LastTransform.Rotation.ToEuler();
        boneRotationEuler.X = 180 - boneRotationEuler.X;
        boneRotationEuler.Y = -boneRotationEuler.Y;
        var newBoneRotation = boneRotationEuler.ToQuaternion();

        newBoneTransform.Rotation = newBoneRotation;

        poseInfo.Apply(newBoneTransform, bone.LastRawTransform, TransformComponents.All, TransformComponents.All, poseInfo.DefaultIK, poseInfo.MirrorMode, true);
    }

    // you can blame the chons for me getting off my ass and making this work
    public void MirrorPose()
    {
        // Mirrors the entire pose, Left/right bone pairs (_l/_r) are swapped with mirrored transforms.
        // Center bones are mirrored in place. Weapons are swapped between hands.
        // Should work with all bones. Has some drifting if you use it like 70 times, but, who is going to do that?!?!?!


        var skeleton = SkeletonPosing.CharacterSkeleton;
        if(skeleton == null || skeleton.Bones.Count == 0)
            return;

        var currentPose = GeneratePoseFile();
        var mirroredPose = new PoseFile();

        var processedBones = new HashSet<string>(); // We need to track processed bones to avoid double-processing left/right pairs in some instances 

        foreach(var (boneName, transform) in currentPose.Bones)
        {
            if(processedBones.Contains(boneName))
                continue;

            var bone = skeleton.GetFirstVisibleBone(boneName);

            // Skip j_ex bones (PartialId 4) &
            // Skip partial root bones & skeleton root
            if(bone == null || (bone.IsPartialRoot && !bone.IsSkeletonRoot) || bone.PartialId == 4)
            {
                mirroredPose.Bones[boneName] = transform;
                continue;
            }

            // Check for left or right bone pair
            string? oppositeBoneName = null;
            if(boneName.EndsWith("_l"))
            {
                oppositeBoneName = boneName[..^1] + "r";
                if(skeleton.GetFirstVisibleBone(oppositeBoneName) == null)
                    oppositeBoneName = null;
            }
            else if(boneName.EndsWith("_r"))
            {
                oppositeBoneName = boneName[..^1] + "l";
                if(skeleton.GetFirstVisibleBone(oppositeBoneName) == null)
                    oppositeBoneName = null;
            }

            if(oppositeBoneName != null && currentPose.Bones.TryGetValue(oppositeBoneName, out var oppositeTransform))
            {
                // Swap left to right pair with mirrored transforms
                mirroredPose.Bones[boneName] = MirrorBoneTransform(oppositeTransform);
                mirroredPose.Bones[oppositeBoneName] = MirrorBoneTransform(transform);

                processedBones.Add(boneName);
                processedBones.Add(oppositeBoneName);
            }
            else
            {
                // Center bone to mirror in place
                mirroredPose.Bones[boneName] = MirrorBoneTransform(transform);
                processedBones.Add(boneName);
            }
        }

        // Swap and mirror weapon bones between main hand and off hand, maybe we should something with props/ornament too, but I would have to do more clever things
        foreach(var (boneName, boneTransform) in currentPose.MainHand)
        {
            mirroredPose.OffHand[boneName] = MirrorBoneTransform(boneTransform);
        }
        foreach(var (boneName, boneTransform) in currentPose.OffHand)
        {
            mirroredPose.MainHand[boneName] = MirrorBoneTransform(boneTransform);
        }

        //This doesn't really work right, I need to do the same thing as the normal skeleton mirroring above to make this work properly, I can do that later
        //foreach(var (boneName, boneTransform) in currentPose.Ornament)
        //{
        //    mirroredPose.MainHand[boneName] = MirrorBoneTransform(boneTransform);
        //}

        // This one will need something clever
        //foreach(var (boneName, boneTransform) in currentPose.Prop)
        //{
        //    mirroredPose.MainHand[boneName] = MirrorBoneTransform(boneTransform);
        //}


        mirroredPose.ModelDifference = MirrorModelTransform(currentPose.ModelDifference);
        mirroredPose.ModelAbsoluteValues = MirrorModelTransform(currentPose.ModelAbsoluteValues);

        var options = new PoseImporterOptions(new BoneFilter(_posingService), TransformComponents.All, true);
        ImportPose_Internal(mirroredPose, options: options, generateSnapshot: true, reset: true, reconcile: true);
    }

    private static Transform MirrorBoneTransform(Transform transform)
    {
        var euler = transform.Rotation.ToEuler();
        euler.X = 180 - euler.X;
        euler.Y = -euler.Y;
        var mirroredRotation = euler.ToQuaternion();

        var mirroredPosition = new Vector3(-transform.Position.X, transform.Position.Y, transform.Position.Z);

        return new Transform
        {
            Position = mirroredPosition,
            Rotation = mirroredRotation,
            Scale = transform.Scale
        };
    }

    private static Transform MirrorModelTransform(Transform transform)
    {
        // Mirrors a model transform across the YZ plane.
        // Uses simple rotation mirroring (only negates Y rotation).

        var mirroredPosition = new Vector3(-transform.Position.X, transform.Position.Y, transform.Position.Z);

        var euler = transform.Rotation.ToEuler();
        euler.Y = -euler.Y;
        var mirroredRotation = euler.ToQuaternion();

        return new Transform
        {
            Position = mirroredPosition,
            Rotation = mirroredRotation,
            Scale = transform.Scale
        };
    }

    public void FlipBone()
    {
        BonePoseInfoId? selectedIsBone = IsSelectedBone();
        // Bone Flip
        if(selectedIsBone.HasValue)
        {
            // Get current bone rotation data
            var bone = SkeletonPosing.GetBone(selectedIsBone.Value);
            if(bone != null)
            {
                var poseInfo = SkeletonPosing.PoseInfo.GetPoseInfo(bone);
                FlipBone(bone, poseInfo);

                // record change for undo
                Snapshot(reset: false);
            }
        }
    }

    public void ResetSelectedBone()
    {
        BonePoseInfoId? selectedIsBone = IsSelectedBone();
        if(selectedIsBone.HasValue)
        {
            ResetBoneStacks(selectedIsBone);
        }
        else if(ModelPosing.HasOverride)
        {
            ResetTransform();
        }
    }

    public void ResetBoneStacks(BonePoseInfoId? boneid)
    {
        if(boneid == null)
            return;

        var bone = SkeletonPosing.GetBone(boneid.Value);
        if(bone != null)
        {
            var poseInfo = SkeletonPosing.PoseInfo.GetPoseInfo(bone);
            if(poseInfo.HasStacks)
            {
                poseInfo.ClearStacks();
                Snapshot(reset: false);
            }
        }
    }

    public void ResetTransform()
    {
        ModelPosing.ResetTransform();
        Snapshot(reset: false);
    }

    public void ClearSelection()
    {
        Selected = PosingSelectionType.None;
        SelectedBones.Clear();
    }

    public void ToggleBoneSelection(BonePoseInfoId boneId)
    {
        var existingBone = SelectedBones.FirstOrDefault(b => b.Equals(boneId));
        if(!existingBone.Equals(default(BonePoseInfoId)))
        {
            SelectedBones.Remove(existingBone);
            if(SelectedBones.Count == 0)
            {
                Selected = PosingSelectionType.None;
            }
            else if(Selected.Value is BonePoseInfoId selectedBone && selectedBone.Equals(boneId))
            {
                Selected = SelectedBones[0];
            }
        }
        else
        {
            SelectedBones.Add(boneId);
            Selected = boneId;
        }
    }

    public void SetBoneSelection(BonePoseInfoId boneId, bool multiSelect)
    {
        if(multiSelect)
        {
            ToggleBoneSelection(boneId);
        }
        else
        {
            SelectedBones.Clear();
            SelectedBones.Add(boneId);
            Selected = boneId;
        }
    }

    public bool IsBoneSelected(BonePoseInfoId boneId)
    {
        return SelectedBones.Any(b => b.Equals(boneId));
    }

    public record struct PoseStack(PoseInfo Info, Transform ModelTransform);
}

public enum ExpressionPhase
{
    None, One, Two, Three
}
