﻿using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Entities.Core;
using Brio.Files;
using Brio.Game.Posing;
using Brio.Input;
using Brio.Resources;
using Brio.UI.Widgets.Posing;
using Brio.UI.Windows.Specialized;
using Dalamud.Plugin.Services;
using OneOf;
using OneOf.Types;
using System.Collections.Generic;
using System.Linq;

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
    private readonly InputService _input;

    public PosingCapability(
        ActorEntity parent,
        PosingOverlayWindow window,
        PosingService posingService,
        ConfigurationService configurationService,
        IFramework framework,
        InputService input)
        : base(parent)
    {
        Widget = new PosingWidget(this);
        _overlayWindow = window;
        _posingService = posingService;
        _configurationService = configurationService;
        _framework = framework;
        _input = input;
    }

    public override void OnEntitySelected()
    {
        base.OnEntitySelected();

        _input.AddListener(KeyBindEvents.Posing_ToggleOverlay, ToggleOverlay);
        _input.AddListener(KeyBindEvents.Posing_Undo, Undo);
        _input.AddListener(KeyBindEvents.Posing_Redo, Redo);
    }

    public override void OnEntityDeselected()
    {
        base.OnEntityDeselected();

        _input.RemoveListener(KeyBindEvents.Posing_ToggleOverlay, ToggleOverlay);
        _input.RemoveListener(KeyBindEvents.Posing_Undo, Undo);
        _input.RemoveListener(KeyBindEvents.Posing_Redo, Redo);
    }

    public void ClearSelection() => Selected = PosingSelectionType.None;

    public void ImportPose(string path, PoseImporterOptions? options = null)
    {
        try
        {
            if(path.EndsWith(".cmp"))
            {
                ImportPose(ResourceProvider.Instance.GetFileDocument<CMToolPoseFile>(path), options, reset: false, reconcile: false);
                return;
            }

            ImportPose(ResourceProvider.Instance.GetFileDocument<PoseFile>(path), options, reset: false, reconcile: false);
        }
        catch
        {
            EventBus.Instance.NotifyError("Invalid pose file.");
        }
    }

    public void ImportPose(OneOf<PoseFile, CMToolPoseFile> rawPoseFile, PoseImporterOptions? options = null, bool generateSnapshot = true, bool reset = true, bool reconcile = true)
    {
        var poseFile = rawPoseFile.Match(
                poseFile => poseFile,
                cmToolPoseFile => cmToolPoseFile.Upgrade()
            );

        if(!poseFile.Bones.Any() && !poseFile.MainHand.Any() && !poseFile.OffHand.Any())
        {
            EventBus.Instance.NotifyError("Invalid pose file.");
            return;
        }

        poseFile.SanitizeBoneNames();

        options ??= _posingService.DefaultImporterOptions;

        if(options.ApplyModelTransform && reset)
            ModelPosing.ResetTransform();

        SkeletonPosing.ImportSkeletonPose(poseFile, options);
        ModelPosing.ImportModelPose(poseFile, options);

        if(generateSnapshot)
            _framework.RunOnTick(() => Snapshot(reset, reconcile), delayTicks: 2);
    }

    public void ExportPose(string path)
    {
        var poseFile = GeneratePoseFile();
        ResourceProvider.Instance.SaveFileDocument(path, poseFile);
    }

    public void Snapshot(bool reset = true, bool reconcile = true)
    {
        var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
        if(undoStackSize <= 0)
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

        if(reconcile)
            Reconcile(reset);
    }

    public void Redo()
    {
        if(_redoStack.TryPop(out var redoStack))
        {
            _undoStack.Push(redoStack);
            SkeletonPosing.PoseInfo = redoStack.Info.Clone();
            ModelPosing.Transform = redoStack.ModelTransform;
        }
    }

    public void Undo()
    {
        if(_undoStack.TryPop(out var undoStack))
            _redoStack.Push(undoStack);

        if(_undoStack.TryPeek(out var applicable))
        {
            SkeletonPosing.PoseInfo = applicable.Info.Clone();
            ModelPosing.Transform = applicable.ModelTransform;
        }
    }

    public void Reset(bool generateSnapshot = true, bool reset = true)
    {
        SkeletonPosing.ResetPose();
        ModelPosing.ResetTransform();

        if(generateSnapshot)
            Snapshot(reset);
    }

    public void ToggleOverlay()
    {
        OverlayOpen = !OverlayOpen;
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
            ImportPose(poseFile, options: all, generateSnapshot: false);
        }, delayTicks: 2);
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
