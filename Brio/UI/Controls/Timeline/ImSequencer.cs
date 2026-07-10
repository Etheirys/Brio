
// This is made possable by the help of 'NoHideout' (Leyla) & `Glorou` (Karou)
// Very kindly taken and adapted for Brio from (https://github.com/NoHideout/TimelineAnimator/tree/master/TimelineAnimator/ImSequencer)

// Originally based on ImSequencer from ImGuizmo
// Copyright (c) 2016-2026 Cedric Guillemet and contributors
// Modifications Copyright (c) 2026 NoHideout, Brio contributors 
//
// Original ImSequencer code licensed under the MIT License.

using Brio.Services.Timeline;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Timeline;

public class ImRect(Vector2 min, Vector2 max)
{
    public Vector2 Min { get; } = min;
    public Vector2 Max { get; } = max;

    public bool Contains(Vector2 p) => p.X >= Min.X && p.Y >= Min.Y && p.X < Max.X && p.Y < Max.Y;

    public bool Overlaps(ImRect r) => r.Min.X < Max.X && r.Max.X > Min.X && r.Min.Y < Max.Y && r.Max.Y > Min.Y;
}

public struct SelectedKeyframe(int trackIndex, Guid keyframeId) : IEquatable<SelectedKeyframe>
{
    public int TrackIndex = trackIndex;
    public Guid KeyframeId = keyframeId;

    public readonly bool Equals(SelectedKeyframe other) => TrackIndex == other.TrackIndex && KeyframeId.Equals(other.KeyframeId);
    public override readonly bool Equals(object? obj) => obj is SelectedKeyframe other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(TrackIndex, KeyframeId);
}

public class ImSequencerState
{
    public float FramePixelWidth = 10f;
    public HashSet<SelectedKeyframe> SelectedKeyframes = [];

    public float LegendWidth = 180f;
    public int SelectedEntry = -1;

    public bool IsDraggingSplitter;

    public bool MovingCurrentFrame;
    public int MovingPos = -1;
    public bool IsDragging;

    public bool IsBoxSelecting;
    public Vector2 BoxSelectionStart;
    public Vector2 BoxSelectionEnd;

    public ZoomScrollbar.State ZoomState;

    public int ContextTrackIndex = -1;
    public TrackKeyframe? ContextKeyframe;
    public int ContextMouseFrame = -1;
}

public class ImSequencer
{
    public ImGuiCol ColorContentBackground { get; set; } = ImGuiCol.ChildBg;
    public ImGuiCol ColorHeaderBackground { get; set; } = ImGuiCol.TableHeaderBg;
    public ImGuiCol ColorHeaderText { get; set; } = ImGuiCol.Text;
    public ImGuiCol ColorHeaderLines { get; set; } = ImGuiCol.Border;
    public ImGuiCol ColorLegendText { get; set; } = ImGuiCol.Text;
    public ImGuiCol ColorStripe1 { get; set; } = ImGuiCol.FrameBg;
    public ImGuiCol ColorStripe2 { get; set; } = ImGuiCol.FrameBgHovered;
    public float ColorContentLinesAlpha { get; set; } = 0.188f;
    public float ColorSelectionAlpha { get; set; } = 0.25f;
    public ImGuiCol ColorKeyframeHover { get; set; } = ImGuiCol.ResizeGripActive;
    public ImGuiCol ColorKeyframeSelected { get; set; } = ImGuiCol.PlotLinesHovered;
    public ImGuiCol ColorKeyframeDefault { get; set; } = ImGuiCol.TabHovered;
    public ImGuiCol ColorPlayhead { get; set; } = ImGuiCol.ButtonActive;
    public ImGuiCol ColorPlayheadText { get; set; } = ImGuiCol.Text;

    private const float KeyframeHalfSize = 6f;

    private struct RenderContext
    {
        public ImDrawListPtr DrawList;
        public ImDrawListPtr ParentDrawList;
        public ImGuiIOPtr IO;
        public Vector2 CanvasPos;
        public Vector2 CanvasSize;
        public Vector2 ContentMin;
        public Vector2 ContentMax;
        public float LegendWidth;
        public float LeftOffset;
        public float ItemHeight;
        public float Scale;
    }

    private static TimelineTrack? GetTrack(List<TimelineTrack> tracks, int index) => index >= 0 && index < tracks.Count ? tracks[index] : null;

