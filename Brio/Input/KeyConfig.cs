using Dalamud.Game.ClientState.Keys;

namespace Brio.Input;
public class KeyConfig(VirtualKey key, bool requireShift = false, bool requireCtrl = false, bool requireAlt = false)
{
    public VirtualKey Key = key;
    public bool requireShift = requireShift;
    public bool RequireCtrl = requireCtrl;
    public bool requireAlt = requireAlt;

    public override string ToString()
    {
        string result = string.Empty;
        if(RequireCtrl)
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

        var fancy = Key.GetFancyName();
        if(fancy == "Control")
        {
            result += "Ctrl";
        }
        else
        {
            result += fancy;
        }

        return result;
    }
}
