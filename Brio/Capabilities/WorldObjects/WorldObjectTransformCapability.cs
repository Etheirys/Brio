using Brio.Core;
using Brio.Entities.Core;
using Brio.UI.Widgets.WorldObjects;

namespace Brio.Capabilities.WorldObjects;

public class WorldObjectTransformCapability : WorldObjectCapability, ITransformable
{
    private Transform _originalTransform;

    public bool IsTransformFrozen { get; set; } = false;

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

    public WorldObjectTransformCapability(Entity parent) : base(parent)
    {
        _originalTransform = GameBgObject.Transform;
        Widget = new WorldObjectTransformWidget(this);
    }

    public void Snapshot() { }
}
