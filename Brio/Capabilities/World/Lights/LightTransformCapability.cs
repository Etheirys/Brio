using Brio.Config;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.Input;
using Brio.UI.Widgets.World.Lights;
using Brio.UI.Windows.Specialized;
using System.Collections.Generic;
using System.Numerics;

using StructsQuaternion = FFXIVClientStructs.FFXIV.Common.Math.Quaternion;
using StructsTransforms = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;
using StructsVector3 = FFXIVClientStructs.FFXIV.Common.Math.Vector3;

namespace Brio.Capabilities.World;

public class LightTransformCapability : LightCapability
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

    public bool HasOverride
    {
        get => rotation != Vector3.Zero || position != Vector3.Zero;
    }

    public bool CanUndo => _undoStack.Count is not 0 and not 1;
    public bool CanRedo => _redoStack.Count > 0;


    private Stack<LightStack> _undoStack = [];
    private Stack<LightStack> _redoStack = [];

    private readonly PosingTransformWindow _overlayTransformWindow;
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly GameInputService _gameInputService;
    private readonly ConfigurationService _configurationService;

    public bool TransformWindowOpen
    {
        get => _overlayTransformWindow.IsOpen;
        set => _overlayTransformWindow.IsOpen = value;
    }

    public bool IsTransformDraggingActive = false;
    public Vector3 rotation = Vector3.Zero;
    public Vector3 position = Vector3.Zero;
    public Vector3 scale = Vector3.Zero;
    public StructsTransforms Transform = new()
    {
        Position = Vector3.Zero,
        Rotation = Quaternion.Zero,
        Scale = Vector3.One
    };

    public LightTransformCapability(Entity parent,
        GameInputService gameInputService,
        ConfigurationService configurationService,
        PosingTransformWindow overlayTransformWindow,
        PosingOverlayWindow window)
        : base(parent)
    {
        _overlayWindow = window;
        _gameInputService = gameInputService;
        _overlayTransformWindow = overlayTransformWindow;
        _configurationService = configurationService;

        Widget = new LightTransformWidget(this);
    }

    public void Redo()
    {
        if(_redoStack.TryPop(out var redoStack))
        {
            _undoStack.Push(redoStack);
            ApplyState(redoStack);
        }
    }

    public void Undo()
    {
        if(_undoStack.TryPop(out var undoStack))
            _redoStack.Push(undoStack);

        if(_undoStack.TryPeek(out var applicable))
        {
            ApplyState(applicable);
        }
    }

    public unsafe void Reset(bool generateSnapshot = true, bool clearHistStack = true)
    {
        Transform.Position = Light.GameLight.GameLight->Transform.Position = Light.GameLight.SpawnPosition;
        Transform.Rotation = Light.GameLight.GameLight->Transform.Rotation = Light.GameLight.SpawnRotation;
        Transform.Scale = Light.GameLight.GameLight->Transform.Scale = Light.GameLight.SpawnScale;

        rotation = Vector3.Zero;
        position = Vector3.Zero;
        scale = Vector3.Zero;

        if(clearHistStack)
            _redoStack.Clear();

        if(generateSnapshot)
            Snapshot();
    }

    public void Snapshot()
    {
        var undoStackSize = _configurationService.Configuration.Posing.UndoStackSize;
        if(undoStackSize <= 0)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            return;
        }

        _redoStack.Clear();

        if(_undoStack.Count == 0)
            _undoStack.Push(new LightStack(GameLight.SpawnPosition, GameLight.SpawnRotation, GameLight.SpawnScale));

        _undoStack.Push(new LightStack(Transform.Position, Transform.Rotation, Transform.Scale));
        _undoStack = _undoStack.Trim(undoStackSize + 1);
    }

    private unsafe void ApplyState(LightStack state)
    {
        rotation = state.Rotation.EulerAngles;
        position = state.Position;
        scale = state.Scale;

        Light.GameLight.GameLight->Transform.Position = Transform.Position = state.Position;
        Light.GameLight.GameLight->Transform.Rotation = Transform.Rotation = state.Rotation;
        Light.GameLight.GameLight->Transform.Scale = Transform.Scale = state.Scale;
    }
}

public record struct LightStack(StructsVector3 Position, StructsQuaternion Rotation, StructsVector3 Scale);
