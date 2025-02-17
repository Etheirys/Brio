using Dalamud.Game.ClientState.Keys;
using System.Runtime.InteropServices;

namespace Brio.Game.Input;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct KeyboardFrame
{
    public const int KeyStateLength = 254;

    public byte Unknown1;
    public fixed uint KeyState[KeyStateLength];

    public readonly bool KeyDown(VirtualKey virtualKey)
        => KeyState[(int)virtualKey] is not 0;

    public void HandleKey(VirtualKey virtualKey)
        => KeyState[(int)virtualKey] = 0;

    public void HandleAllKeys()
    {
        for(int i = 0; i < KeyStateLength; i++)
            KeyState[i] = 0;
    }

    public bool IsKeyDown(VirtualKey virtualKey, bool handle)
    {
        var isDown = KeyDown(virtualKey);
        if(handle)
            HandleKey(virtualKey);
        return isDown;
    }
}
