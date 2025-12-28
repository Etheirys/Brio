using Dalamud.Interface.Style;
using Dalamud.Bindings.ImGui;
using Color = Dalamud.Interface.Style.StyleModelV1;

namespace ImSequencer;


public class SequencerStyle
{

    private static uint DalamudStyle(string type)
    {
        return ImGui.ColorConvertFloat4ToU32(Color.DalamudStandard.Colors[type]);
    }
    public uint Text = DalamudStyle("Text");
    public uint FrameBg = DalamudStyle("FrameBg");
    public uint Header = DalamudStyle("Header");
    public uint MenuBarBg = DalamudStyle("MenuBarBg");
}