    public bool Draw(string sequenceName, ImSequencerState state, List<TimelineTrack> tracks, int frameMin, int frameMax, ref int currentFrame, ref int selectedEntry)
    {
        var ret = false;
        var io = ImGui.GetIO();
        var scale = ImGuiHelpers.GlobalScale;
        var itemHeight = 20f * scale;

        var splitterGap = 4f * scale;
        var scrollbarHeight = 14.0f * scale;
        var requestContextMenu = false;
        var isSplitterHoveredOrActive = false;

        var visibleTracks = new List<TimelineTrack>();
        var expandedParents = new HashSet<string>();

        foreach(var t in tracks)
        {
            var hasNoLoadedParent = string.IsNullOrEmpty(t.ParentName);
            var parentIsExpanded = hasNoLoadedParent || expandedParents.Contains(t.ParentName);

            if(parentIsExpanded)
            {
                visibleTracks.Add(t);
                if(t.IsExpanded)
                    expandedParents.Add(t.Name);
            }
        }

        ImGui.BeginGroup();
        try
        {
            var parentDrawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var canvasSize = ImGui.GetContentRegionAvail();

            var minLegendWidth = 50f * scale;
            var maxLegendWidth = Math.Max(minLegendWidth, canvasSize.X - minLegendWidth);
            state.LegendWidth = Math.Clamp(state.LegendWidth, minLegendWidth, maxLegendWidth);

            var leftOffset = state.LegendWidth + splitterGap;
            var viewWidthPixels = Math.Max(1f, canvasSize.X - leftOffset - ImGui.GetStyle().ScrollbarSize);

            state.ZoomState.MinViewSpan = Math.Max(state.ZoomState.MinViewSpan, 5);
            CalculateZoomAndSpan(state, frameMin, frameMax, viewWidthPixels);
            var firstFrameUsed = (int)Math.Round(state.ZoomState.ViewMin);

            DrawHeader(canvasPos, canvasSize, itemHeight, scale);

            ImGui.SetCursorScreenPos(new Vector2(canvasPos.X + state.LegendWidth - (3f * scale), canvasPos.Y));
            ImGui.InvisibleButton("##headerSplitter", new Vector2(8f * scale, itemHeight));
            if(ImGui.IsItemActive())
                state.LegendWidth += io.MouseDelta.X;
            if(ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
                isSplitterHoveredOrActive = true;
            }

            var spacingY = ImGui.GetStyle().ItemSpacing.Y;
            var childFrameSize = new Vector2(canvasSize.X, Math.Max(10f * scale, canvasSize.Y - itemHeight - scrollbarHeight - (spacingY * 2)));
            var totalUiHeight = itemHeight + childFrameSize.Y + scrollbarHeight;

            ImGui.SetCursorScreenPos(new Vector2(canvasPos.X, canvasPos.Y + itemHeight));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ColorContentBackground));

