using Brio.Capabilities.World;
using Brio.Entities.Core;
using Dalamud.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.World;

public class EnvironmentEntity(IServiceProvider provider) : Entity("environment", provider)
{
    public override string FriendlyName => "Environment";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.MountainSun;

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<TimeCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<WeatherCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<FestivalCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<WorldRenderingCapability>(_serviceProvider, this));
    }
}
