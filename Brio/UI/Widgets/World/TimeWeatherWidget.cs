using Brio.Capabilities.World;
using Brio.Game.Types;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class TimeWeatherWidget(TimeWeatherCapability weatherCapability) : Widget<TimeWeatherCapability>(weatherCapability)
{
    public const int DayTime = 86400 / 60;

    public override string HeaderName => "Time & Weather";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;


    private static readonly WeatherSelector _weatherSelector = new("global_weather_selector");

    public override void DrawBody()
    {
        var isWeatherOverrideEnabledLocked = Capability.EnvironmentService.WeatherOverrideEnabled;
        var isWeatherOverrideEnabledLockedPrevious = isWeatherOverrideEnabledLocked;
        var currentWeather = (int)Capability.EnvironmentService.CurrentWeather;
        var previousWeather = currentWeather;

        var isTimeFrozen = Capability.TimeService.IsTimeFrozen;
        var isTimeFrozenPrevious = isTimeFrozen;

        int minuteOfDay = Capability.TimeService.MinuteOfDay;
        int dayOfMonth = Capability.TimeService.DayOfMonth;

        Vector2 unlockPos = ImGui.GetCursorPos();

        var dateTime = new DateTime().AddMinutes(minuteOfDay);

        ImBrio.VerticalPadding(5);
        ImGui.Text("Time of Day"u8);
        ImBrio.VerticalPadding(5);

        var preservePostime = ImGui.GetCursorPos();
        ImGui.SetCursorPos(unlockPos);
        if(ImBrio.FontIconButtonRight("timeLock", isTimeFrozen ? FontAwesomeIcon.Unlock : FontAwesomeIcon.Lock, 1, isTimeFrozen ? "Unlock Time" : "Lock Time", bordered: false))
            isTimeFrozen = !isTimeFrozen;
        ImGui.SetCursorPos(preservePostime);

        ImBrio.CenterNextElementWithPadding(15);
        var realTime = ImGui.SliderInt("##time_real"u8, ref minuteOfDay, 0, DayTime - 1, dateTime.ToShortTimeString(), ImGuiSliderFlags.NoInput);
        ImBrio.AttachToolTip("Time of Day");

        var time = false;
        var dragday = false;
        using(ImRaii.ItemWidth((ImBrio.GetRemainingWidth() / 2) - ImGui.GetStyle().ItemInnerSpacing.X))
        {
            time = ImGui.SliderInt("##time_set"u8, ref minuteOfDay, 0, DayTime - 1, "%.0f"u8);
            ImBrio.AttachToolTip("Time of Day (In Minutes)");

            ImGui.SameLine();

            dragday = ImGui.SliderInt("##day_set"u8, ref dayOfMonth, 1, 31);
            ImBrio.AttachToolTip("Day of Month");
        }

        if(realTime || time || dragday)
        {
            isTimeFrozen = true;
            Capability.TimeService.MinuteOfDay = minuteOfDay;
            Capability.TimeService.DayOfMonth = dayOfMonth;
        }

        if(isTimeFrozen != isTimeFrozenPrevious)
            Capability.TimeService.IsTimeFrozen = isTimeFrozen;

        //
        //

        ImBrio.VerticalPadding(10);
        ImGui.Separator();

        unlockPos = ImGui.GetCursorPos();

        ImGui.Text("Current Weather /"u8);
        ImGui.SameLine();
        WeatherUnion union = (WeatherId)currentWeather;
        union.Switch(
            row => ImGui.Text($"[{row.Name.ToString()}]"),
            none => ImGui.Text("Unknown Weather"u8)
        );
        ImBrio.VerticalPadding(5);
       
        var preservePos = ImGui.GetCursorPos();

        ImGui.SetCursorPos(unlockPos);
        if(ImBrio.FontIconButtonRight("weatherLock", isWeatherOverrideEnabledLocked ? FontAwesomeIcon.Unlock : FontAwesomeIcon.Lock, 1, isWeatherOverrideEnabledLocked ? "Unlock Weather" : "Lock Weather", bordered: false))
            isWeatherOverrideEnabledLocked = !isWeatherOverrideEnabledLocked;
        ImGui.SetCursorPos(preservePos);

        ImBrio.CenterNextElementWithPadding(10);
        if(ImBrio.BorderedWeatherGameIcon("current_weather", (WeatherUnion)Capability.EnvironmentService.CurrentWeather, showText: false))
        {
            _weatherSelector.SetNaturalWeathers(Capability.EnvironmentService.TerritoryWeatherTable);
            _weatherSelector.Select(Capability.EnvironmentService.CurrentWeather);
            ImGui.OpenPopup("weather_selector"u8);
        }

        var startAt = ImGui.GetCursorPos();       

        ImGui.SameLine();

        ImBrio.CenterNextElementWithPadding(10);
        ImBrio.VerticalPadding(5);
        ImGui.InputInt("###current_weather_input"u8, ref currentWeather, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue);
        ImBrio.AttachToolTip("Weather ID");
      
        using(var popup = ImRaii.Popup("weather_selector"u8))
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

        if(currentWeather != previousWeather)
        {
            isWeatherOverrideEnabledLocked = true;
            Capability.EnvironmentService.CurrentWeather = currentWeather;
        }

        if(isWeatherOverrideEnabledLocked != isWeatherOverrideEnabledLockedPrevious)
            Capability.EnvironmentService.WeatherOverrideEnabled = isWeatherOverrideEnabledLocked;
    }
}
