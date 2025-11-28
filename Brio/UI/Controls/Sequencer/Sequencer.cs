using System;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using System.Numerics;


namespace ImSequencer
{

    public static class ImSequencer
    {
        private static SequencerCustomDraw _sequencerCustomDraw;

        private static bool SequencerAddDelButton(ImDrawListPtr draw_list, Vector2 pos, bool add = true)
            {
                
                ImGuiIOPtr io = ImGui.GetIO();
                ImRect btnRect = new ImRect(pos, new Vector2(pos.X + 16, pos.Y + 16));
                bool overBtn = btnRect.Contains(io.MousePos);
                bool containedClick = overBtn && btnRect.Contains(io.MouseClickedPos[0]);
                bool clickedBtn = containedClick && io.MouseReleased[0];
                uint btnColor = overBtn ? 0xAAEAFFAA : 0x77A3B2AA;
                if (containedClick && io.MouseDownDuration[0] > 0)
                    btnRect.Expand(2.0f);

                float midy = pos.Y + 16 / 2 - 0.5f;
                float midx = pos.X + 16 / 2 - 0.5f;
                draw_list.AddRect(btnRect.Min, btnRect.Max,(uint) btnColor, 4);
                draw_list.AddLine(new Vector2(btnRect.Min.X + 3, midy), new Vector2(btnRect.Max.X - 3, midy), (uint)btnColor, 2f);
                if (add)
                    draw_list.AddLine(new Vector2(midx, btnRect.Min.Y + 3), new Vector2(midx, btnRect.Max.Y - 3), (uint) btnColor, 2f);
                return clickedBtn;
            }

