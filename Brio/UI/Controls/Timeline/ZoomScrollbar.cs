
// This is made possable by the help of 'NoHideout' (Leyla) & `Glorou` (Karou)
// Very kindly taken and adapted for Brio from (https://github.com/NoHideout/TimelineAnimator/blob/master/TimelineAnimator/ImSequencer/ZoomScrollbar.cs)

// Originally based on ImSequencer from ImGuizmo
// Copyright (c) 2016-2026 Cedric Guillemet and contributors
// Modifications Copyright (c) 2026 NoHideout, Brio contributors 
//
// Original ImSequencer code licensed under the MIT License.

using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Timeline;

public sealed class ZoomScrollbar
{
    public struct State
    {
        public double ContentMin;
        public double ContentMax;
        public double ViewMin;
        public double ViewMax;
        public double MinViewSpan;
        public double ZoomSpeed;
        internal int ActiveId;
        internal double GrabOffset;
    }

    private const int HandleNone = 0;
    private const int HandleLeft = 1;
    private const int HandleRight = 2;
    private const int HandleCenter = 3;

    public static bool Draw(string id, ref State s, float height = 14.0f)
    {
        var io = ImGui.GetIO();
        var style = ImGui.GetStyle();
        var draw = ImGui.GetWindowDrawList();

        var cursor = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var size = new Vector2(availX, height);

        var contentSpan = s.ContentMax - s.ContentMin;
        if(contentSpan <= 0)
            contentSpan = 1;
        var pxPerUnit = (float)(size.X / contentSpan);

        if(s.ZoomSpeed <= 0)
            s.ZoomSpeed = 0.15;
        if(s.MinViewSpan <= 0)
            s.MinViewSpan = contentSpan * 0.001;

        var dynamicMinSpan = 30.0 / (pxPerUnit > 0 ? pxPerUnit : 1);
        s.MinViewSpan = Math.Max(s.MinViewSpan, dynamicMinSpan);
        s.MinViewSpan = Math.Min(s.MinViewSpan, contentSpan);

        if(s.ContentMax <= s.ContentMin)
            s.ContentMax = s.ContentMin + 1.0;

        var viewMinLimit = Math.Max(s.ContentMin, s.ContentMax - s.MinViewSpan);
        s.ViewMin = Math.Clamp(s.ViewMin, s.ContentMin, viewMinLimit);

        var viewMaxLimit = Math.Min(s.ContentMax, s.ViewMin + s.MinViewSpan);
        s.ViewMax = Math.Clamp(s.ViewMax, viewMaxLimit, s.ContentMax);

        ImGui.PushID(id);
        ImGui.InvisibleButton("##zoomscroll", size);
        var hovered = ImGui.IsItemHovered();

        var colBg = ImGui.GetColorU32(ImGuiCol.FrameBg);
        var colBar = ImGui.GetColorU32(ImGuiCol.SliderGrabActive);
        var colFill = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
        var colGrip = ImGui.GetColorU32(ImGuiCol.ScrollbarGrab);

        var rBgMin = cursor;
        var rBgMax = new Vector2(cursor.X + size.X, cursor.Y + size.Y);
        draw.AddRectFilled(rBgMin, rBgMax, colBg, style.FrameRounding);

        var contentMin = s.ContentMin;

        float XFrom(double content) => (float)((content - contentMin) * pxPerUnit) + cursor.X;

        var x0 = XFrom(s.ViewMin);
        var x1 = XFrom(s.ViewMax);

        var laneY0 = cursor.Y + 0.25f * size.Y;
        var laneY1 = cursor.Y + 0.75f * size.Y;
        draw.AddRectFilled(new Vector2(cursor.X, laneY0), new Vector2(cursor.X + size.X, laneY1), colBar, style.FrameRounding);

        var thumbRounding = style.GrabRounding > 0 ? style.GrabRounding : style.FrameRounding;
        var thumbMin = new Vector2(x0, cursor.Y + 2);
        var thumbMax = new Vector2(x1, cursor.Y + size.Y - 2);
        draw.AddRectFilled(thumbMin, thumbMax, colFill, thumbRounding);

        var gripW = MathF.Max(4f, MathF.Min(10f, (x1 - x0) * 0.15f));
        draw.AddRectFilled(new Vector2(x0, cursor.Y + 2), new Vector2(x0 + gripW, cursor.Y + size.Y - 2), colGrip, thumbRounding);
        draw.AddRectFilled(new Vector2(x1 - gripW, cursor.Y + 2), new Vector2(x1, cursor.Y + size.Y - 2), colGrip, thumbRounding);

        var mx = io.MousePos.X;
        var my = io.MousePos.Y;
        var inThumb = mx >= x0 && mx <= x1 && my >= thumbMin.Y && my <= thumbMax.Y;
        var inLeftGrip = mx >= x0 && mx <= x0 + gripW && inThumb;
        var inRightGrip = mx >= x1 - gripW && mx <= x1 && inThumb;
        var inCenter = inThumb && !inLeftGrip && !inRightGrip;

        if(hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            if(inLeftGrip)
            {
                s.ActiveId = HandleLeft;
                s.GrabOffset = 0;
            }
            else if(inRightGrip)
            {
                s.ActiveId = HandleRight;
                s.GrabOffset = 0;
            }
            else if(inCenter)
            {
                s.ActiveId = HandleCenter;
                s.GrabOffset = mx - x0;
            }
            else
            {
                s.ActiveId = HandleCenter;
                s.GrabOffset = (x1 - x0) * 0.5f;
            }
        }

        if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if(s.ActiveId != HandleNone && s.ViewMax < s.ViewMin + s.MinViewSpan)
                s.ViewMax = s.ViewMin + s.MinViewSpan;

            s.ActiveId = HandleNone;
        }

