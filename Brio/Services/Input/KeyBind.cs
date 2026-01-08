using Dalamud.Game.ClientState.Keys;

namespace Brio.Input;

public class KeyBind
{
    public VirtualKey Key { get; set; }
    public bool Control { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }

    public KeyBind()
    {
    }

    public KeyBind(VirtualKey key, bool control = false, bool alt = false, bool shift = false)
    {
        Key = key;
        Control = control;
        Alt = alt;
        Shift = shift;
    }

    public bool GetIsEmpty()
    {
        return this.Key == VirtualKey.NO_KEY;
    }

    public override string ToString()
    {
        if(!Control && !Alt && !Shift)
            return Key.GetFancyName();

        string str = string.Empty;

        if(Control)
            str += "Ctrl ";

        if(Alt)
            str += "Alt ";

        if(Shift)
            str += "Shift ";

        str += $"+ {Key.GetFancyName()}";
        return str;
    }
}
