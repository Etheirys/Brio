using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Game.Input;

[StructLayout(LayoutKind.Sequential)]
public struct MouseFrame
{
    public int PositionX;
    public int PositionY;
    public int ScrollValue;
    public MouseState ButtonsPressed;
    public MouseState ButtonsClicked;
    public ulong Unknown1;
    public int DeltaX;
    public int DeltaY;

    public readonly bool IsKeyDown(MouseState mouseButton)
        => ButtonsPressed.HasFlag(mouseButton);

    public readonly Vector2 GetDeltaAsVector2()
        => new(DeltaX, DeltaY);

    public readonly Vector2 GetPositionAsVector2()
        => new(PositionX, PositionY);

    public void HandleDelta()
    {
        DeltaX = DeltaY = 0;
    }
}

public enum MouseState
{
    None = 0,
    Left = 1,
    Middle = 2,
    Right = 4, // Why is this like this??, it's a flag you dummy 
}