            ImGui.BeginChild(sequenceName, childFrameSize, false, ImGuiWindowFlags.AlwaysVerticalScrollbar);
            try
            {
                var childWidth = ImGui.GetContentRegionAvail().X;
                var controlHeight = visibleTracks.Count * itemHeight;

                ImGui.InvisibleButton("contentBar", new Vector2(childWidth, controlHeight));
                ImGui.SetItemAllowOverlap();

                var contentMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();

                var contentMax = new Vector2(itemMax.X, Math.Max(itemMax.Y, canvasPos.Y + itemHeight + childFrameSize.Y));

                var ctx = new RenderContext
                {
                    DrawList = ImGui.GetWindowDrawList(),
                    ParentDrawList = parentDrawList,
                    IO = io,
                    CanvasPos = canvasPos,
                    CanvasSize = canvasSize,
                    ContentMin = contentMin,
                    ContentMax = contentMax,
                    LegendWidth = state.LegendWidth,
                    LeftOffset = leftOffset,
                    ItemHeight = itemHeight,
                    Scale = scale
                };

                var totalPossibleStripes = (int)((ctx.ContentMax.Y - ctx.ContentMin.Y) / ctx.ItemHeight);
                DrawTrackStripes(ctx, Math.Max(visibleTracks.Count, totalPossibleStripes));

                DrawGridLines(ctx, state, frameMin, frameMax);
                DrawHeaderTicks(ctx, state, frameMin, frameMax);

                DrawLegend(ctx, tracks, visibleTracks, ref selectedEntry);
                var clickedOnKeyframe = DrawKeyframes(ctx, state, tracks, visibleTracks, ref requestContextMenu);

                if(!state.IsDraggingSplitter)
                {
                    HandleSelectionAndInput(ctx, state, tracks, visibleTracks, ref selectedEntry, clickedOnKeyframe, ref requestContextMenu);
                    HandlePlayhead(ctx, state, frameMin, frameMax, ref currentFrame, firstFrameUsed);
                }

                if(state.IsDragging)
                    ret = ProcessDragging(ctx, state, tracks, frameMin, frameMax);

                var scrollY = ImGui.GetScrollY();
                ImGui.SetCursorPos(new Vector2(state.LegendWidth - (3f * scale), scrollY));
                ImGui.InvisibleButton("##timelineSplitter", new Vector2(8f * scale, childFrameSize.Y));
                if(ImGui.IsItemActive())
                    state.LegendWidth += io.MouseDelta.X;
                if(ImGui.IsItemHovered() || ImGui.IsItemActive())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
                    isSplitterHoveredOrActive = true;
                }
            }
            finally
            {
                ImGui.EndChild();
                ImGui.PopStyleColor();
            }

            DrawScrollbar(state, viewWidthPixels, leftOffset, scrollbarHeight, scale);

            ImGui.SetCursorScreenPos(new Vector2(canvasPos.X + state.LegendWidth - (3f * scale), canvasPos.Y + itemHeight + childFrameSize.Y));
            ImGui.InvisibleButton("##scrollSplitter", new Vector2(8f * scale, scrollbarHeight));
            if(ImGui.IsItemActive())
                state.LegendWidth += io.MouseDelta.X;
            if(ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
                isSplitterHoveredOrActive = true;
            }

            state.LegendWidth = Math.Clamp(state.LegendWidth, minLegendWidth, maxLegendWidth);

            var splitLineColor = isSplitterHoveredOrActive ? ImGui.GetColorU32(ImGuiCol.SeparatorHovered) : ImGui.GetColorU32(ColorHeaderLines);

            parentDrawList.AddLine(
                new Vector2(canvasPos.X + state.LegendWidth + scale, canvasPos.Y),
                new Vector2(canvasPos.X + state.LegendWidth + scale, canvasPos.Y + totalUiHeight),
                splitLineColor, 2.0f * scale);
        }
        finally
        {
            ImGui.EndGroup();
        }

        if(requestContextMenu)
            ImGui.OpenPopup("SequencerContextMenu");

        return ret;
    }

    private static void CalculateZoomAndSpan(ImSequencerState state, int frameMin, int frameMax, float viewWidthPixels)
    {
        state.ZoomState.ContentMin = frameMin;
        state.ZoomState.ContentMax = frameMax;
        if(state.ZoomState.ContentMax <= state.ZoomState.ContentMin)
            state.ZoomState.ContentMax = state.ZoomState.ContentMin + 1;

        var contentSpan = state.ZoomState.ContentMax - state.ZoomState.ContentMin;

        if(state.ZoomState.MinViewSpan <= 0)
            state.ZoomState.MinViewSpan = 1;
        if(state.ZoomState.MinViewSpan > contentSpan)
            state.ZoomState.MinViewSpan = contentSpan;

        if(state.ZoomState.ViewMax <= state.ZoomState.ViewMin || double.IsNaN(state.ZoomState.ViewMax))
        {
            state.ZoomState.ViewMin = frameMin;
            var initialSpan = viewWidthPixels / state.FramePixelWidth;
            state.ZoomState.ViewMax = state.ZoomState.ViewMin + (initialSpan <= 0 || double.IsNaN(initialSpan) ? 100 : initialSpan);
        }

        var viewMinLimit = Math.Max(state.ZoomState.ContentMin, state.ZoomState.ContentMax - state.ZoomState.MinViewSpan);
        state.ZoomState.ViewMin = Math.Clamp(state.ZoomState.ViewMin, state.ZoomState.ContentMin, viewMinLimit);

        var viewMaxLimit = Math.Min(state.ZoomState.ContentMax, state.ZoomState.ViewMin + state.ZoomState.MinViewSpan);
        state.ZoomState.ViewMax = Math.Clamp(state.ZoomState.ViewMax, viewMaxLimit, state.ZoomState.ContentMax);

        var viewSpan = Math.Max(state.ZoomState.ViewMax - state.ZoomState.ViewMin, 1);
        state.FramePixelWidth = (float)(viewWidthPixels / viewSpan);
    }

    private void DrawHeader(Vector2 canvasPos, Vector2 canvasSize, float itemHeight, float scale)
    {
        var headerWidth = canvasSize.X - ImGui.GetStyle().ScrollbarSize;
        var headerSize = new Vector2(headerWidth, itemHeight);

        ImGui.InvisibleButton("topBar", headerSize);
        ImGui.GetWindowDrawList().AddRectFilled(canvasPos, canvasPos + headerSize, ImGui.GetColorU32(ColorHeaderBackground));

        ImGui.GetWindowDrawList().AddLine(
            new Vector2(canvasPos.X, canvasPos.Y + itemHeight),
            new Vector2(canvasPos.X + headerWidth, canvasPos.Y + itemHeight),
            ImGui.GetColorU32(ColorHeaderLines), 1.5f * scale);
    }

    private void DrawTrackStripes(RenderContext ctx, int visibleTrackCount)
    {
        for(var i = 0; i < visibleTrackCount; i++)
        {
            var col = (i & 1) != 0 ? ImGui.GetColorU32(ColorStripe1) : ImGui.GetColorU32(ColorStripe2);
            var pos = new Vector2(ctx.ContentMin.X + ctx.LeftOffset, ctx.ContentMin.Y + ctx.ItemHeight * i + ctx.Scale);
            var sz = new Vector2(ctx.ContentMax.X, pos.Y + ctx.ItemHeight - ctx.Scale);
            ctx.DrawList.AddRectFilled(pos, sz, col);
        }
    }

    private void DrawGridLines(RenderContext ctx, ImSequencerState state, int frameMin, int frameMax)
    {
        var modFrameCount = 5;
        var frameStep = 1;
        while(modFrameCount * state.FramePixelWidth < 100 * ctx.Scale)
        {
            modFrameCount *= 2;
            frameStep *= 2;
        }

        var lineColor = ImGui.GetColorU32(new Vector4(1, 1, 1, ColorContentLinesAlpha));

        for(var i = frameMin; i <= frameMax; i += frameStep)
        {
            var px = (float)(ctx.ContentMin.X + (i - state.ZoomState.ViewMin) * state.FramePixelWidth + ctx.LeftOffset);
            if(px <= ctx.ContentMax.X && px >= ctx.ContentMin.X + ctx.LeftOffset)
                ctx.DrawList.AddLine(new Vector2(px, ctx.ContentMin.Y), new Vector2(px, ctx.ContentMax.Y), lineColor, ctx.Scale);
        }
    }

    private void DrawHeaderTicks(RenderContext ctx, ImSequencerState state, int frameMin, int frameMax)
    {
        var modFrameCount = 5;
        var frameStep = 1;
        while(modFrameCount * state.FramePixelWidth < 100 * ctx.Scale)
        {
            modFrameCount *= 2;
            frameStep *= 2;
        }

        var halfModFrameCount = modFrameCount / 2;

        var textColor = ImGui.GetColorU32(ColorHeaderText);
        var lineColor = ImGui.GetColorU32(ColorHeaderLines);

        void DrawLine(int i, float regionHeight)
        {
            var baseIndex = i % modFrameCount == 0 || i == frameMax || i == frameMin;
            var halfIndex = i % halfModFrameCount == 0;
            var px = (float)(ctx.ContentMin.X + (i - state.ZoomState.ViewMin) * state.FramePixelWidth + ctx.LeftOffset);

            var tiretStart = (baseIndex ? 4f : halfIndex ? 10f : 14f) * ctx.Scale;
            var tiretEnd = baseIndex ? regionHeight : ctx.ItemHeight;

            if(px <= ctx.ContentMax.X && px >= ctx.ContentMin.X + ctx.LeftOffset)
                ctx.ParentDrawList.AddLine(new Vector2(px, ctx.CanvasPos.Y + tiretStart), new Vector2(px, ctx.CanvasPos.Y + tiretEnd - ctx.Scale), lineColor, ctx.Scale);

            if(baseIndex && px >= ctx.ContentMin.X + ctx.LeftOffset && px <= ctx.ContentMax.X)
                ctx.ParentDrawList.AddText(new Vector2(px + (3f * ctx.Scale), ctx.CanvasPos.Y), textColor, $"{i}");
        }

        for(var i = frameMin; i <= frameMax; i += frameStep)
            DrawLine(i, ctx.ItemHeight);
        DrawLine(frameMin, ctx.ItemHeight);
        DrawLine(frameMax, ctx.ItemHeight);
    }

    private void DrawLegend(RenderContext ctx, List<TimelineTrack> tracks, List<TimelineTrack> visibleTracks, ref int selectedEntry)
    {
        ctx.DrawList.PushClipRect(ctx.ContentMin, new Vector2(ctx.ContentMin.X + ctx.LegendWidth, ctx.ContentMax.Y), true);
        try
        {
            for(var i = 0; i < visibleTracks.Count; i++)
            {
                var track = visibleTracks[i];
                var indent = track.Depth * 14f * ctx.Scale;
                var tPos = new Vector2(ctx.ContentMin.X + (3f * ctx.Scale) + indent, ctx.ContentMin.Y + i * ctx.ItemHeight + (2f * ctx.Scale));
                var textIndent = 16f * ctx.Scale;

                var rowRect = new ImRect(
                    new Vector2(ctx.ContentMin.X, ctx.ContentMin.Y + i * ctx.ItemHeight),
                    new Vector2(ctx.ContentMin.X + ctx.LegendWidth, ctx.ContentMin.Y + (i + 1) * ctx.ItemHeight));

                var bgCol = (i & 1) != 0 ? ImGui.GetColorU32(ColorStripe1) : ImGui.GetColorU32(ColorStripe2);
                ctx.DrawList.AddRectFilled(rowRect.Min, rowRect.Max, bgCol);

                var absoluteIndex = tracks.IndexOf(track);

                if(selectedEntry == absoluteIndex)
                {
                    var selectionColor = ImGui.GetColorU32(new Vector4(1, 1, 1, ColorSelectionAlpha));
                    ctx.DrawList.AddRectFilled(rowRect.Min, rowRect.Max, selectionColor);
                }

                var hoveredArrow = false;

                if(track.HasChildren)
                {
                    var center = new Vector2(tPos.X + (6f * ctx.Scale), ctx.ContentMin.Y + i * ctx.ItemHeight + (ctx.ItemHeight / 2f));
                    var s = 4f * ctx.Scale;
                    var arrowInset = 2f * ctx.Scale;
                    var hitHalf = new Vector2(8f * ctx.Scale, 8f * ctx.Scale);

                    var arrowRect = new ImRect(center - hitHalf, center + hitHalf);
                    hoveredArrow = arrowRect.Contains(ctx.IO.MousePos);
                    var color = ImGui.GetColorU32(hoveredArrow ? ImGuiCol.Text : ImGuiCol.TextDisabled);

                    if(hoveredArrow && ImGui.IsMouseClicked(0))
                        track.IsExpanded = !track.IsExpanded;

                    if(track.IsExpanded)
                        ctx.DrawList.AddTriangleFilled(center + new Vector2(-s, -s + arrowInset), center + new Vector2(s, -s + arrowInset), center + new Vector2(0, s + arrowInset), color);
                    else
                        ctx.DrawList.AddTriangleFilled(center + new Vector2(-s + arrowInset, -s), center + new Vector2(-s + arrowInset, s), center + new Vector2(s + arrowInset, 0), color);
                }

                var muteCenter = new Vector2(ctx.ContentMin.X + ctx.LegendWidth - (12f * ctx.Scale), ctx.ContentMin.Y + i * ctx.ItemHeight + (ctx.ItemHeight / 2f));
                var muteRect = new ImRect(muteCenter - new Vector2(8f * ctx.Scale, 8f * ctx.Scale), muteCenter + new Vector2(8f * ctx.Scale, 8f * ctx.Scale));
                var hoveredMute = muteRect.Contains(ctx.IO.MousePos);

                if(hoveredMute && ImGui.IsMouseClicked(0))
                    track.IsMuted = !track.IsMuted;

                var muteColor = ImGui.GetColorU32(track.IsMuted || hoveredMute ? ImGuiCol.Text : ImGuiCol.TextDisabled);
                var muteLabel = "M";
                var muteTextSize = ImGui.CalcTextSize(muteLabel);
                ctx.DrawList.AddText(muteCenter - (muteTextSize * 0.5f), muteColor, muteLabel);

                if(!hoveredArrow && !hoveredMute && rowRect.Contains(ctx.IO.MousePos) && ImGui.IsMouseClicked(0))
                    selectedEntry = absoluteIndex;

                var nameColor = ImGui.GetColorU32(track.IsMuted ? ImGuiCol.TextDisabled : ColorLegendText);
                ctx.DrawList.AddText(new Vector2(tPos.X + textIndent, tPos.Y), nameColor, track.DisplayName ?? track.Name ?? $"#{i + 1}");
            }
        }
        finally
        {
            ctx.DrawList.PopClipRect();
        }

        for(var i = 0; i < visibleTracks.Count; i++)
        {
            var absoluteIndex = tracks.IndexOf(visibleTracks[i]);
            if(selectedEntry == absoluteIndex)
            {
                var selectionColor = ImGui.GetColorU32(new Vector4(1, 1, 1, ColorSelectionAlpha));
                ctx.DrawList.AddRectFilled(
                    new Vector2(ctx.ContentMin.X + ctx.LeftOffset, ctx.ContentMin.Y + ctx.ItemHeight * i),
                    new Vector2(ctx.ContentMax.X, ctx.ContentMin.Y + ctx.ItemHeight * (i + 1)), selectionColor);
            }
        }
    }

    private bool DrawKeyframes(RenderContext ctx, ImSequencerState state, List<TimelineTrack> tracks, List<TimelineTrack> visibleTracks, ref bool requestContextMenu)
    {
        var clickedOnKeyframe = false;
        ctx.DrawList.PushClipRect(new Vector2(ctx.ContentMin.X + ctx.LeftOffset, ctx.ContentMin.Y), ctx.ContentMax, true);
        try
        {
            for(var i = 0; i < visibleTracks.Count; i++)
            {
                var track = visibleTracks[i];
                var absoluteIndex = tracks.IndexOf(track);
                var y = ctx.ContentMin.Y + ctx.ItemHeight * i + (ctx.ItemHeight / 2f);

                foreach(var kf in track.Keyframes.ToList())
                {
                    var x = (float)(ctx.ContentMin.X + ctx.LeftOffset + (kf.Frame - state.ZoomState.ViewMin) * state.FramePixelWidth);
                    var size = KeyframeHalfSize * ctx.Scale;
                    var keyframeRect = new ImRect(new Vector2(x - size, y - size), new Vector2(x + size, y + size));

                    var isHovered = keyframeRect.Contains(ctx.IO.MousePos);
                    var isSelected = state.SelectedKeyframes.Contains(new SelectedKeyframe(absoluteIndex, kf.Id));

                    if(isHovered && ImGui.IsMouseClicked((ImGuiMouseButton)1))
                    {
                        state.ContextKeyframe = kf;
                        state.ContextTrackIndex = absoluteIndex;
                        state.ContextMouseFrame = (int)Math.Round((ctx.IO.MousePos.X - (ctx.ContentMin.X + ctx.LeftOffset)) / state.FramePixelWidth + state.ZoomState.ViewMin);
                        requestContextMenu = true;
                        clickedOnKeyframe = true;
                    }

                    if(isHovered && ImGui.IsMouseClicked(0) && !state.IsDragging)
                    {
                        if(!isSelected)
                        {
                            if(!ImGui.GetIO().KeyCtrl)
                                state.SelectedKeyframes.Clear();
                            state.SelectedKeyframes.Add(new SelectedKeyframe(absoluteIndex, kf.Id));
                        }

                        state.IsDragging = true;
                        state.MovingPos = (int)ctx.IO.MousePos.X;
                        clickedOnKeyframe = true;
                    }

                    uint drawColor;
                    if(kf.CustomColor.HasValue)
                    {
                        var baseCol = kf.CustomColor.Value;
                        if(isSelected)
                            drawColor = LerpColorToWhite(baseCol, 0.6f);
                        else if(isHovered)
                            drawColor = LerpColorToWhite(baseCol, 0.3f);
                        else
                            drawColor = baseCol;
                    }
                    else
                    {
                        drawColor = isSelected ? ImGui.GetColorU32(ColorKeyframeSelected) :
                            isHovered ? ImGui.GetColorU32(ColorKeyframeHover) :
                            ImGui.GetColorU32(ColorKeyframeDefault);
                    }

                    if(x >= ctx.ContentMin.X + ctx.LeftOffset && x <= ctx.ContentMax.X)
                    {
                        var shape = kf.InterpolationMode == InterpolationMode.Step ? KeyframeShape.Square : kf.Shape;
                        switch(shape)
                        {
                            case KeyframeShape.Circle:
                                ctx.DrawList.AddCircleFilled(new Vector2(x, y), size, drawColor);
                                break;
                            case KeyframeShape.Square:
                                ctx.DrawList.AddRectFilled(new Vector2(x - size, y - size), new Vector2(x + size, y + size), drawColor);
                                break;
                            default:
                                var p = new Vector2[] { new(x, y - size), new(x + size, y), new(x, y + size), new(x - size, y) };
                                ctx.DrawList.AddConvexPolyFilled(ref p[0], p.Length, drawColor);
                                break;
                        }
                    }
                }
            }
        }
        finally
        {
            ctx.DrawList.PopClipRect();
        }

        return clickedOnKeyframe;
    }

    private static uint LerpColorToWhite(uint color, float amount)
    {
        var c = ImGui.ColorConvertU32ToFloat4(color);
        c.X += (1.0f - c.X) * amount;
        c.Y += (1.0f - c.Y) * amount;
        c.Z += (1.0f - c.Z) * amount;
        return ImGui.ColorConvertFloat4ToU32(c);
    }

    private void HandleSelectionAndInput(RenderContext ctx, ImSequencerState state, List<TimelineTrack> tracks, List<TimelineTrack> visibleTracks, ref int selectedEntry, bool clickedOnKeyframe, ref bool requestContextMenu)
    {
        var contentRect = new ImRect(ctx.ContentMin, ctx.ContentMax);
        var clickedOnContent = ImGui.IsMouseClicked(0) && contentRect.Contains(ctx.IO.MousePos) && ImGui.IsWindowFocused();
        var clickedOnTrackArea = clickedOnContent && ctx.IO.MousePos.X > ctx.ContentMin.X + ctx.LeftOffset;

        if(state.IsBoxSelecting)
        {
            state.BoxSelectionEnd = ctx.IO.MousePos;
            ctx.DrawList.AddRectFilled(state.BoxSelectionStart, state.BoxSelectionEnd, ImGui.GetColorU32(new Vector4(1, 1, 1, ColorSelectionAlpha)));

            if(!ctx.IO.MouseDown[0])
            {
                state.IsBoxSelecting = false;
                FinalizeBoxSelection(ctx, state, tracks, visibleTracks);
            }
        }
        else if(clickedOnTrackArea && !state.MovingCurrentFrame && !clickedOnKeyframe && !state.IsDragging)
        {
            state.BoxSelectionStart = ctx.IO.MousePos;
            state.IsBoxSelecting = true;
            if(!ImGui.GetIO().KeyCtrl)
                state.SelectedKeyframes.Clear();

            var clickedRow = (int)((ctx.IO.MousePos.Y - ctx.ContentMin.Y) / ctx.ItemHeight);
            if(clickedRow >= 0 && clickedRow < visibleTracks.Count)
                selectedEntry = tracks.IndexOf(visibleTracks[clickedRow]);
        }

        if(ImGui.IsMouseClicked((ImGuiMouseButton)1) && contentRect.Contains(ctx.IO.MousePos) && !clickedOnKeyframe)
        {
            var hoveredRow = (int)((ctx.IO.MousePos.Y - ctx.ContentMin.Y) / ctx.ItemHeight);
            state.ContextTrackIndex = hoveredRow >= 0 && hoveredRow < visibleTracks.Count ? tracks.IndexOf(visibleTracks[hoveredRow]) : -1;
            state.ContextMouseFrame = (int)Math.Round((ctx.IO.MousePos.X - (ctx.ContentMin.X + ctx.LeftOffset)) / state.FramePixelWidth + state.ZoomState.ViewMin);
            state.ContextKeyframe = null;
            requestContextMenu = true;
        }
    }

    private void FinalizeBoxSelection(RenderContext ctx, ImSequencerState state, List<TimelineTrack> tracks, List<TimelineTrack> visibleTracks)
    {
        var minX = Math.Min(state.BoxSelectionStart.X, state.BoxSelectionEnd.X);
        var maxX = Math.Max(state.BoxSelectionStart.X, state.BoxSelectionEnd.X);
        var minY = Math.Min(state.BoxSelectionStart.Y, state.BoxSelectionEnd.Y);
        var maxY = Math.Max(state.BoxSelectionStart.Y, state.BoxSelectionEnd.Y);
        var selectionRect = new ImRect(new Vector2(minX, minY), new Vector2(maxX, maxY));

        for(var i = 0; i < visibleTracks.Count; i++)
        {
            var track = visibleTracks[i];
            var absoluteIndex = tracks.IndexOf(track);
            var y = ctx.ContentMin.Y + ctx.ItemHeight * i + (ctx.ItemHeight / 2f);

            foreach(var kf in track.Keyframes.ToList())
            {
                var x = (float)(ctx.ContentMin.X + ctx.LeftOffset + (kf.Frame - state.ZoomState.ViewMin) * state.FramePixelWidth);
                var kfSize = KeyframeHalfSize * ctx.Scale;
                var kfRect = new ImRect(new Vector2(x - kfSize, y - kfSize), new Vector2(x + kfSize, y + kfSize));

                if(selectionRect.Overlaps(kfRect))
                    state.SelectedKeyframes.Add(new SelectedKeyframe(absoluteIndex, kf.Id));
            }
        }
    }

    private void HandlePlayhead(RenderContext ctx, ImSequencerState state, int frameMin, int frameMax, ref int currentFrame, int firstFrameUsed)
    {
        var topRect = new ImRect(
            new Vector2(ctx.CanvasPos.X + ctx.LeftOffset, ctx.CanvasPos.Y),
            new Vector2(ctx.CanvasPos.X + ctx.CanvasSize.X, ctx.CanvasPos.Y + ctx.ItemHeight));

        if(!state.IsDragging && !state.IsBoxSelecting && topRect.Contains(ctx.IO.MousePos) && ctx.IO.MouseDown[0])
            state.MovingCurrentFrame = true;

        if(state.MovingCurrentFrame)
        {
            state.SelectedKeyframes.Clear();

            var safeMin = Math.Min(frameMin, frameMax);
            var safeMax = Math.Max(frameMin, frameMax);

            var calculatedFrame = state.FramePixelWidth > 0
                ? (int)((ctx.IO.MousePos.X - topRect.Min.X) / state.FramePixelWidth) + firstFrameUsed
                : safeMin;

            currentFrame = Math.Clamp(calculatedFrame, safeMin, safeMax);

            if(!ctx.IO.MouseDown[0])
                state.MovingCurrentFrame = false;
        }

        var cursorOffset = (float)(ctx.ContentMin.X + ctx.LeftOffset + (currentFrame - state.ZoomState.ViewMin) * state.FramePixelWidth);
        if(cursorOffset >= ctx.ContentMin.X + ctx.LeftOffset && cursorOffset <= ctx.ContentMax.X)
        {
            ctx.DrawList.AddLine(new Vector2(cursorOffset, ctx.ContentMin.Y), new Vector2(cursorOffset, ctx.ContentMax.Y), ImGui.GetColorU32(ColorPlayhead), ctx.Scale);

            var playheadColor = ImGui.GetColorU32(ColorPlayhead);
            var textColor = ImGui.GetColorU32(ColorPlayheadText);
            var headerBottom = ctx.CanvasPos.Y + ctx.ItemHeight;
            var rounding = 3f * ctx.Scale;
            var padding = 4f * ctx.Scale;
            var frameText = $"{currentFrame}";
            var textSize = ImGui.CalcTextSize(frameText);
            var triHeight = 5f * ctx.Scale;
            var boxHeight = textSize.Y + (padding / 2);
            var boxWidthHalf = (textSize.X / 2) + padding;
            var triBaseY = headerBottom - triHeight - ctx.Scale;
            var boxBottom = triBaseY;
            var boxTop = boxBottom - boxHeight;

            var boxMin = new Vector2(cursorOffset - boxWidthHalf, boxTop);
            var boxMax = new Vector2(cursorOffset + boxWidthHalf, boxBottom);

            ctx.ParentDrawList.AddRectFilled(boxMin, boxMax, playheadColor, rounding, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersTopRight);

            var triP1 = new Vector2(cursorOffset - boxWidthHalf, boxBottom);
            var triP2 = new Vector2(cursorOffset + boxWidthHalf, boxBottom);
            var triP3 = new Vector2(cursorOffset, headerBottom - ctx.Scale);
            ctx.ParentDrawList.AddTriangleFilled(triP1, triP2, triP3, playheadColor);

            ctx.ParentDrawList.AddText(new Vector2(cursorOffset - (textSize.X / 2), boxTop + (padding / 4)), textColor, frameText);
        }
    }

    private static bool ProcessDragging(RenderContext ctx, ImSequencerState state, List<TimelineTrack> tracks, int frameMin, int frameMax)
    {
        ImGui.SetNextFrameWantCaptureMouse(true);
        var diffX = (int)ctx.IO.MousePos.X - state.MovingPos;
        var diffFrame = (int)Math.Round(diffX / state.FramePixelWidth);
        var ret = false;

        if(Math.Abs(diffFrame) > 0)
        {
            var canMove = state.SelectedKeyframes.All(sk =>
            {
                var kf = GetTrack(tracks, sk.TrackIndex)?.Keyframes.FirstOrDefault(k => k.Id == sk.KeyframeId);
                return kf != null && kf.Frame + diffFrame >= frameMin && kf.Frame + diffFrame <= frameMax;
            });

            if(canMove)
            {
                foreach(var sk in state.SelectedKeyframes)
                {
                    var kf = GetTrack(tracks, sk.TrackIndex)?.Keyframes.FirstOrDefault(k => k.Id == sk.KeyframeId);
                    if(kf != null)
                        kf.Frame += diffFrame;
                }

                state.MovingPos += (int)Math.Round(diffFrame * state.FramePixelWidth);
                ret = true;
            }
        }

        if(!ctx.IO.MouseDown[0])
        {
            var selectedIds = state.SelectedKeyframes.Select(sk => sk.KeyframeId).ToHashSet();

            foreach(var trackIndex in state.SelectedKeyframes.Select(x => x.TrackIndex).Distinct())
            {
                var track = GetTrack(tracks, trackIndex);
                if(track is null)
                    continue;

                var frameGroups = track.Keyframes.GroupBy(k => k.Frame);
                var toDelete = new HashSet<Guid>();

                foreach(var group in frameGroups)
                {
                    if(group.Count() > 1)
                    {
                        var selectedInGroup = group.Where(k => selectedIds.Contains(k.Id)).ToList();
                        var unselectedInGroup = group.Where(k => !selectedIds.Contains(k.Id)).ToList();

                        if(selectedInGroup.Count == 1 && unselectedInGroup.Count != 0)
                            foreach(var kf in unselectedInGroup)
                                toDelete.Add(kf.Id);
                    }
                }

                track.Keyframes.RemoveAll(k => toDelete.Contains(k.Id));
                track.Keyframes = [.. track.Keyframes.OrderBy(k => k.Frame)];
            }

            state.IsDragging = false;
        }

        return ret;
    }

    private static void DrawScrollbar(ImSequencerState state, float viewWidthPixels, float leftOffset, float height, float scale)
    {
        var scrollbarCursorPos = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(scrollbarCursorPos.X + leftOffset);
        ImGui.SetItemAllowOverlap();

        if(ZoomScrollbar.Draw("sequencer_zoom", ref state.ZoomState, height, scale))
        {
            var viewSpan = Math.Max(state.ZoomState.ViewMax - state.ZoomState.ViewMin, 1);
            state.FramePixelWidth = (float)(viewWidthPixels / viewSpan);
        }

        ImGui.SetCursorPos(new Vector2(scrollbarCursorPos.X, scrollbarCursorPos.Y + height));
    }
}
