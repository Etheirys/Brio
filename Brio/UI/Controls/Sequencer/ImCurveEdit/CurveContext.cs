using System.Numerics;
using Dalamud.Bindings.ImGui;
using ImSequencer.Memory;

namespace ImSequencer.ImCurveEdit
{
    
    public abstract unsafe class CurveContext
    {
        public bool focused = false;
        public ImGuiIOPtr Io;
        public ImDrawList* DrawList;
        public Vector2 ScreenMin;
        public Vector2 ScreenMax;
        public Vector2 ScreenRange;
        public Vector2 Min;
        public Vector2 Max;
        public Vector2 Range;
        public Vector2 CanvasOrigin = new(0.5f);

        public Vector2 PointToCanvas(Vector2 pt)
        {
            return ((pt - Min) / Range) - CanvasOrigin;
        }

        public Vector2 CanvasToPoint(Vector2 pt)
        {
            return (pt) * Range + Min;
        }

        public Vector2 PointToScreen(Vector2 pt)
        {
            return pt * ScreenRange + ScreenMin;
        }

        public Vector2 ScreenToPoint(Vector2 pt)
        {
            return (pt - ScreenMin) / ScreenRange;
        }

        public Vector2 ScreenToCanvas(Vector2 pt)
        {
            return PointToCanvas(ScreenToPoint(pt));
        }

        public Vector2 CanvasToScreen(Vector2 pt)
        {
            return PointToScreen(CanvasToPoint(pt));
        }

        public abstract int GetCurveCount();

        public virtual bool IsVisible(int curveIndex)
        { return true; }

        public virtual CurveType GetCurveType(int curveIndex)
        { return CurveType.CurveSmooth; }

        public abstract int GetPointCount(int curveIndex);

        public abstract uint GetCurveColor(int curveIndex);

        public abstract Vector2[] GetPoints(int curveIndex);

        public abstract int EditPoint(int curveIndex, int pointIndex, Vector2 value);

        public abstract void AddPoint(int curveIndex, Vector2 value);

        public virtual uint GetBackgroundColor()
        { return 0x1B202020; }

        // handle undo/redo thru this functions
        public virtual void BeginEdit(int index)
        { }

        public virtual void EndEdit()
        { }
    };
}