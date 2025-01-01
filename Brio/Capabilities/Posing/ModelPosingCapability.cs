using System.Numerics;
using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Core;
using Brio.Entities.Actor;
using Brio.Files;
using Brio.Game.Posing;

namespace Brio.Capabilities.Posing;

internal class ModelPosingCapability : ActorCharacterCapability
{
    public bool HasOverride => _transformOverride.HasValue;

    public unsafe Transform Transform
    {
        get
        {
            if(_transformOverride.HasValue)
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
        if(_originalTransform.HasValue)
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
        if(!options.ApplyModelTransform)
        {
            return;
        }
        
        var temp = new Transform
        {
            Position = options.PositionTransformType switch
            {
                PoseImportTransformType.Difference => Transform.Position + poseFile.ModelDifference.Position,
                PoseImportTransformType.Absolute => poseFile.ModelAbsoluteValues.Position,
                _ => Transform.Position
            },
            Rotation = options.RotationTransformType switch
            {
                PoseImportTransformType.Difference => Quaternion.Normalize(Transform.Rotation * poseFile.ModelDifference.Rotation),
                PoseImportTransformType.Absolute => poseFile.ModelAbsoluteValues.Rotation,
                _ => Transform.Rotation 
            },
            Scale = options.ScaleTransformType switch
            {
                PoseImportTransformType.Difference => Transform.Scale + poseFile.ModelDifference.Scale,
                PoseImportTransformType.Absolute => poseFile.ModelAbsoluteValues.Scale,
                _ => Transform.Scale
            }
        };

        Transform = temp;
    }

    public void ExportModelPose(PoseFile poseFile)
    {
        if(_originalTransform.HasValue)
        {
            poseFile.ModelDifference = Transform.CalculateDiff(_originalTransform.Value);
        }

        poseFile.ModelAbsoluteValues = Transform;
    }
}
