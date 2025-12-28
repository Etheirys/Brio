using Dalamud.Bindings.ImGui;
using System;

public sealed class ZoomScrollbar
{
    public struct State
    {
        public double ContentMin;
        public double ContentMax;
        public double ViewMin;
        public double ViewMax;
        public double MinViewSpan;
        public double PanSpeed;
        public double ZoomSpeed;
        internal int ActiveId;
        internal double GrabOffset;
    }

    private const int Handle_None = 0;
    private const int Handle_Left = 1;
    private const int Handle_Right = 2;
    private const int Handle_Center = 3;

    public static bool Draw(string id, ref State s, float height = 22.0f)
    {
        if (s.ZoomSpeed <= 0) s.ZoomSpeed = 0.15;
        if (s.MinViewSpan <= 0) s.MinViewSpan = (s.ContentMax - s.ContentMin) * 0.001;

        if (s.ContentMax <= s.ContentMin) s.ContentMax = s.ContentMin + 1.0;
        s.ViewMin = Math.Clamp(s.ViewMin, s.ContentMin, s.ContentMax - s.MinViewSpan);
        s.ViewMax = Math.Clamp(s.ViewMax, s.ViewMin + s.MinViewSpan, s.ContentMax);

        var io = ImGui.GetIO();
        var style = ImGui.GetStyle();
        var draw = ImGui.GetWindowDrawList();

        var cursor = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var size = new System.Numerics.Vector2(availX, height);

        ImGui.PushID(id);
        ImGui.InvisibleButton("##zoomscroll", size);
        bool hovered = ImGui.IsItemHovered();
        bool active = ImGui.IsItemActive();

        uint colBg = ImGui.GetColorU32(ImGuiCol.ChildBg);
        uint colBar = ImGui.GetColorU32(ImGuiCol.ScrollbarGrab);
        uint colFill = ImGui.GetColorU32(ImGuiCol.ScrollbarGrab) * 0x88FFFFFF;
        uint colGrip = ImGui.GetColorU32(ImGuiCol.ScrollbarGrabActive);

        var rBgMin = cursor;
        var rBgMax = new System.Numerics.Vector2(cursor.X + size.X, cursor.Y + size.Y);
        draw.AddRectFilled(rBgMin, rBgMax, colBg, style.FrameRounding);

        double contentSpan = s.ContentMax - s.ContentMin;
        double viewSpan = s.ViewMax - s.ViewMin;

        float pxPerUnit = (float)(size.X / contentSpan);
        double contentMin = s.ContentMin;

        float xFrom(double content)
        {
            return (float)((content - contentMin) * pxPerUnit) + cursor.X;
        }

        float x0 = xFrom(s.ViewMin);
        float x1 = xFrom(s.ViewMax);

        float laneY0 = cursor.Y + 0.25f * size.Y;
        float laneY1 = cursor.Y + 0.75f * size.Y;
        draw.AddRectFilled(new System.Numerics.Vector2(cursor.X, laneY0),
                           new System.Numerics.Vector2(cursor.X + size.X, laneY1),
                           colBar, style.FrameRounding);

        var thumbRounding = style.GrabRounding > 0 ? style.GrabRounding : style.FrameRounding;
        var thumbMin = new System.Numerics.Vector2(x0, cursor.Y + 3);
        var thumbMax = new System.Numerics.Vector2(x1, cursor.Y + size.Y - 3);
        draw.AddRectFilled(thumbMin, thumbMax, colFill, thumbRounding);

        float gripW = MathF.Max(4f, MathF.Min(10f, (x1 - x0) * 0.15f)); // visual only
        draw.AddRectFilled(new System.Numerics.Vector2(x0, cursor.Y + 3),
                           new System.Numerics.Vector2(x0 + gripW, cursor.Y + size.Y - 3),
                           colGrip, thumbRounding);
        draw.AddRectFilled(new System.Numerics.Vector2(x1 - gripW, cursor.Y + 3),
                           new System.Numerics.Vector2(x1, cursor.Y + size.Y - 3),
                           colGrip, thumbRounding);

        var mx = io.MousePos.X;
        var my = io.MousePos.Y;
        bool inThumb = (mx >= x0 && mx <= x1 && my >= thumbMin.Y && my <= thumbMax.Y);
        bool inLeftGrip = (mx >= x0 && mx <= x0 + gripW && inThumb);
        bool inRightGrip = (mx >= x1 - gripW && mx <= x1 && inThumb);
        bool inCenter = (inThumb && !inLeftGrip && !inRightGrip);

        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            if (inLeftGrip) { s.ActiveId = Handle_Left; s.GrabOffset = 0; }
            else if (inRightGrip) { s.ActiveId = Handle_Right; s.GrabOffset = 0; }
            else if (inCenter) { s.ActiveId = Handle_Center; s.GrabOffset = mx - x0; }
            else { s.ActiveId = Handle_Center; s.GrabOffset = (x1 - x0) * 0.5f; }
        }

        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            if (s.ActiveId != Handle_None)
            {
                if (s.ViewMax < s.ViewMin + s.MinViewSpan)
                {
                    s.ViewMax = s.ViewMin + s.MinViewSpan;
                }
            }
            s.ActiveId = Handle_None;
        }

        bool changed = false;

        if (hovered)
        {
            float wheel = io.MouseWheel;
            if (Math.Abs(wheel) > 0.0001f)
            {
                double t = Math.Clamp((mx - cursor.X) / size.X, 0.0, 1.0);
                ZoomAround(ref s, 1.0 - wheel * s.ZoomSpeed, t);
                changed = true;
            }
        }

        if (s.ActiveId != Handle_None && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            double newContentPos = ((mx - cursor.X) / pxPerUnit) + contentMin;

            switch (s.ActiveId)
            {
                case Handle_Left:
                    {
                        double newMin = newContentPos;
                        newMin = Math.Min(newMin, s.ViewMax - s.MinViewSpan);
                        newMin = Math.Max(newMin, s.ContentMin);

                        s.ViewMin = newMin;
                        changed = true;
                        break;
                    }
                case Handle_Right:
                    {
                        double newMax = newContentPos;
                        newMax = Math.Max(newMax, s.ViewMin + s.MinViewSpan);
                        newMax = Math.Min(newMax, s.ContentMax);

                        s.ViewMax = newMax;
                        changed = true;
                        break;
                    }
                case Handle_Center:
                    {
                        double span = s.ViewMax - s.ViewMin;

                        double grabOffsetContent = s.GrabOffset / pxPerUnit;
                        double newMin = newContentPos - grabOffsetContent;
                        double newMax = newMin + span;

                        if (newMin < s.ContentMin)
                        {
                            newMin = s.ContentMin;
                            newMax = newMin + span;
                        }
                        if (newMax > s.ContentMax)
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

        if (hovered && ImGui.IsKeyDown(ImGuiKey.LeftCtrl))
        {
            float wheel = io.MouseWheel;
            if (Math.Abs(wheel) > 0.0001f)
            {
                double t = Math.Clamp((mx - cursor.X) / size.X, 0.0, 1.0);
                ZoomAround(ref s, 1.0 - wheel * (s.ZoomSpeed * 1.75), t);
                changed = true;
            }
        }

        ImGui.PopID();
        return changed;
    }

    private static void ZoomAround(ref State s, double zoomFactor, double pivot01)
    {
        double span = s.ViewMax - s.ViewMin;
        double center = s.ViewMin + span * pivot01;

        double newSpan = Math.Max(s.MinViewSpan, Math.Min((s.ContentMax - s.ContentMin), span * zoomFactor));
        double newMin = center - newSpan * pivot01;
        double newMax = newMin + newSpan;

        if (newMin < s.ContentMin)
        {
            newMin = s.ContentMin;
            newMax = newMin + newSpan;
        }
        if (newMax > s.ContentMax)
        {
            newMax = s.ContentMax;
            newMin = newMax - newSpan;
        }

        s.ViewMin = newMin;
        s.ViewMax = newMax;
    }
}
