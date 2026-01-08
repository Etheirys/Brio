using Dalamud.Game.ClientState.Keys;

namespace Brio.Input;

public struct KeyConfig(VirtualKey key, bool requireShift = false, bool requireCtrl = false, bool requireAlt = false)
{
    public VirtualKey Key = key;
    public bool RequireShift = requireShift;
    public bool RequireCtrl = requireCtrl;
    public bool RequireAlt = requireAlt;

    public override readonly string ToString()
    {
        string result = string.Empty;
        if(RequireCtrl)
        {
            result += "Ctrl+";
        }
        if(RequireAlt)
        {
            result += "Alt+";
        }
        if(RequireShift)
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
