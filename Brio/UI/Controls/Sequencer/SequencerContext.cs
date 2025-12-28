using System.Numerics;

namespace ImSequencer;

public class SeqContext
{
    public struct SequencerContext {
        public float framePixelWidth = 10f;
        public float framePixelWidthTarget = 10f;
        public int legendWidth = 200;

        public int movingEntry = -1;
        public int movingPos = -1;
        public int movingPart = -1;
        public int delEntry = -1;
        public int dupEntry = -1;
        public int ItemHeight = 20;
        
        public bool MovingScrollBar = false;
        public bool MovingCurrentFrame = false;

        public bool PanningView = false;
        public Vector2 PanningViewSource;
        public int PanningViewFrame;
        public bool SizingRBar = false;
        public bool SizingLBar = false;
        public SequencerStyle Style = new SequencerStyle();
        public ZoomScrollbar.State ScrollbarState = new();
        public SequencerContext()
        {
        }
    }



}