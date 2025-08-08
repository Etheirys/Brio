using Brio.Capabilities.World;
using Brio.Game.Types;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class WeatherWidget(WeatherCapability weatherCapability) : Widget<WeatherCapability>(weatherCapability)
{
    public override string HeaderName => "Weather";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

    private static readonly WeatherSelector _weatherSelector = new("global_weather_selector");

    public override void DrawBody()
    {
        var isLocked = Capability.WeatherService.WeatherOverrideEnabled;
        var isLockedPrevious = isLocked;
        var currentWeather = (int)Capability.WeatherService.CurrentWeather;
        var previousWeather = currentWeather;

        Vector2 unlockPos = Vector2.Zero;


        if(ImBrio.BorderedGameIcon("current_weather", (WeatherUnion)Capability.WeatherService.CurrentWeather, showText: false))
        {
            _weatherSelector.SetNaturalWeathers(Capability.WeatherService.TerritoryWeatherTable);
            _weatherSelector.Select(Capability.WeatherService.CurrentWeather);
            ImGui.OpenPopup("weather_selector");
        }

        ImGui.SameLine();
        var startAt = ImGui.GetCursorPos();

        ImGui.SetNextItemWidth(-((ImGui.GetStyle().FramePadding.X * 2) + ImGui.CalcTextSize("XXX").X));
        ImGui.InputInt("###current_weather_input", ref currentWeather, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.SameLine();
        unlockPos = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(startAt.X);
        ImGui.SetCursorPosY(startAt.Y + (ImGui.GetTextLineHeight() * 1.2f));
        WeatherUnion union = (WeatherId)currentWeather;
        union.Switch(
            row => ImGui.Text(row.Name.ToString()),
            none => ImGui.NewLine()
        );

        using(var popup = ImRaii.Popup("weather_selector"))
        {
            if(popup.Success)
            {
                _weatherSelector.Draw();

                if(_weatherSelector.SoftSelectionChanged && _weatherSelector.SoftSelected != null)
                {
                    currentWeather = _weatherSelector.SoftSelected.Match(
                        naturalWeather => (int)naturalWeather.RowId,
                        weatherOverride => 0
                    );
                }

                if(_weatherSelector.SelectionChanged)
                    ImGui.CloseCurrentPopup();
            }
        }

        var preservePos = ImGui.GetCursorPos();
        ImGui.SetCursorPos(unlockPos);
        if(isLocked)
        {
            if(ImBrio.FontIconButtonRight("lock", FontAwesomeIcon.Unlock, 1, "Unlock Weather", bordered: false))
                isLocked = false;
        }
        else
        {
            if(ImBrio.FontIconButtonRight("lock", FontAwesomeIcon.Lock, 1, "Lock Weather", bordered: false))
                isLocked = true;
        }
        ImGui.SetCursorPos(preservePos);

        if(currentWeather != previousWeather)
        {
            isLocked = true;
            Capability.WeatherService.CurrentWeather = currentWeather;
        }

        if(isLocked != isLockedPrevious)
            Capability.WeatherService.WeatherOverrideEnabled = isLocked;
    }
}
