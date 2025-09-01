using Brio.Capabilities.World;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class TimeWidget(TimeCapability timeCapability) : Widget<TimeCapability>(timeCapability)
{
    public override string HeaderName => "Time";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

    public override void DrawBody()
    {
        var unlockPos = Vector2.Zero;
        var isLocked = Capability.TimeService.IsTimeFrozen;
        var isLockedPrevious = isLocked;
        int minuteOfDay = Capability.TimeService.MinuteOfDay;
        int dayOfMonth = Capability.TimeService.DayOfMonth;
        var displayTime = TimeSpan.FromMinutes(minuteOfDay);

        int originalMinute = minuteOfDay;
        int originalDay = dayOfMonth;

        ImGui.Text($"{displayTime.Hours:D2}:{displayTime.Minutes:D2}");
        ImGui.SameLine();

        ImGui.PushItemWidth(-((ImGui.GetStyle().FramePadding.X * 2) + ImGui.CalcTextSize("XXXXXXXXXXXXX").X));
        ImGui.SliderInt("Time of Day", ref minuteOfDay, 0, 1439);
        ImGui.SameLine();
        unlockPos = ImGui.GetCursorPos();
        ImGui.NewLine();
        ImGui.SliderInt("Day of Month", ref dayOfMonth, 1, 31);
        ImGui.PopItemWidth();


        var preservePos = ImGui.GetCursorPos();
        ImGui.SetCursorPos(unlockPos);
        if(ImBrio.FontIconButtonRight("timeLock", isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock, 1, isLocked ? "Unlock Time" : "lock Time", bordered: false))
            isLocked = !isLocked;
        ImGui.SetCursorPos(preservePos);

        if(originalMinute != minuteOfDay)
        {
            isLocked = true;
            Capability.TimeService.MinuteOfDay = minuteOfDay;
        }

        if(originalDay != dayOfMonth)
        {
            isLocked = true;
            Capability.TimeService.DayOfMonth = dayOfMonth;
        }

        if(isLocked != isLockedPrevious)
            Capability.TimeService.IsTimeFrozen = isLocked;
    }
}
