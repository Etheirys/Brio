using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.Game.World;
using Brio.UI.Widgets.World;

namespace Brio.Capabilities.World;

public class TimeWeatherCapability : Capability
{
    public WeatherService WeatherService { get; }
    public TimeService TimeService { get; }

    public TimeWeatherCapability(Entity parent, TimeService timeService, WeatherService weatherService) : base(parent)
    {
        WeatherService = weatherService;
        TimeService = timeService;

        Widget = new TimeWeatherWidget(this);
    }
}
