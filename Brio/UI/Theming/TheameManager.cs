using System.Numerics;

namespace Brio.UI.Theming;

public static class ThemeManager
{

    public static Theme CurrentTheme { get; set; }

    static ThemeManager()
    {
        CurrentTheme = new Theme
        {
            Name = "Default",
            Accent = new ThemeAccent
            {
                AccentColor = SetColor(new Vector4(98, 75, 224, 255)),
                AccentColorLight = SetColor(new Vector4(98, 75, 224, 255)),
                AccentColorStrong = SetColor(new Vector4(98, 75, 224, 255)),
                AccentColorDim = SetColor(new Vector4(98, 75, 224, 255)),

                AccentCheckMark = SetColor(new Vector4(98, 75, 224, 255)),
                AccentButtonHovered = SetColor(new Vector4(74, 56, 170, 255)),

                AccentTabActive = SetColor(new Vector4(98, 75, 224, 255)),
                AccentTabUnfocusedActive = SetColor(new Vector4(73, 48, 205, 255)),
            },
            Core = new ThemeCore
            {

            }
        };
    }

    static uint SetColor(Vector4 colorVector)
    {
        uint r = (uint)(colorVector.X) & 0xFF;
        uint g = (uint)(colorVector.Y) & 0xFF;
        uint b = (uint)(colorVector.Z) & 0xFF;
        uint a = (uint)(colorVector.W) & 0xFF;

        return (a << 24) | (b << 16) | (g << 8) | r;
    }
}

public record class Theme
{
    public required string Name;

    public required ThemeAccent Accent;

    public required ThemeCore Core;
}

public record class ThemeAccent
{
    public uint AccentColor = 0;
    public uint AccentColorLight;
    public uint AccentColorStrong;
    public uint AccentColorDim;


    public uint AccentCheckMark;
    public uint AccentButtonHovered;

    public uint AccentTabActive;
    public uint AccentTabUnfocusedActive;
}

public record class ThemeCore
{
    public uint Text;
}
