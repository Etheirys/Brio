using Brio.Game.World;
using ImGuiNET;
using System;

namespace Brio.UI.Components;
public static class TimeControls
{
    public static void Draw()
    {
        if(ImGui.CollapsingHeader("Time Control", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var isLocked = TimeService.Instance.TimeOverrideEnabled;
            var isLockedPrevious = isLocked;
            ImGui.Checkbox("Lock Time", ref isLocked);
            if(isLocked != isLockedPrevious) 
                TimeService.Instance.TimeOverrideEnabled = isLocked;

            long currentTime = TimeService.Instance.EorzeaTime;

            long timeVal = currentTime % 2764800;
            long secondInDay = timeVal % 86400;
            int timeOfDay = (int)(secondInDay / 60f);
            int dayOfMonth = (int)(Math.Floor(timeVal / 86400f) + 1);
            var displayTime = TimeSpan.FromMinutes(timeOfDay);

            int originalTime = timeOfDay;
            int originalDay = dayOfMonth;
            if(!isLocked) ImGui.BeginDisabled();

            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Day of Month").X);
            ImGui.SliderInt("Time of Day", ref timeOfDay, 0, 1439, $"{displayTime.Hours:D2}:{displayTime.Minutes:D2}");
            ImGui.SliderInt("Day of Month", ref dayOfMonth, 1, 31);
            ImGui.PopItemWidth();

            if(!isLocked) ImGui.EndDisabled();


            if(originalTime != timeOfDay || originalDay != dayOfMonth)
            {
                long newTime = ((timeOfDay * 60) + (86400 * ((byte)(dayOfMonth) - 1)));

                TimeService.Instance.EorzeaTime = newTime;
            }
        }
    }
}
