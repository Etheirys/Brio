using Dalamud.Game.ClientState.Keys;

namespace Brio.Input;
public class KeyConfig(VirtualKey key, bool requireShift = false, bool requireCtrl = false, bool requireAlt = false)
{
    public VirtualKey key = key;
    public bool requireShift = requireShift;
    public bool requireCtrl = requireCtrl;
    public bool requireAlt = requireAlt;

    public bool isShift = key == VirtualKey.SHIFT;
    public bool isCtrl = key == VirtualKey.CONTROL;
    public bool isAlt = key == VirtualKey.MENU;

    public override string ToString()
    {
        string result = string.Empty;
        if (requireCtrl)
        {
            result += "Ctrl+";
        }
        if(requireAlt)
        {
            result += "Alt+";
        }
        if(requireShift)
        {
            result += "Shift+";
        }

        var fancy = key.GetFancyName();
        if (fancy == "Control")
        {
            result += "Ctrl";
        } else
        {
            result += fancy;
        }

        return result;
    }
}
