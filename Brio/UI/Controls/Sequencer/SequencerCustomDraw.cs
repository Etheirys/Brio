using Dalamud.Bindings.ImGui;

namespace ImSequencer
{
    public struct SequencerCustomDraw
    {

        public int Index;
        public ImRect CustomRect;
        public ImRect LegendRect;
        public ImRect ClippingRect;
        public ImRect LegendClippingRect;


        public SequencerCustomDraw(
            int index, ImRect customRect, ImRect legendRect, ImRect clippingRect, ImRect legendClippingRect)
        {
            Index = index;
            CustomRect = customRect;
            LegendRect = legendRect;
            ClippingRect = clippingRect;
            LegendClippingRect = legendClippingRect;
        }


    }
}




