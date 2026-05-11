using Brio.Entities.Core;
using Brio.Game.WorldObjects;
using Brio.UI.Widgets.WorldObjects;

namespace Brio.Capabilities.WorldObjects;

public class WorldObjectLifetimeCapability : WorldObjectCapability
{
    private readonly WorldObjectService _bgObjectService;

    public bool CanDestroy => true;
    public bool CanClone => GameBgObject.ObjectType is WorldObjectType.BgObject or WorldObjectType.StaticVfx or WorldObjectType.Prop;

    public WorldObjectLifetimeCapability(Entity parent, WorldObjectService bgObjectService) : base(parent)
    {
        _bgObjectService = bgObjectService;
        Widget = new WorldObjectLifetimeWidget(this);
    }

    public void Destroy()
    {
        if(!CanDestroy) return;
        _bgObjectService.Destroy(GameBgObject);
    }

    public void Clone()
    {
        if(!CanClone) return;
        _bgObjectService.Clone(GameBgObject);
    }

    public void MoveToCamera() => _bgObjectService.MoveToCamera(GameBgObject);
}
