using Brio.Capabilities.Actor;
using Brio.Game.Posing;
using OneOf.Types;
using Brio.Entities.Actor;
using Brio.UI.Widgets.Posing;
using Brio.UI.Windows.Specialized;
using Brio.Entities.Core;
using Brio.Core;
using Brio.Files;
using Brio.Resources;
using OneOf;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Brio.Config;

namespace Brio.Capabilities.Posing;

internal class PosingCapability : ActorCharacterCapability
{
    public PosingSelectionType Selected { get; set; } = new None();

    public SkeletonPosingCapability SkeletonPosing => Entity.GetCapability<SkeletonPosingCapability>();
    public ModelPosingCapability ModelPosing => Entity.GetCapability<ModelPosingCapability>();

    public bool HasOverride
    {
        get
        {
            if(Entity.TryGetCapability<SkeletonPosingCapability>(out var skeletonPosing))
                if(skeletonPosing.PoseInfo.IsOveridden)
                    return true;

            if(Entity.TryGetCapability<ModelPosingCapability>(out var modelPosing))
                if(modelPosing.HasOverride)
                    return true;

            return false;
        }
    }

    public bool HasUndoStack => _undoStack.Count > 1;
    public bool HasRedoStack => _redoStack.Any();
    public bool HasIKApplied => SkeletonPosing.PoseInfo.HasIKStacks;

    private Stack<PoseStack> _undoStack = [];
    private Stack<PoseStack> _redoStack = [];

    public bool OverlayOpen
    {
        get => _overlayWindow.IsOpen;
        set => _overlayWindow.IsOpen = value;
    }

    private readonly PosingOverlayWindow _overlayWindow;
    private readonly PosingService _posingService;
    private readonly ConfigurationService _configurationService;
    private readonly IFramework _framework;

    public PosingCapability(ActorEntity parent, PosingOverlayWindow window, PosingService posingService, ConfigurationService configurationService, IFramework framework) : base(parent)
    {
        Widget = new PosingWidget(this);
        _overlayWindow = window;
        _posingService = posingService;
        _configurationService = configurationService;
        _framework = framework;
    }

    public void ClearSelection() => Selected = PosingSelectionType.None;

    public void ImportPose(string path, PoseImporterOptions? options = null)
    {
        try
        {
            if (path.EndsWith(".cmp"))
            {
                ImportPose(ResourceProvider.Instance.GetFileDocument<CMToolPoseFile>(path), options);
                return;
            }

            ImportPose(ResourceProvider.Instance.GetFileDocument<PoseFile>(path), options);
        }
        catch
        {
            EventBus.Instance.NotifyError("Invalid pose file.");
        }
    }

    public void ImportPose(OneOf<PoseFile, CMToolPoseFile> rawPoseFile, PoseImporterOptions? options = null, bool generateSnapshot = true)
    {
        var poseFile = rawPoseFile.Match(
                poseFile => poseFile,
                cmToolPoseFile => cmToolPoseFile.Upgrade()
            );

        if (!poseFile.Bones.Any() && !poseFile.MainHand.Any() && !poseFile.OffHand.Any())
        {
            EventBus.Instance.NotifyError("Invalid pose file.");
            return;
        }

        poseFile.SanitizeBoneNames();

        options ??= _posingService.ImporterOptions;

        SkeletonPosing.ImportSkeletonPose(poseFile, options);
        ModelPosing.ImportModelPose(poseFile, options);

        if (generateSnapshot)
            _framework.RunOnTick(() => Snapshot(), delayTicks: 2);
    }

    public void ExportPose(string path)
    {
        var poseFile = GeneratePoseFile();
        ResourceProvider.Instance.SaveFileDocument(path, poseFile);
    }

    public void Snapshot()
    {
        var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
        if (undoStackSize <= 0)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            return;
        }

        

        if(!_undoStack.Any())
            _undoStack.Push(new PoseStack(new PoseInfo(), ModelPosing.OriginalTransform));

        _redoStack.Clear();
        _undoStack.Push(new PoseStack(SkeletonPosing.PoseInfo.Clone(), ModelPosing.Transform));
        _undoStack = _undoStack.Trim(undoStackSize + 1);
    }

    public void Redo()
    {
        if (_redoStack.TryPop(out var redoStack))
        {
            _undoStack.Push(redoStack);
            SkeletonPosing.PoseInfo = redoStack.Info.Clone();
            ModelPosing.Transform = redoStack.ModelTransform;
        }
    }

    public void Undo()
    {
        if (_undoStack.TryPop(out var undoStack))
            _redoStack.Push(undoStack);

        if (_undoStack.TryPeek(out var applicable))
        {
            SkeletonPosing.PoseInfo = applicable.Info.Clone();
            ModelPosing.Transform = applicable.ModelTransform;
        }
    }

    public void Reset(bool generateSnapshot = true)
    {
        SkeletonPosing.ResetPose();
        ModelPosing.ResetTransform();

        if (generateSnapshot)
            Snapshot();
    }

    public async void SnapshotIK()
    {
        if(SkeletonPosing.PoseInfo.HasIKStacks)
        {
            var boneFilter = new BoneFilter(_posingService);
            // TODO: This is annoying
            boneFilter.AddExcludedPrefix("j_mune");
            boneFilter.AddExcludedPrefix("n_sippo");
            var all = new PoseImporterOptions(boneFilter, TransformComponents.All, true);
            var poseFile = await _framework.RunOnTick(() => GeneratePoseFile(), delayTicks: 2);
            Reset(false);
            ImportPose(poseFile, options: all, generateSnapshot: true);
        }
    }

    private PoseFile GeneratePoseFile()
    {
        var poseFile = new PoseFile();
        SkeletonPosing.ExportSkeletonPose(poseFile);
        ModelPosing.ExportModelPose(poseFile);
        return poseFile;
    }

    internal record struct PoseStack(PoseInfo Info, Transform ModelTransform);
}