        var changed = false;

        if(hovered)
        {
            var wheel = io.MouseWheel;
            if(Math.Abs(wheel) > 0.0001f)
            {
                var t = Math.Clamp((mx - cursor.X) / size.X, 0.0, 1.0);
                ZoomAround(ref s, 1.0 - wheel * s.ZoomSpeed, t);
                changed = true;
            }
        }

        if(s.ActiveId != HandleNone && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            var newContentPos = ((mx - cursor.X) / pxPerUnit) + contentMin;

            switch(s.ActiveId)
            {
                case HandleLeft:
                {
                    var newMin = newContentPos;
                    newMin = Math.Min(newMin, s.ViewMax - s.MinViewSpan);
                    newMin = Math.Max(newMin, s.ContentMin);

                    s.ViewMin = newMin;
                    changed = true;
                    break;
                }
                case HandleRight:
                {
                    var newMax = newContentPos;
                    newMax = Math.Max(newMax, s.ViewMin + s.MinViewSpan);
                    newMax = Math.Min(newMax, s.ContentMax);

                    s.ViewMax = newMax;
                    changed = true;
                    break;
                }
                case HandleCenter:
                {
                    var span = s.ViewMax - s.ViewMin;

                    var grabOffsetContent = s.GrabOffset / pxPerUnit;
                    var newMin = newContentPos - grabOffsetContent;
                    var newMax = newMin + span;

                    if(newMin < s.ContentMin)
                    {
                        newMin = s.ContentMin;
                        newMax = newMin + span;
                    }

                    if(newMax > s.ContentMax)
                    {
                        newMax = s.ContentMax;
                        newMin = newMax - span;
                    }

                    s.ViewMin = newMin;
                    s.ViewMax = newMax;
                    changed = true;
                    break;
                }
            }
        }

        if(hovered && ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            var wheel = io.MouseWheel;
            if(Math.Abs(wheel) > 0.0001f)
            {
                var t = Math.Clamp((mx - cursor.X) / size.X, 0.0, 1.0);
                ZoomAround(ref s, 1.0 - wheel * (s.ZoomSpeed * 1.75), t);
                changed = true;
            }
        }

        ImGui.PopID();
        return changed;
    }

    private static void ZoomAround(ref State s, double zoomFactor, double pivot01)
    {
        var span = s.ViewMax - s.ViewMin;
        var center = s.ViewMin + span * pivot01;

        var newSpan = Math.Max(s.MinViewSpan, Math.Min(s.ContentMax - s.ContentMin, span * zoomFactor));
        var newMin = center - newSpan * pivot01;
        var newMax = newMin + newSpan;

        if(newMin < s.ContentMin)
        {
            newMin = s.ContentMin;
            newMax = newMin + newSpan;
        }

        if(newMax > s.ContentMax)
        {
            newMax = s.ContentMax;
            newMin = newMax - newSpan;
        }

        s.ViewMin = newMin;
        s.ViewMax = newMax;
    }
}
