using Brio.Capabilities.World;
using Brio.Entities.Core;
using Dalamud.Interface;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.World;

public class EnvironmentContainerEntity(IServiceProvider provider) : Entity("environment", provider)
{
    public override string FriendlyName => "Environment";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.CloudMoon;

    public override int ContextButtonCount => 0;
    public override EntityFlags Flags => EntityFlags.AllowOutsideGpose | EntityFlags.DefaultOpen;

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<EnvironmentLifetimeCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<TimeWeatherCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<SkyEditorCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<EnvironmentEditorCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<FestivalCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<WorldRenderingCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<DebugEnvironmentCapability>(_serviceProvider, this));
    }

    public override void OnChildAttached() => SortChildren();
    public override void OnChildDetached() => SortChildren();

    private void SortChildren()
    {
        _children.Sort((a, b) =>
        {
            if(a is LightEntity actorA && b is LightEntity actorB)
                return actorA.GameLight.Index.CompareTo(actorB.GameLight.Index);

            return string.Compare(a.Id.Unique, b.Id.Unique, StringComparison.Ordinal);
        });
    }
}
