using Brio.Config;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.Input;
using Brio.Services;
using Brio.UI.Widgets.World.Lights;
using Brio.UI.Windows.Specialized;
using System.Numerics;

using StructsQuaternion = FFXIVClientStructs.FFXIV.Common.Math.Quaternion;
using StructsTransforms = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;
using StructsVector3 = FFXIVClientStructs.FFXIV.Common.Math.Vector3;

namespace Brio.Capabilities.World;

public class LightTransformCapability : LightCapability, ITransformable, IHistoryCompatible
{
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
    public bool IsGismoVisible
    {
        get => GameLight.IsGismoVisible;
        set => GameLight.IsGismoVisible = value;
    }
    public bool IsAdvancedGismoVisible
    {
        get => GameLight.IsAdvancedGismoVisible;
        set => GameLight.IsAdvancedGismoVisible = value;
    }

    public bool HasOverride
    {
        get => rotation != Vector3.Zero || position != Vector3.Zero;
    }

    public bool CanUndo
        => Entity.EntityManager.CanUndoSelected;
    public bool CanRedo
        => Entity.EntityManager.CanRedoSelected;

    private readonly PosingOverlayWindow _overlayWindow;
    private readonly GameInputService _gameInputService;
    private readonly ConfigurationService _configurationService;
    private readonly HistoryService _historyService;
    private readonly LightWindow _lightWindow;

    public bool ShouldHideBodyInHierarchy =>
        _lightWindow.IsOpen && _configurationService.Configuration.Posing.IfLightWindowisOpenDontUseSceneManager;

    public bool LightWindowOpen
    {
        get => _lightWindow.IsOpen;
        set => _lightWindow.IsOpen = value;
    }

    public bool IsTransformDraggingActive = false;

    public bool IsTransformFrozen { get; set; }
    public bool TransformOverride => rotation != Vector3.Zero || position != Vector3.Zero || scale != Vector3.Zero;

    public Transform Transform
    {
        get
        {
            _originalTransform ??= field;
            return field;
        }
        set
        {
            _originalTransform ??= field;
            StructsTransform = new StructsTransforms()
            {
                Position = value.Position,
                Rotation = Quaternion.Multiply(Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z), Transform.Rotation),
                Scale = value.Scale
            };

            SetTransform(value);

            field = value;
        }
    }

    public Transform OriginalTransform
    {
        get => _originalTransform ?? default;
        set => _originalTransform = value;
    }

    private Transform? _originalTransform = null;

    public Vector3 rotation = Vector3.Zero;
    public Vector3 position = Vector3.Zero;
    public Vector3 scale = Vector3.Zero;
    public StructsTransforms StructsTransform = new()
    {
        Position = Vector3.Zero,
        Rotation = Quaternion.Zero,
        Scale = Vector3.One
    };

    public LightTransformCapability(Entity parent,
        GameInputService gameInputService,
        ConfigurationService configurationService,
        HistoryService historyService,
        PosingOverlayWindow window,
        LightWindow lightWindow)
        : base(parent)
    {
        _overlayWindow = window;
        _gameInputService = gameInputService;
        _configurationService = configurationService;
        _historyService = historyService;
        _lightWindow = lightWindow;

        Transform = new Transform
        {
            Position = GameLight.SpawnPosition,
            Rotation = GameLight.SpawnRotation,
            Scale = GameLight.SpawnScale
        };

        IsAdvancedGismoVisible = _configurationService.Configuration.Posing.IsAdvancedGizmoEnabled;

        Widget = new LightTransformWidget(this);
    }

    public override void OnEntitySelected()
    {
        if(_configurationService.Configuration.Posing.AutoSelectTransformOnEntitySelect)
        {
            IsGismoVisible = true;
        }
    }

    public override void OnEntityDeselected()
    {
        IsGismoVisible = false;
    }

    public void Redo()
        => Entity.EntityManager.RedoSelected();

    public void Undo()
        => Entity.EntityManager.UndoSelected();

    public unsafe void Reset(bool generateSnapshot = true, bool clearHistStack = true)
    {
        StructsTransform.Position = Light.GameLight.GameLight->Transform.Position = Light.GameLight.SpawnPosition;
        StructsTransform.Rotation = Light.GameLight.GameLight->Transform.Rotation = Light.GameLight.SpawnRotation;
        StructsTransform.Scale = Light.GameLight.GameLight->Transform.Scale = Light.GameLight.SpawnScale;

        Transform = new Transform()
        {
            Position = Light.GameLight.SpawnPosition,
            Rotation = Light.GameLight.SpawnRotation,
            Scale = Light.GameLight.SpawnScale
        };

        rotation = Vector3.Zero;
        position = Vector3.Zero;
        scale = Vector3.Zero;

        if(clearHistStack)
            _historyService.ClearRedo(Entity.Id);

        if(generateSnapshot)
            Snapshot();
    }

    public object CaptureInitialState()
        => new LightStack(GameLight.SpawnPosition, GameLight.SpawnRotation, GameLight.SpawnScale);

    public void Snapshot()
    {
        _historyService.Snapshot(Entity.Id, this, new LightStack(StructsTransform.Position, StructsTransform.Rotation, StructsTransform.Scale));
    }

    public unsafe void ApplyState(object state)
    {
        var lightStack = (LightStack)state;

        rotation = lightStack.Rotation.EulerAngles;
        position = lightStack.Position;
        scale = lightStack.Scale;

        Light.GameLight.GameLight->Transform.Position = StructsTransform.Position = lightStack.Position;
        Light.GameLight.GameLight->Transform.Rotation = StructsTransform.Rotation = lightStack.Rotation;
        Light.GameLight.GameLight->Transform.Scale = StructsTransform.Scale = lightStack.Scale;

        Transform = new Transform()
        {
            Position = lightStack.Position,
            Rotation = lightStack.Rotation,
            Scale = lightStack.Scale
        };
    }

    public unsafe void SetTransform(Transform transform)
    {
        rotation = transform.Rotation.ToEuler();
        position = transform.Position;
        scale = transform.Scale;

        Light.GameLight.GameLight->Transform.Position = StructsTransform.Position = transform.Position;
        Light.GameLight.GameLight->Transform.Rotation = StructsTransform.Rotation = transform.Rotation;
        Light.GameLight.GameLight->Transform.Scale = StructsTransform.Scale = transform.Scale;
    }
}
public record struct LightStack(StructsVector3 Position, StructsQuaternion Rotation, StructsVector3 Scale);
