using Brio.Entities.Actor;
using Brio.Game.Posing;
using Brio.Core;
using Brio.Capabilities.Actor;
using Brio.Files;

namespace Brio.Capabilities.Posing;

internal class ModelPosingCapability : ActorCharacterCapability
{
    public bool HasOverride => _transformOverride.HasValue;

    public unsafe Transform Transform
    {
        get
        {
            if (_transformOverride.HasValue)
                return _transformOverride.Value;

            return _transformService.GetTransform(GameObject);
        }
        set
        {
            _originalTransform ??= Transform;
            _transformOverride = value;
            _transformService.SetTransform(GameObject, value);
        }
    }

    public unsafe Transform OriginalTransform
    {
        get
        {
            if(_originalTransform.HasValue)
                return _originalTransform.Value;

            return Transform;
        }
    }

    public Transform? OverrideTransform => _transformOverride;


    private Transform? _transformOverride = null;
    private Transform? _originalTransform = null;

    private readonly ModelTransformService _transformService;

    public ModelPosingCapability(ActorEntity parent, ModelTransformService transformService) : base(parent)
    {
        _transformService = transformService;
    }

    public void ResetTransform()
    {
        _transformOverride = null;
        if (_originalTransform.HasValue)
        {
            _transformService.SetTransform(GameObject, _originalTransform.Value);
            _originalTransform = null;
        }
    }

    public override void Dispose()
    {
        ResetTransform();
    }

    public void ImportModelPose(PoseFile poseFile, PoseImporterOptions options)
    {
        if (options.ApplyModelTransform)
            Transform += poseFile.ModelDifference;
    }

    public void ExportModelPose(PoseFile poseFile)
    {
        if (_originalTransform.HasValue)
            poseFile.ModelDifference = Transform.CalculateDiff(_originalTransform.Value);
    }
}
