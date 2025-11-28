using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Entities;
using Brio.Files;
using Brio.Game.Actor.Extensions;
using Brio.Game.Cutscene;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Resources;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using static Brio.Game.Actor.ActionTimelineService;
using ImSequencer;
using ImSequencer.ImCurveEdit;
using ImSequencer.Memory;

namespace Brio.UI.Controls.Editors;


public unsafe class RampEdit : ImSequencer.ImCurveEdit.CurveContext
{
    
    public RampEdit()
    {
        var temp = new Random();
        
        mPts =  new Vector2[3][];
        
        for (var i = 0; i < 3; i++)
        {
            mPts[i] = new Vector2[3];
            for (var j = 0; j < 3; j++)
            {
                
                mPts[i][j] = new Vector2(temp.NextSingle() % 1, temp.NextSingle() % 1);
            }
            SortEntries(i);
        }

    
    }

    private Vector2[][] mPts;
    
    public override int GetCurveCount()
    {
        return mPts.Length;
    }

    public override int GetPointCount(int curveIndex)
    {
        return mPts[curveIndex].Length;
    }

    public override uint GetCurveColor(int curveIndex)
    {
        switch (curveIndex)
        {
            case 0:
                return 0xFF0000FF;
            case 1:
                return 0xFF00AA00;
            case 2:
                return 0xFFFF0000;
            default:
                return 0xFF0AAA00;
        }
        
    }

    public override Vector2[] GetPoints(int curveIndex)
    {
        return mPts[curveIndex];
    }

    public override int EditPoint(int curveIndex, int pointIndex, Vector2 value)
    {
        mPts[curveIndex][pointIndex] = NormalizeVector(value);
        SortEntries(curveIndex);
        return 1;
    }

    public override void AddPoint(int curveIndex, Vector2 value)
    {
        Vector2[] tempPts = new Vector2[mPts[curveIndex].Length + 1];
        mPts[curveIndex].CopyTo(tempPts, 0);
        tempPts[^1] = value;
        mPts[curveIndex] = tempPts;
        
    }
    
    private void SortEntries(int curveIndex)
    {
        mPts[curveIndex] = [..mPts[curveIndex].OrderBy(x =>x.X)];
    }

    private Vector2 NormalizeVector(Vector2 vec)
    {
        float x = float.Max(float.Min(vec.X, 1), 0);
        float y = float.Max(float.Min(vec.Y, 1), 0);
        return new Vector2(x, y);
    }
}


public unsafe class Sequencer : Window, SequenceInterface
{
    
    public struct Item
    {
        public uint color;
        public int start;
        public int end;
        public ItemType type; //switch this to enum later
        public bool expanded;
        public unsafe object* reference;

        public unsafe Item(ItemType _type, int _start, int _end, bool _expanded, object* _reference = null)
        {
            start = _start;
            end = _end;
            type = _type;
            expanded = _expanded;
            reference = _reference;
        }
        //public Item(int _type, int _start, int _end, bool _expanded) => Item((ItemType)_type, _start, _end, expanded);
    }
    public enum ItemType
    {
        Camera,
        Light,
        Bone
    }

