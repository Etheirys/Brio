using System;
using Dalamud.Interface;
using Brio.Capabilities.Debug;
using Brio.Entities.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Brio.Entities.Debug;

internal class DebugEntity(IServiceProvider provider) : Entity(FixedId, provider)
{
    public const string FixedId = "debug_entity";

    public override string FriendlyName => "Debug";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Bug;

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<DebugCapability>(_serviceProvider, this));
    }
}
