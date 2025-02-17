using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World;

namespace Brio.Capabilities.World;

public class WeatherCapability : Capability
{
    public WeatherService WeatherService { get; }

    public WeatherCapability(Entity parent, WeatherService weatherService) : base(parent)
    {
        WeatherService = weatherService;
        Widget = new WeatherWidget(this);
    }
}