    private int frameMin = 0;
    private int frameMax = 100;
    public UnsafeList<Item> items = [];
    
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public Sequencer(IDalamudPluginInterface pluginInterfacePlugin )
        : base("Timeline##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        items.Add(new Item
        {
            color = 0xFF0000,
            end = 20,
            start = 0,
            type = 0,
            expanded = false
        });
        rampEdit = new Dictionary<int, RampEdit>();
        for (var i = 0; i < 4; i++)
        {
            var temp = new RampEdit();
            rampEdit.Add(i, temp);
        }
    }

    public void Dispose() { }
    private int _index;
    private bool _expanded = true;
    private int _currFrame;
    private int _firstFrame;
    public Dictionary<int, RampEdit> rampEdit;

    public override void Draw()
    {
        var ptr = ImGui.GetWindowDrawList();
        ImRect CustomRect = new ImRect(new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight()));
        ImRect LegendRect = new ImRect(new Vector2(ImGui.GetWindowWidth() * .3f, ImGui.GetWindowHeight()));
        ImRect LegendClip = new ImRect(new Vector2(ImGui.GetWindowWidth() * .3f, ImGui.GetWindowHeight()));
        ImRect CustomClip = new ImRect(new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight()));

        ImGui.PushItemWidth(75f);
        ImGui.InputInt("Frame Min", ref frameMin);
        ImGui.SameLine();
        ImGui.InputInt("Frame Max", ref frameMax);
        ImGui.Separator();
        
        ImGui.PopItemWidth();


        ImSequencer.ImSequencer.Sequencer(this, ref _currFrame , ref _expanded, ref _index, ref _firstFrame, options);
    }
    public bool focused { get; set; }
    public int GetFrameMin() => frameMin;
    public SEQUENCER_OPTIONS options = SEQUENCER_OPTIONS.SEQUENCER_ADD | SEQUENCER_OPTIONS.SEQUENCER_CHANGE_FRAME |
                                       SEQUENCER_OPTIONS.SEQUENCER_COPYPASTE | SEQUENCER_OPTIONS.SEQUENCER_EDIT_ALL;


    public int GetFrameMax() => frameMax;

    public int GetItemCount() => items.Size;

    public void BeginEdit(int index)
    {
    }

    public void EndEdit()
    {
    }

    public int GetItemTypeCount() => Enum.GetNames(typeof(ItemType)).Length;

    public string GetItemTypeName(int index) => items[index].type.ToString();

    public string GetItemLabel(int index)
    {
        return $"[{index.ToString()}] {Enum.GetName(items[index].type)}";
    }

    public string GetCollapseFmt(int frameCount, int sequenceCount)
    {
        return $"{frameCount.ToString()} frames / {sequenceCount.ToString()} entries";
    }

    public  void Get(int index, int** start, int** end, int* type, uint* color)
    {
        if (start != null) *start = &items.Data[index].start;
        if (end != null) *end = &items.Data[index].end;
        if (type != null) *type = (int)items[index].type;
        if (color != null) *color = 0xFFAA8080;
    }

    public void Add(int index)
    {
        items.Add(new Item()
        {
            color = 0000000,
            end = 60,
            expanded = true,
            start = 40,
            type = 0
        });
    }

    public void Del(int index)
    {
    }

    public void Duplicate(int index)
    {
    }

    public void Copy()
    {
    }

    public void Paste()
    {
    }

    public uint GetCustomHeight(int index)
    {
        return (uint)(items[index].expanded ? 300 : 0);
    }

    public void DoubleClick(int index)
    {

        var item = items.GetPointer(index);
        if (item->expanded)
        {
            item->expanded = false;
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var at = items.GetPointer(i);
            at->expanded = false;
        }
        item->expanded = true;
            
    }

    public unsafe void CustomDraw(
        int index, ImDrawListPtr drawList, ImRect customRect, ImRect legendRect, ImRect clippingRect,
        ImRect legendClippingRect)
    {
        if (!rampEdit.TryGetValue(index, out var ramp))
        {
            return;
        };
        var labels = new[] { "Translation", "Rotation" , "Scale"};
        ramp.Max = new Vector2((float)items[index].end/frameMax, 1f);
        ramp.Min = new Vector2((float)(items[index].start-frameMin)/frameMax, 0f);
        drawList.PushClipRect(clippingRect.Min, clippingRect.Max, true);
        for (int i = 0; i < 3; i++)
        {
            Vector2 pta = new Vector2(legendRect.Min.X + 30, legendRect.Min.Y + i * 14f);
            Vector2 ptb = new Vector2(legendRect.Max.X, legendRect.Min.Y + (i + 1) * 14f);
            drawList.AddText(pta, 0xFFFFFFFF, labels[i]);
            var imRect = new ImRect(pta, ptb);
            if (imRect.Contains(ImGui.GetMousePos()) && ImGui.IsMouseClicked(0))
            {
                //rampEdit
            }

        }
        ImGui.SetCursorScreenPos(customRect.Min);
        //drawList.AddRect(customRect.Min, customRect.Max, 0xFFAABBCC);
        ramp.Range = new Vector2(customRect.Min.X, customRect.Max.X);
        ImCurveEdit.Edit(ramp, customRect.Max-customRect.Min,(uint)(137+ index) );
        drawList.PopClipRect();
        drawList.PushClipRect(clippingRect.Min, clippingRect.Max, true);
        rampEdit[index] = ramp;
        

        drawList.PopClipRect();
        
    }

    public void CustomDrawCompact(int index, ImDrawListPtr drawList, ImRect customRect,  ImRect clippingRect)
    {

        if (!rampEdit.TryGetValue(index, out var ramp))
        {
            return;
        };
        drawList.PushClipRect(clippingRect.Min, clippingRect.Max, true);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < ramp.GetPointCount(i); j++)
            {
                float p = ramp.GetPoints(i)[j].X;
                if (p < items[index].start || p > items[index].end)
                    continue;
                float r = (p*(items[index].end - items[index].start) + frameMin) / (float)(frameMax - frameMin);
                float x = Extensions.ImLerp(customRect.Min.X, customRect.Max.X, r);
                drawList.AddLine(new Vector2(x, customRect.Min.Y + 6), new Vector2(x, customRect.Max.Y - 4), 0xAA000000,
                    4.0f);

            }
        }
        rampEdit[index] = ramp;
        drawList.PopClipRect();
    }

    private static bool IsOpen = false;
    
}
