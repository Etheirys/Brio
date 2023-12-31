using Dalamud.Interface;
using Brio.Entities.Core;
using System;

namespace Brio.Entities.World;

internal class WorldEntity : Entity
{
    public override string FriendlyName => "World";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Globe;
    public override bool IsAttached => true;

    public WorldEntity(IServiceProvider provider) : base("world", provider)
    {
        OnAttached();
    }
}
