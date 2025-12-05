using Brio.Capabilities.Debug;
using Brio.Entities.Core;
using Dalamud.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Debug;

public class DebugEntity(IServiceProvider provider) : Entity(FixedId, provider)
{
    public const string FixedId = "debug_entity";

    public override string FriendlyName => "Debug";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.Bug;

    public override EntityFlags Flags => EntityFlags.AllowOutsideGpose;

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<DebugCapability>(_serviceProvider, this));
    }
}
