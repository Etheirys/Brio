namespace ImSequencer.ImCurveEdit
{
    public struct EditPoint
    {
        public int curveIndex;
        public int pointIndex;

        public EditPoint(int curveIndex, int pointIndex)
        {
            this.curveIndex = curveIndex;
            this.pointIndex = pointIndex;
        }

        public static bool operator <(in EditPoint a, in EditPoint b)
        {
            if (a.curveIndex < b.curveIndex)

                return true;
            if (a.curveIndex > b.curveIndex)
                return false;

            if (a.pointIndex < b.pointIndex)
                return true;
            return false;
        }

        public static bool operator >(in EditPoint a, in EditPoint b)
        {
            if (a.curveIndex > b.curveIndex)

                return true;
            if (a.curveIndex < b.curveIndex)
                return false;

            if (a.pointIndex > b.pointIndex)
                return true;
            return false;
        }
    };
}