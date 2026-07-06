using Brio.Config;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.Input;
using Brio.Services;
using Brio.UI.Widgets.WorldObjects;
using Brio.UI.Windows.Specialized;
using System.Numerics;

namespace Brio.Capabilities.WorldObjects;

public class WorldObjectTransformCapability : WorldObjectCapability, ITransformable, IHistoryCompatible
{
    private readonly PosingOverlayWindow _overlayWindow;
    private readonly GameInputService _gameInputService;
    private readonly ConfigurationService _configurationService;
    private readonly HistoryService _historyService;

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

    public bool HasOverride
    {
        get => false;
    }

    private Transform _originalTransform;

    public bool IsTransformFrozen { get; set; } = false;

    public bool CanUndo => Entity.EntityManager.CanUndoSelected;
    public bool CanRedo => Entity.EntityManager.CanRedoSelected;

    public bool TransformOverride
    {
        get
        {
            var t = Transform;
            return t.Position != _originalTransform.Position
                || t.Rotation != _originalTransform.Rotation
                || t.Scale != _originalTransform.Scale;
        }
    }

    public Transform Transform
    {
        get => GameBgObject.Transform = GameBgObject.GetTransform();
        set
        {
            if(!IsTransformFrozen)
            {
                GameBgObject.Transform = value;
                GameBgObject.SetTransform(value);
            }
        }
    }

    public Transform OriginalTransform
    {
        get => _originalTransform;
        set => _originalTransform = value;
    }

    public WorldObjectTransformCapability(Entity parent, PosingOverlayWindow overlayWindow, GameInputService gameInputService, ConfigurationService configurationService, HistoryService historyService) : base(parent)
    {
        _originalTransform = GameBgObject.Transform;
        _overlayWindow = overlayWindow;
        _gameInputService = gameInputService;
        _configurationService = configurationService;
        _historyService = historyService;

        Widget = new WorldObjectWidget(this);
    }

    public object CaptureInitialState() => new WorldObjectStack(OriginalTransform.Position, OriginalTransform.Rotation, OriginalTransform.Scale);

    public void ApplyState(object state)
    {
        var stack = (WorldObjectStack)state;
        SetTransform(new Transform { Position = stack.Position, Rotation = stack.Rotation, Scale = stack.Scale });
    }

    public void Snapshot()
    {
        var t = Transform;
        _historyService.Snapshot(Entity.Id, this, new WorldObjectStack(t.Position, t.Rotation, t.Scale));
    }

    public void Undo() => Entity.EntityManager.UndoSelected();
    public void Redo() => Entity.EntityManager.RedoSelected();

    public unsafe void SetTransform(Transform state)
    {
        Transform = state;
    }
}
public record struct WorldObjectStack(Vector3 Position, Quaternion Rotation, Vector3 Scale);
