using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace ImSequencer.ImCurveEdit
{
    public static unsafe class ImCurveEdit
    {
        private static float Smoothstep(float edge0, float edge1, float x)
        {
            x = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            return x * x * (3 - 2 * x);
        }

        private static float Distance(float x, float y, float x1, float y1, float x2, float y2)
        {
            float A = x - x1;
            float B = y - y1;
            float C = x2 - x1;
            float D = y2 - y1;

            float dot = A * C + B * D;
            float len_sq = C * C + D * D;
            float param = -1.0f;
            if (len_sq > float.Epsilon)
                param = dot / len_sq;

            float xx, yy;

            if (param < 0.0f)
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1.0f)
            {
                xx = x2;
                yy = y2;
            }
            else
            {
                xx = x1 + param * C;
                yy = y1 + param * D;
            }

            float dx = x - xx;
            float dy = y - yy;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private static Vector2[] localOffsets = new Vector2[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1) };

        private static int DrawPoint(CurveContext ctx, Vector2 pos, bool edited)
        {
            int ret = 0;
            var draw = ctx.DrawList;
            var io = ctx.Io;

            Vector2 center = ctx.CanvasToScreen(pos);
            Vector2* offsets = stackalloc Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                offsets[i] = center + localOffsets[i] * 4.5f;
            }

            ImRect anchor = new(center - new Vector2(5, 5), center + new Vector2(5, 5));
            draw->AddConvexPolyFilled(offsets, 4, 0xFFAA00AA);
            if (anchor.Contains(io.MousePos))
            {
                ret = 1;
                if (io.MouseDown[0])
                    ret = 2;
            }
            if (edited)
                draw->AddPolyline(offsets, 4, 0xFFFFFFFF, ImDrawFlags.Closed, 3.0f);
            else if (ret != 0)
                draw->AddPolyline(offsets, 4, 0xFF80B0FF, ImDrawFlags.Closed, 2.0f);
            else
                draw->AddPolyline(offsets, 4, 0xFF0080FF, ImDrawFlags.Closed, 2.0f);

            return ret;
        }

        static bool selectingQuad = false;
        static Vector2 quadSelection;
        static int overCurve = -1;
        static int movingCurve = -1;
        static bool scrollingV = false;
        static HashSet<EditPoint> selection = new();
        static Queue<(EditPoint, bool removeOrAdd)> selectionOpQueue = new();
        static bool overSelectedPoint = false;

        static bool pointsMoved = false;
        static Vector2 mousePosOrigin;
        static ImVector<Vector2> originalPoints;

        public static int Edit(CurveContext ctx, Vector2 size, uint id, ImRect* clippingRect = null, ImVector<EditPoint>* selectedPoints = null)
        {
            int ret = 0;
            var io = ImGui.GetIO();
            var window = ImGuiP.GetCurrentWindow();
            var pos = ImGui.GetCursorScreenPos();
            ImRect bb = new(pos, pos + size);
            ImGuiP.ItemSize(bb);

            if (!ImGuiP.ItemAdd(bb, id, ref bb, ImGuiItemFlags.None))
            {
                return ret;
            }

            bool hovered = ImGuiP.ItemHoverable(bb, id);
            ctx.Io = io;
            ctx.ScreenMin = bb.Min;
            ctx.ScreenMax = bb.Max;
            ctx.ScreenRange = size;
            ctx.Range = ctx.Max - ctx.Min;
            ctx.focused = ImGui.IsItemFocused();
            ImDrawList* draw = ImGui.GetWindowDrawList();
            
            if(clippingRect != null)
                draw->PushClipRect(clippingRect->Min,  clippingRect->Max, false);
            ctx.DrawList = draw;

            int curveCount = ctx.GetCurveCount();

            draw->AddRectFilled(pos, pos + size, ctx.GetBackgroundColor());
            draw->AddLine(ctx.PointToScreen(new(0.0f, 0.5f)), ctx.PointToScreen(new(1.0f, 0.5f)), 0xFF000000, 1.5f);

            bool overCurveOrPoint = false;
            int localOverCurve = -1;
            int highLightedCurveIndex = -1;

            for (int c = 0; c < curveCount; ++c)
            {
                if (c == overCurve || !ctx.IsVisible(c))
                    continue;
                int ptCount = ctx.GetPointCount(c);
                if (ptCount < 1)
                    continue;
                CurveType curveType = ctx.GetCurveType(c);
                if (curveType == CurveType.None)
                    continue;

                DrawCurve(ctx, ref overCurveOrPoint, ref localOverCurve, highLightedCurveIndex, c, ptCount, curveType);
            }

            if (overCurve != -1)
            {
                int c = overCurve;
                int ptCount = ctx.GetPointCount(c);
                CurveType curveType = ctx.GetCurveType(c);
                if (ptCount > 0 && curveType != CurveType.None)
                {
                    DrawCurve(ctx, ref overCurveOrPoint, ref localOverCurve, highLightedCurveIndex, c, ptCount, curveType);
                }
            }

            if (localOverCurve == -1)
                overCurve = -1;

            // move selection

            if (overSelectedPoint && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (io.MouseDelta != Vector2.Zero && selection.Count != 0)
                {
                    if (!pointsMoved)
                    {
                        ctx.BeginEdit(0);
                        mousePosOrigin = io.MousePos;
                        originalPoints.Resize(selection.Count);
                        int index = 0;
                        foreach (var sel in selection)
                        {
                            var pts = ctx.GetPoints(sel.curveIndex);
                            originalPoints[index++] = pts[sel.pointIndex];
                        }
                    }
                    pointsMoved = true;
                    ret = 1;

                    Vector2 delta = ctx.ScreenToCanvas(io.MousePos) - ctx.ScreenToCanvas(mousePosOrigin);
                    int originalIndex = 0;
                    foreach (var sel in selection)
                    {
                        Vector2 p = originalPoints[originalIndex] + delta;
                        int newIndex = ctx.EditPoint(sel.curveIndex, sel.pointIndex, p);
                        if (newIndex != sel.pointIndex)
                        {
                            EditPoint newSel = new(sel.curveIndex, newIndex);
                            selectionOpQueue.Enqueue((sel, false));
                            selectionOpQueue.Enqueue((newSel, true));
                        }
                        originalIndex++;
                    }

                    while (selectionOpQueue.Count > 0)
                    {
                        var result = selectionOpQueue.Dequeue();
                        if (result.removeOrAdd)
                        {
                            selection.Add(result.Item1);
                        }
                        else
                        {
                            selection.Remove(result.Item1);
                        }
                    }
                }
            }

            if (overSelectedPoint && !io.MouseDown[0])
            {
                overSelectedPoint = false;
                if (pointsMoved)
                {
                    pointsMoved = false;
                    ctx.EndEdit();
                }
            }

            // add point
            if (overCurve != -1 && io.MouseDoubleClicked[0])
            {
                Vector2 np = ctx.ScreenToCanvas(io.MousePos);
                ctx.BeginEdit(overCurve);
                ctx.AddPoint(overCurve, np);
                ctx.EndEdit();
                ret = 1;
            }

            // move curve
            // TODO: add undo redo
            if (movingCurve != -1)
            {
                int ptCount = ctx.GetPointCount(movingCurve);
                var pts = ctx.GetPoints(movingCurve);

                if (!pointsMoved)
                {
                    ImGui.SetNextFrameWantCaptureMouse(true);
                    ImGuiP.SetActiveID(id, window);
                    mousePosOrigin = io.MousePos;
                    pointsMoved = true;
                    originalPoints.Resize(ptCount);
                    for (int index = 0; index < ptCount; index++)
                    {
                        originalPoints[index] = pts[index];
                    }
                }
                if (ptCount >= 1)
                {
                    Vector2 delta = ctx.ScreenToCanvas(io.MousePos) - ctx.ScreenToCanvas(mousePosOrigin);
                    for (int p = 0; p < ptCount; p++)
                    {
                        ctx.EditPoint(movingCurve, p, originalPoints[p] + delta);
                    }
                    ret = 1;
                }
                if (!io.MouseDown[0])
                {
                    ImGuiP.ClearActiveID();
                    movingCurve = -1;
                    pointsMoved = false;
                    ctx.EndEdit();
                }
            }
            if (movingCurve == -1 && overCurve != -1 && ImGui.IsMouseClicked(0) && selection.Count != 0 && !selectingQuad)
            {
                movingCurve = overCurve;
                ctx.BeginEdit(overCurve);
            }

            // quad selection
            if (selectingQuad)
            {
                Vector2 bmin = Vector2.Min(quadSelection, io.MousePos);
                Vector2 bmax = Vector2.Max(quadSelection, io.MousePos);
                draw->AddRectFilled(bmin, bmax, 0x40FF0000, 1.0f);
                draw->AddRect(bmin, bmax, 0xFFFF0000, 1.0f);
                ImRect selectionQuad = new(bmin, bmax);
                if (!io.MouseDown[0])
                {
                    if (!io.KeyShift)
                        selection.Clear();
                    // select everythnig is quad
                    for (int c = 0; c < curveCount; c++)
                    {
                        if (!ctx.IsVisible(c))
                            continue;

                        int ptCount = ctx.GetPointCount(c);
                        if (ptCount < 1)
                            continue;

                        var pts = ctx.GetPoints(c);
                        for (int p = 0; p < ptCount; p++)
                        {
                            Vector2 center = ctx.CanvasToScreen(pts[p]);
                            if (selectionQuad.Contains(center))
                                selection.Add(new EditPoint(c, p));
                        }
                    }
                    // done
                    selectingQuad = false;
                }
            }
            if (!overCurveOrPoint && ImGui.IsMouseClicked(0) && !selectingQuad && movingCurve == -1 && !overSelectedPoint && hovered)
            {
                selectingQuad = true;
                quadSelection = io.MousePos;
            }

            if (selectedPoints != null)
            {
                selectedPoints->Resize(selection.Count);
                int index = 0;
                foreach (var point in selection)
                    (*selectedPoints)[index++] = point;
            }

            if (hovered)
            {
                if (MathF.Abs(io.MouseWheel) > float.Epsilon)
                {
                    float r = (io.MousePos.Y - pos.Y) / size.Y;
                    float ratioY = Extensions.ImLerp(ctx.Min.Y, ctx.Max.Y, r);
                    static float ScaleValue(float wheel, float v, float ratio)
                    {
                        v -= ratio;
                        v *= 1.0f - wheel * 0.05f;
                        v += ratio;
                        return v;
                    }

                    var wheel = io.MouseWheel;
                    ctx.Min.Y = ScaleValue(wheel, ctx.Min.Y, ratioY);
                    ctx.Max.Y = ScaleValue(wheel, ctx.Max.Y, ratioY);
                }
                if (!scrollingV && ImGui.IsMouseDown(ImGuiMouseButton.Middle))
                {
                    scrollingV = true;
                }
            }

            if (scrollingV)
            {
                float deltaH = io.MouseDelta.Y * ctx.Range.Y;
                ctx.Min.Y -= deltaH;
                ctx.Max.Y -= deltaH;
                if (!ImGui.IsMouseDown(ImGuiMouseButton.Middle))
                    scrollingV = false;
            }
            if(clippingRect !=  null)
                draw->PopClipRect();
            return ret;
        }

        private static void DrawCurve(CurveContext ctx, ref bool overCurveOrPoint, ref int localOverCurve, int highLightedCurveIndex, int c, int ptCount, CurveType curveType)
        {
            ctx.Io = ImGui.GetIO();
            ctx.DrawList = ImGui.GetWindowDrawList();
            var io = ctx.Io;
            var draw = ctx.DrawList;
            var pts = ctx.GetPoints(c);
            uint curveColor = ctx.GetCurveColor(c);
            if (c == highLightedCurveIndex && selection.Count == 0 && !selectingQuad || movingCurve == c)
                curveColor = 0xFFFFFFFF;

            for (int p = 0; p < ptCount - 1; p++)
            {
                Vector2 p1 = ctx.CanvasToPoint(pts[p]);
                Vector2 p2 = ctx.CanvasToPoint(pts[p + 1]);

                if (curveType == CurveType.CurveSmooth || curveType == CurveType.CurveLinear)
                {
                    nuint subStepCount = (curveType == CurveType.CurveSmooth) ? 20u : 2u;
                    float step = 1.0f / (subStepCount - 1);
                    for (nuint substep = 0; substep < subStepCount - 1; substep++)
                    {
                        float t = substep * step;

                        Vector2 sp1 = Vector2.Lerp(p1, p2, t);
                        Vector2 sp2 = Vector2.Lerp(p1, p2, t + step);

                        float rt1 = Smoothstep(p1.X, p2.X, sp1.X);
                        float rt2 = Smoothstep(p1.X, p2.X, sp2.X);

                        Vector2 pos1 = ctx.PointToScreen(new(sp1.X, Extensions.ImLerp(p1.Y, p2.Y, rt1)));
                        Vector2 pos2 = ctx.PointToScreen(new(sp2.X, Extensions.ImLerp(p1.Y, p2.Y, rt2)));

                        if (Distance(io.MousePos.X, io.MousePos.Y, pos1.X, pos1.Y, pos2.X, pos2.Y) < 8.0f && !scrollingV)
                        {
                            localOverCurve = c;
                            overCurve = c;
                            overCurveOrPoint = true;
                        }

                        draw->AddLine(pos1, pos2, curveColor, 1.3f);
                    } // substep
                }
                else if (curveType == CurveType.CurveDiscrete)
                {
                    Vector2 dp1 = ctx.PointToScreen(p1);
                    Vector2 dp2 = ctx.PointToScreen(new(p2.X, p1.Y));
                    Vector2 dp3 = ctx.PointToScreen(p2);
                    draw->AddLine(dp1, dp2, curveColor, 1.3f);
                    draw->AddLine(dp2, dp3, curveColor, 1.3f);

                    if (Distance(io.MousePos.X, io.MousePos.Y, dp1.X, dp1.Y, dp3.X, dp1.Y) < 8.0f ||
                       Distance(io.MousePos.X, io.MousePos.Y, dp3.X, dp1.Y, dp3.X, dp3.Y) < 8.0f
                       /*&& localOverCurve == -1*/)
                    {
                        localOverCurve = c;
                        overCurve = c;
                        overCurveOrPoint = true;
                    }
                }
            }

            for (int p = 0; p < ptCount; p++)
            {
                int drawState = DrawPoint(ctx, pts[p], selection.Contains(new EditPoint(c, p)) && movingCurve == -1 && !scrollingV);
                if (drawState != 0 && movingCurve == -1 && !selectingQuad)
                {
                    overCurveOrPoint = true;
                    overSelectedPoint = true;
                    overCurve = -1;
                    if (drawState == 2)
                    {
                        var point = new EditPoint(c, p);
                        if (!io.KeyShift && !selection.Contains(point))
                            selection.Clear();
                        selection.Add(point);
                    }
                }
            }
        }
    }
}
