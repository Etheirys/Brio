using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Entities.WorldObjects;
using Brio.Game.WorldObjects;
using System;

namespace Brio.Capabilities.WorldObjects;

public class WorldObjectCapability(Entity parent) : Capability(parent), IDisposable
{
    public WorldObjectEntity BgObjectEntity => (Entity as WorldObjectEntity)!;
    public WorldObject GameBgObject => (WorldObject)BgObjectEntity.GameBgObject; // TODO (Ken) fix this so it doesn't box by adding this to the iterface
}
