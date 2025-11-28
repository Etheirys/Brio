using Dalamud.Bindings.ImGui;

namespace ImSequencer;

public unsafe interface SequenceInterface
{
    bool focused { get; set; }
    int GetFrameMin();
    int GetFrameMax();
    int GetItemCount();
    void BeginEdit(int index);
    void EndEdit();
    int GetItemTypeCount();
    string GetItemTypeName(int index);
    string GetItemLabel(int index);
    string GetCollapseFmt(int frameCount, int sequenceCount);
    void Get(int index, int** start, int** end, int* type, uint* color);
    void Add(int index);
    void Del(int index);
    void Duplicate(int index);
    void Copy();
    void Paste();
    uint GetCustomHeight(int index);
    void DoubleClick(int index);
    void CustomDraw(int index, ImDrawListPtr drawList, ImRect customRect, ImRect legendRect, ImRect clippingRect, ImRect legendClippingRect);
    void CustomDrawCompact(int index, ImDrawListPtr drawList, ImRect customRect, ImRect clippingRect);
}