            private static SeqContext.SequencerContext ctx = new();
            public static unsafe bool Sequencer(SequenceInterface sequence, ref int currentFrame, ref bool expanded, ref int selectedEntry, ref int firstFrame, SEQUENCER_OPTIONS sequenceOptions)
            {
                bool ret = false;
                ImGuiIOPtr io = ImGui.GetIO();
                int cx = (int)(io.MousePos.X);
                int cy = (int)(io.MousePos.Y);
                bool popupOpened = false;
                int sequenceCount = sequence.GetItemCount();
                if (sequenceCount == 0)
                    return false;
                ImGui.BeginGroup();
                ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
                Vector2 canvas_pos = ImGui.GetCursorScreenPos();
                Vector2 canvas_size = ImGui.GetContentRegionAvail();
                int firstFrameUsed = firstFrame;

                int controlHeight = sequenceCount * ctx.ItemHeight;
                for (int i = 0; i < sequenceCount; i++)
                    controlHeight += (int)sequence.GetCustomHeight(i);
                int frameCount = Math.Max(sequence.GetFrameMax() - sequence.GetFrameMin(), 1);
                var viewWidthPixels = canvas_size.X - ctx.legendWidth;


                
                var customDraws = new List<SequencerCustomDraw>();
                var compactCustomDraws = new List<SequencerCustomDraw>();
                // zoom in/out
                int visibleFrameCount = (int)Math.Floor((canvas_size.X - ctx.legendWidth) / ctx.framePixelWidth);
                float barWidthRatio = Math.Min((float)visibleFrameCount / (float)frameCount, 1f);
                float barWidthInPixels = barWidthRatio * (canvas_size.X - ctx.legendWidth);

                ImRect regionRect = new ImRect(canvas_pos, Extensions.Add(canvas_pos, canvas_size));


                ctx.PanningViewSource = new Vector2();
                ctx.PanningViewFrame = 0;
                if (ImGui.IsWindowFocused() && io.KeyAlt && io.MouseDown[2])
                {
                    if (!ctx.PanningView)
                    {
                        ctx.PanningViewSource = io.MousePos;
                        ctx.PanningView = true;
                        ctx.PanningViewFrame = firstFrame;
                    }
                    firstFrame = ctx.PanningViewFrame - (int)((io.MousePos.X - ctx.PanningViewSource.X) / ctx.framePixelWidth);
                    firstFrame = Math.Clamp(firstFrame, sequence.GetFrameMin(), sequence.GetFrameMax() - visibleFrameCount);
                }
                if (ctx.PanningView && !io.MouseDown[2])
                {
                    ctx.PanningView = false;
                }
                ctx.ScrollbarState.ContentMin = sequence.GetFrameMin();
                ctx.ScrollbarState.ContentMax = sequence.GetFrameMax();

                if (ctx.ScrollbarState.MinViewSpan <= 0)
                    ctx.ScrollbarState.MinViewSpan = 10;

                if (ctx.ScrollbarState.ViewMax <= ctx.ScrollbarState.ViewMin || ctx.ScrollbarState.ViewMax > ctx.ScrollbarState.ContentMax || double.IsNaN(ctx.ScrollbarState.ViewMax))
                {
                    ctx.ScrollbarState.ViewMin = sequence.GetFrameMin();
                    double initialSpan = viewWidthPixels / ctx.framePixelWidth;
                    if (initialSpan <= 0 || double.IsNaN(initialSpan) || double.IsInfinity(initialSpan))
                        initialSpan = 100;
                    ctx.ScrollbarState.ViewMax = ctx.ScrollbarState.ViewMin + initialSpan;
                }

                ctx.ScrollbarState.ViewMin = Math.Clamp(ctx.ScrollbarState.ViewMin, ctx.ScrollbarState.ContentMin, ctx.ScrollbarState.ContentMax - ctx.ScrollbarState.MinViewSpan);
                ctx.ScrollbarState.ViewMax = Math.Clamp(ctx.ScrollbarState.ViewMax, ctx.ScrollbarState.ViewMin + ctx.ScrollbarState.MinViewSpan, ctx.ScrollbarState.ContentMax);

                firstFrame = (int)Math.Round(ctx.ScrollbarState.ViewMin);
                double viewSpan = ctx.ScrollbarState.ViewMax - ctx.ScrollbarState.ViewMin;
                if (viewSpan <= 0) viewSpan = 1;
                ctx.framePixelWidthTarget = Math.Clamp(ctx.framePixelWidthTarget, 0.1f, 50f);

                ctx.framePixelWidth = Extensions.ImLerp(ctx.framePixelWidth, ctx.framePixelWidthTarget, 0.33f);

                frameCount = sequence.GetFrameMax() - sequence.GetFrameMin();
                if (visibleFrameCount >= frameCount)
                    firstFrame = sequence.GetFrameMin();

                // --
                if (!expanded)
                {
                    if(ImGui.InvisibleButton("canvas", new Vector2(canvas_size.X - canvas_pos.X, (float)ctx.ItemHeight)))
                    {
                        expanded = true;
                    };
                    draw_list.AddRectFilled(canvas_pos, new Vector2(canvas_size.X + canvas_pos.X, canvas_pos.Y + ctx.ItemHeight), 0xFF3D3837, ImDrawFlags.None);
                    string tmps = string.Format(sequence.GetCollapseFmt(frameCount, sequenceCount));
                    draw_list.AddText(new Vector2(canvas_pos.X + 26, canvas_pos.Y + 2), ctx.Style.Text, tmps);
                }
                else
                {
                    bool hasScrollBar = true;
                    Vector2 headerSize = new Vector2(canvas_size.X, (float)ctx.ItemHeight);
                    Vector2 scrollBarSize = new Vector2(canvas_size.X, 14f);
                    ImGui.InvisibleButton("topBar", headerSize);
                    draw_list.AddRectFilled(canvas_pos, Extensions.Add(canvas_pos, headerSize), 0xFFFF0000, ImDrawFlags.None);
                    Vector2 childFramePos = ImGui.GetCursorScreenPos();
                    Vector2 childFrameSize = new Vector2(canvas_size.X, canvas_size.Y - 8f - headerSize.Y - (hasScrollBar ? scrollBarSize.Y : 0));
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
                    ImGui.BeginChildFrame(889, childFrameSize);
                    sequence.focused = ImGui.IsWindowFocused();
                    ImGui.InvisibleButton("contentBar", new Vector2(canvas_size.X, (float)controlHeight));
                    Vector2 contentMin = ImGui.GetItemRectMin();
                    Vector2 contentMax = ImGui.GetItemRectMax();
                    ImRect contentRect = new ImRect(contentMin, contentMax);
                    float contentHeight = contentMax.Y - contentMin.Y;

                    // full background
                    draw_list.AddRectFilled(canvas_pos, Extensions.Add(canvas_pos, canvas_size), ctx.Style.FrameBg, ImDrawFlags.None);

                    // current frame top
                    ImRect topRect = new ImRect(new Vector2(canvas_pos.X + ctx.legendWidth, canvas_pos.Y), new Vector2(canvas_pos.X + canvas_size.X, canvas_pos.Y + ctx.ItemHeight));

                    if (!ctx.MovingCurrentFrame && !ctx.MovingScrollBar && ctx.movingEntry == -1 && (sequenceOptions & SEQUENCER_OPTIONS.SEQUENCER_CHANGE_FRAME) != 0 && currentFrame >= 0 && topRect.Contains(io.MousePos) && io.MouseDown[0])
                    {
                        ctx.MovingCurrentFrame = true;
                    }
                    if (ctx.MovingCurrentFrame)
                    {
                        if (frameCount != 0)
                        {
                            currentFrame = (int)((io.MousePos.X - topRect.Min.X) / ctx.framePixelWidth) + firstFrameUsed;
                            if (currentFrame < sequence.GetFrameMin())
                                currentFrame = sequence.GetFrameMin();
                            if (currentFrame >= sequence.GetFrameMax())
                                currentFrame = sequence.GetFrameMax();
                        }
                        if (!io.MouseDown[0])
                            ctx.MovingCurrentFrame = false;
                    }

                    //header
                    draw_list.AddRectFilled(canvas_pos, new Vector2(canvas_size.X + canvas_pos.X, canvas_pos.Y + ctx.ItemHeight), ctx.Style.MenuBarBg - 0xAAAAAAAA, ImDrawFlags.None);
                    if ((sequenceOptions & SEQUENCER_OPTIONS.SEQUENCER_ADD) != 0)
                    {
                        if (SequencerAddDelButton(draw_list, new Vector2(canvas_pos.X + ctx.legendWidth - ctx.ItemHeight, canvas_pos.Y + 2), true))
                            ImGui.OpenPopup("addEntry");

                        if (ImGui.BeginPopup("addEntry"))
                        {
                            for (int i = 0; i < sequence.GetItemTypeCount(); i++)
                                if (ImGui.Selectable(sequence.GetItemTypeName(i)))
                                {
                                    sequence.Add(i);
                                    selectedEntry = sequence.GetItemCount() - 1;
                                }

                            ImGui.EndPopup();
                            popupOpened = true;
                        }
                    }

                    //header frame number and lines
                    int modFrameCount = 10;
                    int frameStep = 1;
                    while ((modFrameCount * ctx.framePixelWidth) < 150)
                    {
                        modFrameCount *= 2;
                        frameStep *= 2;
                    }
                    int halfModFrameCount = modFrameCount / 2;

                    void DrawLine(int i, int regionHeight)
                    {
                        bool baseIndex = ((i % modFrameCount) == 0) || (i == sequence.GetFrameMax() || i == sequence.GetFrameMin());
                        bool halfIndex = (i % halfModFrameCount) == 0;
                        int px = (int)canvas_pos.X + (int)(i * ctx.framePixelWidth) + ctx.legendWidth - (int)(firstFrameUsed * ctx.framePixelWidth);
                        int tiretStart = baseIndex ? 4 : (halfIndex ? 10 : 14);
                        int tiretEnd = baseIndex ? regionHeight : ctx.ItemHeight;

                        if (px <= (canvas_size.X + canvas_pos.X) && px >= (canvas_pos.X + ctx.legendWidth))
                        {
                            draw_list.AddLine(new Vector2(px, canvas_pos.Y + tiretStart), new Vector2(px, canvas_pos.Y + tiretEnd - 1), 0xFF606060, 1);
                            draw_list.AddLine(new Vector2(px, canvas_pos.Y + ctx.ItemHeight), new Vector2(px, canvas_pos.Y + regionHeight - 1), 0x30606060, 1);
                        }

                        if (baseIndex && px > (canvas_pos.X + ctx.legendWidth))
                        {
                            string tmps = string.Format("{0}", i);
                            draw_list.AddText(new Vector2(px + 3f, canvas_pos.Y), ctx.Style.Text, tmps);
                        }
                    }

                    void DrawLineContent(int i, int regionHeight)
                    {
                        int px = (int)canvas_pos.X + (int)(i * ctx.framePixelWidth) + ctx.legendWidth - (int)(firstFrameUsed * ctx.framePixelWidth);
                        int tiretStart = (int)contentMin.Y;
                        int tiretEnd = (int)contentMax.Y;

                        if (px <= (canvas_size.X + canvas_pos.X) && px >= (canvas_pos.X + ctx.legendWidth))
                        {
                            draw_list.AddLine(new Vector2(px, tiretStart), new Vector2(px, tiretEnd), 0x30606060, 1);
                        }
                    }

                    for (int i = sequence.GetFrameMin(); i <= sequence.GetFrameMax(); i += frameStep)
                    {
                        DrawLine(i, ctx.ItemHeight);
                    }
                    DrawLine(sequence.GetFrameMin(), ctx.ItemHeight);
                    DrawLine(sequence.GetFrameMax(), ctx.ItemHeight);

                    //ImGui.PushClipRect(childFramePos, Extensions.Add(childFramePos, childFrameSize), true);
                    draw_list.PushClipRect(childFramePos, Extensions.Add(childFramePos, childFrameSize), true);

                    // draw item names in the legend rect on the left
                    int customHeight = 0;
                    for (int i = 0; i < sequenceCount; i++)
                    {
                        int* start, end;
                        uint color;
                        sequence.Get(i, &start, &end, null, &color);
                        Vector2 tpos = new Vector2(contentMin.X + 3, contentMin.Y + i * ctx.ItemHeight + 2 + customHeight);
                        draw_list.AddText(tpos, 0xFFFFFFFF, sequence.GetItemLabel(i));

                        if ((sequenceOptions & SEQUENCER_OPTIONS.SEQUENCER_DEL) != 0)
                        {
                            if (SequencerAddDelButton(draw_list, new Vector2(contentMin.X + ctx.legendWidth - ctx.ItemHeight + 2 - 10, tpos.Y + 2), false))
                                ctx.delEntry = i;

                            if (SequencerAddDelButton(draw_list, new Vector2(contentMin.X + ctx.legendWidth - ctx.ItemHeight - ctx.ItemHeight + 2 - 10, tpos.Y + 2), true))
                                ctx.dupEntry = i;
                        }
                        customHeight += (int)sequence.GetCustomHeight(i);
                    }

                    // slots background
                    customHeight = 0;
                    for (int i = 0; i < sequenceCount; i++)
                    {
                        uint col = (i & 1) != 0 ? 0xFF3A3636u : 0xFF413D3Du;

                        int localCustomHeight = (int)sequence.GetCustomHeight(i);
                        Vector2 pos = new Vector2(contentMin.X + ctx.legendWidth, contentMin.Y + ctx.ItemHeight * i + 1 + customHeight);
                        Vector2 sz = new Vector2(canvas_size.X + canvas_pos.X, pos.Y + ctx.ItemHeight - 1 + localCustomHeight);
                        if (!popupOpened && cy >= pos.Y && cy < pos.Y + (ctx.ItemHeight + localCustomHeight) && ctx.movingEntry == -1 && cx > contentMin.X && cx < contentMin.X + canvas_size.X)
                        {
                            col += 0x80201008u;
                            pos.X -= ctx.legendWidth;
                        }
                        draw_list.AddRectFilled(pos, sz, col, ImDrawFlags.None);
                        customHeight += localCustomHeight;
                    }
                    draw_list.PushClipRect(Extensions.Add(childFramePos, new Vector2(ctx.legendWidth, 0f)), Extensions.Add(childFramePos, childFrameSize), false);
                    //ImGui.PushClipRect(Extensions.Add(childFramePos, new Vector2(ctx.legendWidth, 0f)), Extensions.Add(childFramePos, childFrameSize), false);
                    
                    // vertical frame lines in content area
                    for (int i = sequence.GetFrameMin(); i <= sequence.GetFrameMax(); i += frameStep)
                    {
                        DrawLineContent(i, (int)contentHeight);
                    }
                    DrawLineContent(sequence.GetFrameMin(), (int)contentHeight);
                    DrawLineContent(sequence.GetFrameMax(), (int)contentHeight);

                    // selection
                    bool selected = selectedEntry >= 0;
                    if (selected)
                    {
                        customHeight = 0;
                        for (int i = 0; i < selectedEntry; i++)
                            customHeight += (int)sequence.GetCustomHeight(i);
                        draw_list.AddRectFilled(new Vector2(contentMin.X, contentMin.Y + ctx.ItemHeight * selectedEntry + customHeight), new Vector2(contentMin.X + canvas_size.X, contentMin.Y + ctx.ItemHeight * (selectedEntry + 1) + customHeight), 0x801080FF, 1f);
                    }

                    // slots
                    customHeight = 0;
                    for (int i = 0; i < sequenceCount; i++)
                    {
                        int* start, end;
                        uint color;
                        sequence.Get(i, &start, &end, null, &color);
                        int localCustomHeight = (int)sequence.GetCustomHeight(i);

                        Vector2 pos = new Vector2(contentMin.X + ctx.legendWidth - firstFrameUsed * ctx.framePixelWidth, contentMin.Y + ctx.ItemHeight * i + 1 + customHeight);
                        Vector2 slotP1 = new Vector2(pos.X + *start * ctx.framePixelWidth, pos.Y + 2);
                        Vector2 slotP2 = new Vector2(pos.X + *end * ctx.framePixelWidth + ctx.framePixelWidth, pos.Y + ctx.ItemHeight - 2);
                        Vector2 slotP3 = new Vector2(pos.X + *end * ctx.framePixelWidth + ctx.framePixelWidth, pos.Y + ctx.ItemHeight - 2 + localCustomHeight);
                        uint slotColor = color | 0xFF000000u;
                        uint slotColorHalf = (color & 0xFFFFFFu) | 0x40000000u;

                        if (slotP1.X <= (canvas_size.X + contentMin.X) && slotP2.X >= (contentMin.X + ctx.legendWidth))
                        {
                            draw_list.AddRectFilled(slotP1, slotP3, slotColorHalf, 2);
                            draw_list.AddRectFilled(slotP1, slotP2, slotColor, 2);
                        }

                        ImRect selectRect = new ImRect(slotP1, slotP2);
                        if (selectRect.Contains(io.MousePos) && io.MouseDoubleClicked[0])
                        {
                            sequence.DoubleClick(i);
                        }
                        // Ensure grabbable handles
                        float max_handle_width = (slotP2.X - slotP1.X) / 3.0f;
                        float min_handle_width = Math.Min(10.0f, max_handle_width);
                        float handle_width = Math.Clamp(ctx.framePixelWidth / 2.0f, min_handle_width, max_handle_width);
                        ImRect[] rects = {
                        new ImRect(slotP1, new Vector2(slotP1.X + handle_width, slotP2.Y)),
                        new ImRect(new Vector2(slotP2.X - handle_width, slotP1.Y), slotP2),
                        new ImRect(slotP1, slotP2)
                    };

                        uint[] quadColor = { 0xFFFFFFFFu, 0xFFFFFFFFu, slotColor + (selected ? 0u : 0x202020u) };
                        if (ctx.movingEntry == -1 && (sequenceOptions & SEQUENCER_OPTIONS.SEQUENCER_EDIT_STARTEND) != 0)
                        {
                            for (int j = 2; j >= 0; j--)
                            {
                                ImRect rc = rects[j];
                                if (!rc.Contains(io.MousePos))
                                    continue;
                                draw_list.AddRectFilled(rc.Min, rc.Max, quadColor[j], 2);
                            }

                            for (int j = 0; j < 3; j++)
                            {
                                ImRect rc = rects[j];
                                if (!rc.Contains(io.MousePos))
                                    continue;
                                ImRect subselectRect = new ImRect(childFramePos,
                                    Extensions.Add(childFramePos, childFrameSize));
                                if (!subselectRect.Contains(io.MousePos))
                                    continue;
                                if (ImGui.IsMouseClicked(0) && !ctx.MovingScrollBar && !ctx.MovingCurrentFrame)
                                {
                                    ctx.movingEntry = i;
                                    ctx.movingPos = cx;
                                    ctx.movingPart = j + 1;
                                    sequence.BeginEdit(ctx.movingEntry);
                                    break;
                                }
                            }
                        }

                        // custom draw
                        if (localCustomHeight > 0)
                        {
                            Vector2 rp = new Vector2(canvas_pos.X + *start, contentMin.Y + ctx.ItemHeight * i + 1 + customHeight);
                            ImRect customRect = new ImRect(Extensions.Add(rp, new Vector2( ctx.legendWidth - (firstFrameUsed - sequence.GetFrameMin() - 0.5f) * ctx.framePixelWidth, (float)ctx.ItemHeight)),
                                Extensions.Add(rp, new Vector2(ctx.legendWidth + (sequence.GetFrameMax() - firstFrameUsed - 0.5f + 2f) * ctx.framePixelWidth, (float)(localCustomHeight + ctx.ItemHeight))));
                            ImRect clippingRect = new ImRect(Extensions.Add(rp, new Vector2((float)ctx.legendWidth, (float)ctx.ItemHeight)), Extensions.Add(rp, new Vector2(canvas_size.X, (float)(localCustomHeight + ctx.ItemHeight))));

                            ImRect legendRect = new ImRect(Extensions.Add(rp, new Vector2(0f, (float)ctx.ItemHeight)), Extensions.Add(rp, new Vector2((float)ctx.legendWidth, (float)localCustomHeight)));
                            ImRect legendClippingRect = new ImRect(Extensions.Add(canvas_pos, new Vector2(0f, (float)ctx.ItemHeight)), Extensions.Add(canvas_pos, new Vector2((float)ctx.legendWidth, (float)(localCustomHeight + ctx.ItemHeight))));
                            customDraws.Add(new SequencerCustomDraw { Index = i, CustomRect = customRect, LegendRect = legendRect, ClippingRect = clippingRect, LegendClippingRect = legendClippingRect });
                        }
                        else
                        {
                            Vector2 rp = new Vector2(canvas_pos.X, contentMin.Y + ctx.ItemHeight * i + customHeight);
                            ImRect customRect = new ImRect(Extensions.Add(rp, new Vector2(ctx.legendWidth - (firstFrameUsed - sequence.GetFrameMin() - 0.5f) * ctx.framePixelWidth, 0f)),
                                Extensions.Add(rp, new Vector2(ctx.legendWidth + (sequence.GetFrameMax() - firstFrameUsed - 0.5f + 2f) * ctx.framePixelWidth, (float)ctx.ItemHeight)));
                            ImRect clippingRect = new ImRect(Extensions.Add(rp, new Vector2((float)ctx.legendWidth, 0f)), Extensions.Add(rp, new Vector2(canvas_size.X, (float)ctx.ItemHeight)));

                            compactCustomDraws.Add(new SequencerCustomDraw { Index = i, CustomRect = customRect, LegendRect = new ImRect(), ClippingRect = clippingRect, LegendClippingRect = new ImRect() });
                        }
                        customHeight += localCustomHeight;
                    }

                    // moving
                    if (ctx.movingEntry >= 0)
                    {
                        ImGui.SetNextFrameWantCaptureMouse(true);
                        int diffFrame = (int)((cx - ctx.movingPos) / ctx.framePixelWidth);
                        if (Math.Abs(diffFrame) > 0)
                        {
                            int* start, end;
                            uint color;
                            sequence.Get(ctx.movingEntry, &start, &end, null, &color);
                            selectedEntry = ctx.movingEntry;
                            ref int l = ref *start;
                            ref int r = ref *end;
                            if ((ctx.movingPart & 1) != 0)
                                l += diffFrame;
                            if ((ctx.movingPart & 2) != 0)
                                r += diffFrame;
                            if (l < 0)
                            {
                                if ((ctx.movingPart & 2) != 0)
                                    r -= l;
                                l = 0;
                            }
                            if ((ctx.movingPart & 1) != 0 && l > r)
                                l = r;
                            if ((ctx.movingPart & 2) != 0 && r < l)
                                r = l;
                            ctx.movingPos += (int)(diffFrame * ctx.framePixelWidth);
                        }
                        if (!io.MouseDown[0])
                        {
                            if (diffFrame == 0 && ctx.movingPart != 0)
                            {
                                selectedEntry = ctx.movingEntry;
                                ret = true;
                            }
                            ctx.movingEntry = -1;
                            sequence.EndEdit();
                        }
                    }
                    draw_list.PopClipRect();
                    draw_list.PopClipRect();
                    
                    // cursor 
                    
   
                    draw_list.PushClipRect(Extensions.Add(childFramePos, new Vector2(ctx.legendWidth, -30f)), Extensions.Add(childFramePos, childFrameSize), false);
                    if (currentFrame >= firstFrame && currentFrame <= sequence.GetFrameMax())
                    {
                        float cursorWidth = 2f;
                        float cursorOffset = contentMin.X + ctx.legendWidth + (currentFrame - firstFrameUsed) * ctx.framePixelWidth + ctx.framePixelWidth / 2 - cursorWidth * 0.5f;

                        draw_list.AddTriangle(new Vector2(cursorOffset+ .5f, canvas_pos.Y + 10), new Vector2(cursorOffset - 5 , canvas_pos.Y), new Vector2(cursorOffset + 5 , canvas_pos.Y),0xFF2A2AFF, 2f );
                        draw_list.AddLine(new Vector2(cursorOffset, canvas_pos.Y + 10), new Vector2(cursorOffset, contentMax.Y), 0xFF2A2AFF, cursorWidth);
                        
                        draw_list.AddText(new Vector2(cursorOffset + 10, canvas_pos.Y + 2), 0xFFFFFFFF, $"{currentFrame.ToString()}");
                    }
                    draw_list.PopClipRect();



                    draw_list.PushClipRect(Extensions.Add(childFramePos, new Vector2(ctx.legendWidth, 0f)), Extensions.Add(childFramePos, childFrameSize), false);
                    foreach (var customDraw in customDraws)
                        sequence.CustomDraw(customDraw.Index, draw_list, customDraw.CustomRect, customDraw.LegendRect, customDraw.ClippingRect, customDraw.LegendClippingRect);
                    foreach (var customDraw in compactCustomDraws)
                        sequence.CustomDrawCompact(customDraw.Index, draw_list, customDraw.CustomRect, customDraw.ClippingRect);
                    draw_list.PopClipRect();
                    // copy paste
                    /*if ((sequenceOptions & (int)SEQUENCER_OPTIONS.SEQUENCER_COPYPASTE) != 0)
                    {
                        ImRect rectCopy = new ImRect(new Vector2(contentMin.X + 100, canvas_pos.Y + 2), new Vector2(contentMin.X + 100 + 30, canvas_pos.Y + ctx.ItemHeight - 2));
                        bool inRectCopy = rectCopy.Contains(io.MousePos);
                        uint copyColor = inRectCopy ? 0xFF1080FFu : 0xFF000000u;
                        draw_list.AddText(rectCopy.Min, copyColor, "Copy");

                        ImRect rectPaste = new ImRect(new Vector2(contentMin.X + 140, canvas_pos.Y + 2), new Vector2(contentMin.X + 140 + 30, canvas_pos.Y + ctx.ItemHeight - 2));
                        bool inRectPaste = rectPaste.Contains(io.MousePos);
                        uint pasteColor = inRectPaste ? 0xFF1080FFu : 0xFF000000u;
                        draw_list.AddText(rectPaste.Min, pasteColor, "Paste");

                        if (inRectCopy && io.MouseReleased[0])
                        {
                            sequence.Copy();
                        }
                        if (inRectPaste && io.MouseReleased[0])
                        {
                            sequence.Paste();
                        }
                    } */

                    ImGui.EndChildFrame();
                    ImGui.PopStyleColor();
                    
                    var scrollbarCursorPos = ImGui.GetCursorPos();
                    ImGui.SetCursorPosX(scrollbarCursorPos.X + ctx.legendWidth);
                    ImGui.SetItemAllowOverlap();
                    //Scrollbar logic
                    bool scrollbarChanged = ZoomScrollbar.Draw("sequencer_zoom", ref ctx.ScrollbarState, 14.0f);
                    if (scrollbarChanged)
                    {
                        firstFrame = (int)Math.Round(ctx.ScrollbarState.ViewMin);
                        viewSpan = ctx.ScrollbarState.ViewMax - ctx.ScrollbarState.ViewMin;
                        if (viewSpan <= 0) viewSpan = 1;
                        ctx.framePixelWidth = (float)(viewWidthPixels / viewSpan);
                        ctx.framePixelWidthTarget = ctx.framePixelWidth;
                    }
                    ImGui.SetCursorPos(scrollbarCursorPos + new Vector2(0, 14.0f));
                    

                    
                }

                ImGui.EndGroup();

                if (regionRect.Contains(io.MousePos))
                {
                    bool overCustomDraw = false;
                    foreach (SequencerCustomDraw custom in customDraws)
                    {
                        _sequencerCustomDraw = custom;
                        if (_sequencerCustomDraw.CustomRect.Contains(io.MousePos))
                        {
                            overCustomDraw = true;
                        }
                    }
                    if (overCustomDraw)
                    {
                    }
                    else
                    {
                        // Mouse wheel zoom logic comes later
                    }
                }

                if (expanded)
                {
                    bool overExpanded = SequencerAddDelButton(draw_list,
                        new Vector2(canvas_pos.X + 2, canvas_pos.Y + 2), !expanded);
                    if (overExpanded && io.MouseReleased[0])
                    {
                        expanded = !expanded;
                    }

                }

                if (ctx.delEntry != -1)
                {
                    sequence.Del(ctx.delEntry);
                    if (selectedEntry == ctx.delEntry || selectedEntry >= sequence.GetItemCount())
                        selectedEntry = -1;
                }

                if (ctx.dupEntry != -1)
                {
                    sequence.Duplicate(ctx.dupEntry);
                }
                return ret;
            }


        }


    }


