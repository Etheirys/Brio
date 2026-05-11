using System.Numerics;

namespace Brio.UI.Theming;

//
// Good god, I am never making anther Theme again, this was a nightmare to make and I don't want to do it again.
// They also should have been data driven, but I was procrastinating on this and I didn't wanna (TODO) >:|
//

public static class ThemeManager
{
    public static Theme CurrentTheme { get; set; }

    static ThemeManager()
    {
        CurrentTheme = BrioDark;
    }

    public static Theme BrioDark { get; } = new Theme
    {
        Name = "Brio Dark",
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
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(255, 255, 255, 255)),
            TextDisabled = SetColor(new Vector4(128, 128, 128, 255)),
            TextSelectedBg = SetColor(new Vector4(98, 75, 224, 255)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(25, 25, 25, 248)),
            ChildBg = SetColor(new Vector4(25, 25, 25, 66)),
            PopupBg = SetColor(new Vector4(25, 25, 25, 248)),
            Border = SetColor(new Vector4(44, 44, 44, 255)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 128)),
            TitleBg = SetColor(new Vector4(27, 27, 27, 232)),
            TitleBgActive = SetColor(new Vector4(33, 33, 33, 255)),
            TitleBgCollapsed = SetColor(new Vector4(30, 30, 30, 255)),
            MenuBarBg = SetColor(new Vector4(36, 36, 36, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(36, 36, 36, 255)),
            FrameBgHovered = SetColor(new Vector4(57, 57, 57, 255)),
            FrameBgActive = SetColor(new Vector4(33, 33, 3, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(62, 62, 62, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(70, 70, 70, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(70, 70, 70, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(101, 101, 101, 255)),
            SliderGrabActive = SetColor(new Vector4(123, 123, 123, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(255, 255, 255, 31)),
            ButtonHovered = SetColor(new Vector4(74, 56, 170, 255)),
            ButtonActive = SetColor(new Vector4(54, 42, 122, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(0, 0, 0, 60)),
            HeaderHovered = SetColor(new Vector4(0, 0, 0, 90)),
            HeaderActive = SetColor(new Vector4(0, 0, 0, 120)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(75, 75, 75, 121)),
            SeparatorHovered = SetColor(new Vector4(37, 25, 98, 255)),
            SeparatorActive = SetColor(new Vector4(98, 75, 224, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(41, 41, 41, 255)),
            TabHovered = SetColor(new Vector4(42, 29, 113, 255)),
            TabActive = SetColor(new Vector4(98, 75, 224, 255)),
            TabUnfocused = SetColor(new Vector4(41, 39, 41, 255)),
            TabUnfocusedActive = SetColor(new Vector4(73, 48, 205, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(91, 70, 208, 105)),
            DockingEmptyBg = SetColor(new Vector4(51, 51, 51, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(48, 48, 48, 255)),
            TableBorderStrong = SetColor(new Vector4(79, 79, 89, 255)),
            TableBorderLight = SetColor(new Vector4(59, 59, 64, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(98, 75, 224, 255)),
            PlotLines = SetColor(new Vector4(156, 156, 156, 255)),
            DragDropTarget = SetColor(new Vector4(98, 75, 224, 255)),
            NavHighlight = SetColor(new Vector4(98, 75, 224, 179)),
            NavWindowingDimBg = SetColor(new Vector4(204, 204, 204, 51)),
            NavWindowingHighlight = SetColor(new Vector4(204, 204, 204, 89)),
        },
    };

    public static Theme BrioLight { get; } = new Theme
    {
        Name = "Brio Light",
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
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(30, 30, 30, 255)),
            TextDisabled = SetColor(new Vector4(160, 160, 160, 255)),
            TextSelectedBg = SetColor(new Vector4(98, 75, 224, 120)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(255, 255, 255, 255)),
            ChildBg = SetColor(new Vector4(245, 245, 245, 120)),
            PopupBg = SetColor(new Vector4(255, 255, 255, 255)),
            Border = SetColor(new Vector4(210, 210, 210, 255)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 20)),
            TitleBg = SetColor(new Vector4(235, 235, 235, 255)),
            TitleBgActive = SetColor(new Vector4(225, 225, 225, 255)),
            TitleBgCollapsed = SetColor(new Vector4(230, 230, 230, 255)),
            MenuBarBg = SetColor(new Vector4(240, 240, 240, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(225, 225, 225, 255)),
            FrameBgHovered = SetColor(new Vector4(210, 210, 210, 255)),
            FrameBgActive = SetColor(new Vector4(195, 195, 195, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(190, 190, 190, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(160, 160, 160, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(135, 135, 135, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(160, 160, 160, 255)),
            SliderGrabActive = SetColor(new Vector4(120, 120, 120, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(210, 210, 210, 255)),
            ButtonHovered = SetColor(new Vector4(185, 185, 185, 255)),
            ButtonActive = SetColor(new Vector4(160, 160, 160, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(0, 0, 0, 30)),
            HeaderHovered = SetColor(new Vector4(0, 0, 0, 55)),
            HeaderActive = SetColor(new Vector4(0, 0, 0, 80)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(200, 200, 200, 200)),
            SeparatorHovered = SetColor(new Vector4(150, 150, 150, 255)),
            SeparatorActive = SetColor(new Vector4(100, 100, 100, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(220, 220, 220, 255)),
            TabHovered = SetColor(new Vector4(195, 195, 195, 255)),
            TabActive = SetColor(new Vector4(175, 175, 175, 255)),
            TabUnfocused = SetColor(new Vector4(225, 225, 225, 255)),
            TabUnfocusedActive = SetColor(new Vector4(200, 200, 200, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(150, 150, 150, 105)),
            DockingEmptyBg = SetColor(new Vector4(235, 235, 235, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(215, 215, 215, 255)),
            TableBorderStrong = SetColor(new Vector4(170, 170, 170, 255)),
            TableBorderLight = SetColor(new Vector4(200, 200, 200, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(0, 0, 0, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(100, 100, 100, 255)),
            PlotLines = SetColor(new Vector4(100, 100, 100, 255)),
            DragDropTarget = SetColor(new Vector4(80, 80, 80, 255)),
            NavHighlight = SetColor(new Vector4(100, 100, 100, 179)),
            NavWindowingDimBg = SetColor(new Vector4(100, 100, 100, 51)),
            NavWindowingHighlight = SetColor(new Vector4(100, 100, 100, 89)),
        },
    };

    public static Theme ArchonBlue { get; } = new Theme
    {
        Name = "Archon Blue",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(255, 255, 255, 255)),
            AccentColorLight = SetColor(new Vector4(255, 255, 255, 255)),
            AccentColorStrong = SetColor(new Vector4(255, 255, 255, 255)),
            AccentColorDim = SetColor(new Vector4(255, 255, 255, 255)),
            AccentCheckMark = SetColor(new Vector4(219, 219, 219, 255)),
            AccentButtonHovered = SetColor(new Vector4(29, 76, 230, 201)),
            AccentTabActive = SetColor(new Vector4(20, 20, 58, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(35, 67, 108, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(255, 255, 255, 255)),
            TextDisabled = SetColor(new Vector4(128, 128, 128, 255)),
            TextSelectedBg = SetColor(new Vector4(66, 151, 250, 89)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(29, 48, 108, 229)),
            ChildBg = SetColor(new Vector4(0, 0, 0, 0)),
            PopupBg = SetColor(new Vector4(17, 28, 50, 235)),
            Border = SetColor(new Vector4(17, 78, 174, 113)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 0)),
            TitleBg = SetColor(new Vector4(21, 40, 100, 224)),
            TitleBgActive = SetColor(new Vector4(21, 40, 100, 224)),
            TitleBgCollapsed = SetColor(new Vector4(21, 40, 100, 224)),
            MenuBarBg = SetColor(new Vector4(36, 36, 36, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(33, 54, 96, 224)),
            FrameBgHovered = SetColor(new Vector4(142, 142, 142, 102)),
            FrameBgActive = SetColor(new Vector4(163, 163, 163, 171)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(78, 101, 148, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(115, 144, 199, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(99, 127, 180, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(107, 129, 185, 255)),
            SliderGrabActive = SetColor(new Vector4(171, 171, 171, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(31, 63, 171, 177)),
            ButtonHovered = SetColor(new Vector4(29, 76, 230, 201)),
            ButtonActive = SetColor(new Vector4(48, 76, 189, 241)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(48, 90, 255, 79)),
            HeaderHovered = SetColor(new Vector4(88, 121, 254, 204)),
            HeaderActive = SetColor(new Vector4(78, 140, 238, 226)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(110, 110, 128, 128)),
            SeparatorHovered = SetColor(new Vector4(16, 29, 74, 224)),
            SeparatorActive = SetColor(new Vector4(20, 20, 93, 241)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(33, 54, 96, 224)),
            TabHovered = SetColor(new Vector4(17, 28, 50, 224)),
            TabActive = SetColor(new Vector4(20, 20, 58, 224)),
            TabUnfocused = SetColor(new Vector4(17, 28, 50, 224)),
            TabUnfocusedActive = SetColor(new Vector4(35, 67, 108, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(66, 150, 250, 179)),
            DockingEmptyBg = SetColor(new Vector4(51, 51, 51, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(48, 48, 51, 255)),
            TableBorderStrong = SetColor(new Vector4(79, 79, 89, 255)),
            TableBorderLight = SetColor(new Vector4(59, 59, 64, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(201, 201, 201, 64)),
            ResizeGripHovered = SetColor(new Vector4(199, 199, 199, 171)),
            ResizeGripActive = SetColor(new Vector4(11, 21, 54, 224)),
            PlotLines = SetColor(new Vector4(156, 156, 156, 255)),
            DragDropTarget = SetColor(new Vector4(255, 255, 0, 230)),
            NavHighlight = SetColor(new Vector4(66, 150, 250, 255)),
            NavWindowingDimBg = SetColor(new Vector4(204, 204, 204, 51)),
            NavWindowingHighlight = SetColor(new Vector4(255, 255, 255, 179)),
        },
    };

    public static Theme ForestAuraWarm { get; } = new Theme
    {
        Name = "Forest Aura Green",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(100, 180, 90, 255)),
            AccentColorLight = SetColor(new Vector4(120, 195, 110, 255)),
            AccentColorStrong = SetColor(new Vector4(135, 210, 120, 255)),
            AccentColorDim = SetColor(new Vector4(75, 150, 65, 255)),
            AccentCheckMark = SetColor(new Vector4(120, 195, 100, 255)),
            AccentButtonHovered = SetColor(new Vector4(60, 130, 50, 255)),
            AccentTabActive = SetColor(new Vector4(65, 120, 45, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(48, 95, 35, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(210, 225, 200, 255)),
            TextDisabled = SetColor(new Vector4(100, 115, 85, 255)),
            TextSelectedBg = SetColor(new Vector4(65, 130, 50, 120)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(16, 28, 14, 250)),
            ChildBg = SetColor(new Vector4(20, 33, 17, 100)),
            PopupBg = SetColor(new Vector4(14, 26, 12, 250)),
            Border = SetColor(new Vector4(45, 75, 35, 180)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 60)),
            TitleBg = SetColor(new Vector4(12, 22, 10, 240)),
            TitleBgActive = SetColor(new Vector4(18, 30, 14, 255)),
            TitleBgCollapsed = SetColor(new Vector4(15, 26, 12, 255)),
            MenuBarBg = SetColor(new Vector4(22, 35, 18, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(28, 45, 22, 255)),
            FrameBgHovered = SetColor(new Vector4(40, 60, 30, 255)),
            FrameBgActive = SetColor(new Vector4(33, 52, 25, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(48, 78, 35, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(65, 105, 48, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(80, 125, 58, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(85, 140, 65, 255)),
            SliderGrabActive = SetColor(new Vector4(110, 170, 85, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(33, 55, 25, 200)),
            ButtonHovered = SetColor(new Vector4(60, 130, 50, 220)),
            ButtonActive = SetColor(new Vector4(45, 95, 35, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(0, 0, 0, 60)),
            HeaderHovered = SetColor(new Vector4(0, 0, 0, 90)),
            HeaderActive = SetColor(new Vector4(0, 0, 0, 120)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(48, 78, 35, 150)),
            SeparatorHovered = SetColor(new Vector4(75, 125, 55, 255)),
            SeparatorActive = SetColor(new Vector4(100, 180, 90, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(25, 40, 18, 255)),
            TabHovered = SetColor(new Vector4(48, 78, 35, 255)),
            TabActive = SetColor(new Vector4(65, 120, 45, 255)),
            TabUnfocused = SetColor(new Vector4(22, 36, 16, 255)),
            TabUnfocusedActive = SetColor(new Vector4(48, 95, 35, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(80, 160, 65, 105)),
            DockingEmptyBg = SetColor(new Vector4(14, 24, 10, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(26, 42, 20, 255)),
            TableBorderStrong = SetColor(new Vector4(50, 80, 38, 255)),
            TableBorderLight = SetColor(new Vector4(35, 58, 26, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(180, 255, 160, 10)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(100, 180, 90, 255)),
            PlotLines = SetColor(new Vector4(110, 175, 90, 255)),
            DragDropTarget = SetColor(new Vector4(100, 180, 90, 255)),
            NavHighlight = SetColor(new Vector4(100, 180, 90, 179)),
            NavWindowingDimBg = SetColor(new Vector4(120, 150, 100, 51)),
            NavWindowingHighlight = SetColor(new Vector4(175, 210, 155, 89)),
        },
    };

    public static Theme Sepia { get; } = new Theme
    {
        Name = "Sepia Dark",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(200, 160, 100, 255)),
            AccentColorLight = SetColor(new Vector4(215, 175, 120, 255)),
            AccentColorStrong = SetColor(new Vector4(225, 185, 130, 255)),
            AccentColorDim = SetColor(new Vector4(170, 135, 80, 255)),
            AccentCheckMark = SetColor(new Vector4(210, 175, 120, 255)),
            AccentButtonHovered = SetColor(new Vector4(155, 115, 60, 255)),
            AccentTabActive = SetColor(new Vector4(160, 120, 65, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(130, 95, 45, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(225, 210, 185, 255)),
            TextDisabled = SetColor(new Vector4(120, 105, 80, 255)),
            TextSelectedBg = SetColor(new Vector4(160, 120, 65, 120)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(28, 22, 16, 250)),
            ChildBg = SetColor(new Vector4(33, 26, 18, 100)),
            PopupBg = SetColor(new Vector4(26, 20, 14, 250)),
            Border = SetColor(new Vector4(75, 58, 38, 180)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 60)),
            TitleBg = SetColor(new Vector4(22, 17, 12, 240)),
            TitleBgActive = SetColor(new Vector4(30, 23, 16, 255)),
            TitleBgCollapsed = SetColor(new Vector4(26, 20, 14, 255)),
            MenuBarBg = SetColor(new Vector4(35, 27, 18, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(45, 35, 22, 255)),
            FrameBgHovered = SetColor(new Vector4(60, 47, 30, 255)),
            FrameBgActive = SetColor(new Vector4(52, 40, 25, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(80, 62, 40, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(105, 82, 52, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(125, 98, 62, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(140, 108, 68, 255)),
            SliderGrabActive = SetColor(new Vector4(170, 135, 88, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(55, 42, 27, 200)),
            ButtonHovered = SetColor(new Vector4(155, 115, 60, 220)),
            ButtonActive = SetColor(new Vector4(110, 82, 42, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(0, 0, 0, 60)),
            HeaderHovered = SetColor(new Vector4(0, 0, 0, 90)),
            HeaderActive = SetColor(new Vector4(0, 0, 0, 120)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(80, 62, 40, 150)),
            SeparatorHovered = SetColor(new Vector4(130, 100, 60, 255)),
            SeparatorActive = SetColor(new Vector4(200, 160, 100, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(40, 31, 20, 255)),
            TabHovered = SetColor(new Vector4(75, 57, 35, 255)),
            TabActive = SetColor(new Vector4(160, 120, 65, 255)),
            TabUnfocused = SetColor(new Vector4(37, 28, 18, 255)),
            TabUnfocusedActive = SetColor(new Vector4(130, 95, 45, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(160, 120, 65, 105)),
            DockingEmptyBg = SetColor(new Vector4(25, 19, 13, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(42, 33, 21, 255)),
            TableBorderStrong = SetColor(new Vector4(80, 62, 40, 255)),
            TableBorderLight = SetColor(new Vector4(58, 45, 28, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 220, 180, 10)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(200, 160, 100, 255)),
            PlotLines = SetColor(new Vector4(180, 145, 95, 255)),
            DragDropTarget = SetColor(new Vector4(200, 160, 100, 255)),
            NavHighlight = SetColor(new Vector4(200, 160, 100, 179)),
            NavWindowingDimBg = SetColor(new Vector4(160, 140, 110, 51)),
            NavWindowingHighlight = SetColor(new Vector4(200, 185, 155, 89)),
        },
    };

    public static Theme Slate { get; } = new Theme
    {
        Name = "Slate",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(180, 180, 180, 255)),
            AccentColorLight = SetColor(new Vector4(200, 200, 200, 255)),
            AccentColorStrong = SetColor(new Vector4(220, 220, 220, 255)),
            AccentColorDim = SetColor(new Vector4(140, 140, 140, 255)),
            AccentCheckMark = SetColor(new Vector4(220, 220, 220, 255)),
            AccentButtonHovered = SetColor(new Vector4(130, 130, 130, 255)),
            AccentTabActive = SetColor(new Vector4(160, 160, 160, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(120, 120, 120, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(220, 220, 220, 255)),
            TextDisabled = SetColor(new Vector4(100, 100, 100, 255)),
            TextSelectedBg = SetColor(new Vector4(160, 160, 160, 120)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(30, 30, 30, 250)),
            ChildBg = SetColor(new Vector4(35, 35, 35, 100)),
            PopupBg = SetColor(new Vector4(28, 28, 28, 250)),
            Border = SetColor(new Vector4(60, 60, 60, 180)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 60)),
            TitleBg = SetColor(new Vector4(22, 22, 22, 240)),
            TitleBgActive = SetColor(new Vector4(28, 28, 28, 255)),
            TitleBgCollapsed = SetColor(new Vector4(25, 25, 25, 255)),
            MenuBarBg = SetColor(new Vector4(35, 35, 35, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(45, 45, 45, 255)),
            FrameBgHovered = SetColor(new Vector4(60, 60, 60, 255)),
            FrameBgActive = SetColor(new Vector4(55, 55, 55, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(75, 75, 75, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(95, 95, 95, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(115, 115, 115, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(130, 130, 130, 255)),
            SliderGrabActive = SetColor(new Vector4(160, 160, 160, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(55, 55, 55, 200)),
            ButtonHovered = SetColor(new Vector4(90, 90, 90, 220)),
            ButtonActive = SetColor(new Vector4(70, 70, 70, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(0, 0, 0, 60)),
            HeaderHovered = SetColor(new Vector4(0, 0, 0, 90)),
            HeaderActive = SetColor(new Vector4(0, 0, 0, 120)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(75, 75, 75, 150)),
            SeparatorHovered = SetColor(new Vector4(120, 120, 120, 255)),
            SeparatorActive = SetColor(new Vector4(180, 180, 180, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(40, 40, 40, 255)),
            TabHovered = SetColor(new Vector4(70, 70, 70, 255)),
            TabActive = SetColor(new Vector4(90, 90, 90, 255)),
            TabUnfocused = SetColor(new Vector4(38, 38, 38, 255)),
            TabUnfocusedActive = SetColor(new Vector4(65, 65, 65, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(150, 150, 150, 105)),
            DockingEmptyBg = SetColor(new Vector4(25, 25, 25, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(42, 42, 42, 255)),
            TableBorderStrong = SetColor(new Vector4(75, 75, 75, 255)),
            TableBorderLight = SetColor(new Vector4(55, 55, 55, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 10)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(180, 180, 180, 255)),
            PlotLines = SetColor(new Vector4(160, 160, 160, 255)),
            DragDropTarget = SetColor(new Vector4(200, 200, 200, 255)),
            NavHighlight = SetColor(new Vector4(180, 180, 180, 179)),
            NavWindowingDimBg = SetColor(new Vector4(150, 150, 150, 51)),
            NavWindowingHighlight = SetColor(new Vector4(200, 200, 200, 89)),
        },
    };

    public static Theme Midnight { get; } = new Theme
    {
        Name = "Meta-Midnight",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(100, 120, 200, 255)),
            AccentColorLight = SetColor(new Vector4(100, 120, 200, 255)),
            AccentColorStrong = SetColor(new Vector4(100, 120, 200, 255)),
            AccentColorDim = SetColor(new Vector4(100, 120, 200, 255)),
            AccentCheckMark = SetColor(new Vector4(150, 165, 210, 255)),
            AccentButtonHovered = SetColor(new Vector4(60, 75, 150, 201)),
            AccentTabActive = SetColor(new Vector4(25, 30, 70, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(40, 50, 110, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(200, 205, 225, 255)),
            TextDisabled = SetColor(new Vector4(90, 95, 120, 255)),
            TextSelectedBg = SetColor(new Vector4(60, 75, 150, 120)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(8, 10, 30, 250)),
            ChildBg = SetColor(new Vector4(10, 12, 35, 100)),
            PopupBg = SetColor(new Vector4(8, 10, 28, 250)),
            Border = SetColor(new Vector4(30, 40, 90, 180)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 60)),
            TitleBg = SetColor(new Vector4(6, 8, 25, 240)),
            TitleBgActive = SetColor(new Vector4(10, 12, 35, 255)),
            TitleBgCollapsed = SetColor(new Vector4(8, 10, 28, 255)),
            MenuBarBg = SetColor(new Vector4(12, 14, 38, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(15, 18, 45, 255)),
            FrameBgHovered = SetColor(new Vector4(25, 30, 65, 255)),
            FrameBgActive = SetColor(new Vector4(20, 25, 55, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(30, 38, 85, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(45, 55, 110, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(55, 68, 130, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(60, 75, 150, 255)),
            SliderGrabActive = SetColor(new Vector4(80, 95, 170, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(20, 25, 65, 200)),
            ButtonHovered = SetColor(new Vector4(60, 75, 150, 220)),
            ButtonActive = SetColor(new Vector4(40, 52, 120, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(0, 0, 0, 60)),
            HeaderHovered = SetColor(new Vector4(0, 0, 0, 90)),
            HeaderActive = SetColor(new Vector4(0, 0, 0, 120)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(30, 38, 85, 150)),
            SeparatorHovered = SetColor(new Vector4(50, 65, 130, 255)),
            SeparatorActive = SetColor(new Vector4(100, 120, 200, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(12, 15, 40, 255)),
            TabHovered = SetColor(new Vector4(30, 38, 85, 255)),
            TabActive = SetColor(new Vector4(25, 30, 70, 255)),
            TabUnfocused = SetColor(new Vector4(10, 12, 35, 255)),
            TabUnfocusedActive = SetColor(new Vector4(40, 50, 110, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(60, 75, 150, 105)),
            DockingEmptyBg = SetColor(new Vector4(10, 12, 30, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(15, 18, 48, 255)),
            TableBorderStrong = SetColor(new Vector4(35, 42, 90, 255)),
            TableBorderLight = SetColor(new Vector4(22, 27, 65, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 10)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(100, 120, 200, 255)),
            PlotLines = SetColor(new Vector4(100, 120, 180, 255)),
            DragDropTarget = SetColor(new Vector4(100, 120, 200, 255)),
            NavHighlight = SetColor(new Vector4(100, 120, 200, 179)),
            NavWindowingDimBg = SetColor(new Vector4(150, 155, 180, 51)),
            NavWindowingHighlight = SetColor(new Vector4(180, 190, 220, 89)),
        },
    };

    public static Theme CubeOrange { get; } = new Theme
    {
        Name = "Cube Orange",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(224, 138, 75, 255)),
            AccentColorLight = SetColor(new Vector4(224, 138, 75, 255)),
            AccentColorStrong = SetColor(new Vector4(224, 138, 75, 255)),
            AccentColorDim = SetColor(new Vector4(224, 138, 75, 255)),
            AccentCheckMark = SetColor(new Vector4(219, 195, 180, 255)),
            AccentButtonHovered = SetColor(new Vector4(170, 90, 29, 201)),
            AccentTabActive = SetColor(new Vector4(58, 35, 20, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(108, 68, 35, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(255, 255, 255, 255)),
            TextDisabled = SetColor(new Vector4(128, 128, 128, 255)),
            TextSelectedBg = SetColor(new Vector4(200, 130, 66, 89)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(108, 60, 29, 229)),
            ChildBg = SetColor(new Vector4(0, 0, 0, 0)),
            PopupBg = SetColor(new Vector4(50, 28, 17, 235)),
            Border = SetColor(new Vector4(174, 100, 17, 113)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 0)),
            TitleBg = SetColor(new Vector4(100, 50, 21, 224)),
            TitleBgActive = SetColor(new Vector4(100, 50, 21, 224)),
            TitleBgCollapsed = SetColor(new Vector4(100, 50, 21, 224)),
            MenuBarBg = SetColor(new Vector4(46, 30, 22, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(96, 56, 33, 224)),
            FrameBgHovered = SetColor(new Vector4(142, 110, 100, 102)),
            FrameBgActive = SetColor(new Vector4(163, 130, 120, 171)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(148, 95, 62, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(199, 130, 90, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(180, 112, 75, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(185, 120, 90, 255)),
            SliderGrabActive = SetColor(new Vector4(171, 145, 130, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(171, 95, 31, 177)),
            ButtonHovered = SetColor(new Vector4(170, 90, 29, 201)),
            ButtonActive = SetColor(new Vector4(130, 68, 24, 241)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(255, 140, 48, 79)),
            HeaderHovered = SetColor(new Vector4(254, 160, 88, 204)),
            HeaderActive = SetColor(new Vector4(220, 130, 78, 226)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(128, 95, 75, 128)),
            SeparatorHovered = SetColor(new Vector4(74, 35, 16, 224)),
            SeparatorActive = SetColor(new Vector4(93, 50, 20, 241)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(96, 56, 33, 224)),
            TabHovered = SetColor(new Vector4(50, 28, 17, 224)),
            TabActive = SetColor(new Vector4(58, 35, 20, 224)),
            TabUnfocused = SetColor(new Vector4(50, 28, 17, 224)),
            TabUnfocusedActive = SetColor(new Vector4(108, 68, 35, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(208, 130, 70, 105)),
            DockingEmptyBg = SetColor(new Vector4(51, 33, 22, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(48, 33, 20, 255)),
            TableBorderStrong = SetColor(new Vector4(89, 62, 40, 255)),
            TableBorderLight = SetColor(new Vector4(64, 46, 30, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(201, 160, 150, 64)),
            ResizeGripHovered = SetColor(new Vector4(199, 155, 150, 171)),
            ResizeGripActive = SetColor(new Vector4(54, 28, 11, 224)),
            PlotLines = SetColor(new Vector4(180, 140, 100, 255)),
            DragDropTarget = SetColor(new Vector4(255, 160, 0, 230)),
            NavHighlight = SetColor(new Vector4(200, 130, 66, 255)),
            NavWindowingDimBg = SetColor(new Vector4(204, 185, 180, 51)),
            NavWindowingHighlight = SetColor(new Vector4(255, 220, 200, 179)),
        },
    };

    public static Theme CubeIndigo { get; } = new Theme
    {
        Name = "Cube Indigo",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(80, 60, 200, 255)),
            AccentColorLight = SetColor(new Vector4(100, 80, 220, 255)),
            AccentColorStrong = SetColor(new Vector4(60, 45, 175, 255)),
            AccentColorDim = SetColor(new Vector4(45, 32, 145, 255)),
            AccentCheckMark = SetColor(new Vector4(80, 200, 90, 255)),
            AccentButtonHovered = SetColor(new Vector4(65, 50, 185, 201)),
            AccentTabActive = SetColor(new Vector4(30, 22, 95, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(50, 38, 138, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(235, 235, 255, 255)),
            TextDisabled = SetColor(new Vector4(120, 115, 165, 255)),
            TextSelectedBg = SetColor(new Vector4(80, 60, 200, 89)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(28, 22, 88, 229)),
            ChildBg = SetColor(new Vector4(0, 0, 0, 0)),
            PopupBg = SetColor(new Vector4(18, 14, 58, 235)),
            Border = SetColor(new Vector4(80, 60, 200, 113)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 0)),
            TitleBg = SetColor(new Vector4(22, 17, 72, 224)),
            TitleBgActive = SetColor(new Vector4(22, 17, 72, 224)),
            TitleBgCollapsed = SetColor(new Vector4(22, 17, 72, 224)),
            MenuBarBg = SetColor(new Vector4(18, 14, 55, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(35, 27, 105, 224)),
            FrameBgHovered = SetColor(new Vector4(75, 65, 140, 102)),
            FrameBgActive = SetColor(new Vector4(90, 78, 158, 171)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(55, 42, 155, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(80, 62, 190, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(100, 80, 220, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(80, 60, 200, 255)),
            SliderGrabActive = SetColor(new Vector4(100, 80, 220, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(55, 42, 165, 177)),
            ButtonHovered = SetColor(new Vector4(65, 50, 185, 201)),
            ButtonActive = SetColor(new Vector4(45, 34, 138, 241)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(80, 60, 200, 55)),
            HeaderHovered = SetColor(new Vector4(80, 60, 200, 110)),
            HeaderActive = SetColor(new Vector4(80, 60, 200, 165)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(55, 42, 138, 128)),
            SeparatorHovered = SetColor(new Vector4(230, 200, 60, 224)),
            SeparatorActive = SetColor(new Vector4(245, 215, 70, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(35, 27, 105, 224)),
            TabHovered = SetColor(new Vector4(18, 14, 58, 224)),
            TabActive = SetColor(new Vector4(30, 22, 95, 224)),
            TabUnfocused = SetColor(new Vector4(18, 14, 58, 224)),
            TabUnfocusedActive = SetColor(new Vector4(50, 38, 138, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(80, 60, 200, 105)),
            DockingEmptyBg = SetColor(new Vector4(16, 12, 50, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(22, 17, 68, 255)),
            TableBorderStrong = SetColor(new Vector4(55, 42, 138, 255)),
            TableBorderLight = SetColor(new Vector4(35, 27, 95, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(80, 60, 200, 64)),
            ResizeGripHovered = SetColor(new Vector4(80, 60, 200, 171)),
            ResizeGripActive = SetColor(new Vector4(30, 22, 95, 224)),
            PlotLines = SetColor(new Vector4(80, 200, 90, 255)),
            DragDropTarget = SetColor(new Vector4(230, 200, 60, 255)),
            NavHighlight = SetColor(new Vector4(80, 60, 200, 255)),
            NavWindowingDimBg = SetColor(new Vector4(45, 35, 120, 51)),
            NavWindowingHighlight = SetColor(new Vector4(200, 195, 240, 179)),
        },
    };

    public static Theme TransparentRed { get; } = new Theme
    {
        Name = "Mimiclot (Red)",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(224, 75, 75, 255)),
            AccentColorLight = SetColor(new Vector4(224, 75, 75, 255)),
            AccentColorStrong = SetColor(new Vector4(224, 75, 75, 255)),
            AccentColorDim = SetColor(new Vector4(224, 75, 75, 255)),
            AccentCheckMark = SetColor(new Vector4(219, 180, 180, 255)),
            AccentButtonHovered = SetColor(new Vector4(170, 29, 29, 201)),
            AccentTabActive = SetColor(new Vector4(58, 20, 20, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(108, 35, 35, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(255, 255, 255, 255)),
            TextDisabled = SetColor(new Vector4(128, 128, 128, 255)),
            TextSelectedBg = SetColor(new Vector4(200, 66, 66, 89)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(108, 29, 29, 229)),
            ChildBg = SetColor(new Vector4(0, 0, 0, 0)),
            PopupBg = SetColor(new Vector4(50, 17, 17, 235)),
            Border = SetColor(new Vector4(174, 17, 17, 113)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 0)),
            TitleBg = SetColor(new Vector4(100, 21, 21, 224)),
            TitleBgActive = SetColor(new Vector4(100, 21, 21, 224)),
            TitleBgCollapsed = SetColor(new Vector4(100, 21, 21, 224)),
            MenuBarBg = SetColor(new Vector4(46, 22, 22, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(96, 33, 33, 224)),
            FrameBgHovered = SetColor(new Vector4(142, 100, 100, 102)),
            FrameBgActive = SetColor(new Vector4(163, 120, 120, 171)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(148, 62, 62, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(199, 90, 90, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(180, 75, 75, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(185, 90, 90, 255)),
            SliderGrabActive = SetColor(new Vector4(171, 130, 130, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(171, 31, 31, 177)),
            ButtonHovered = SetColor(new Vector4(170, 29, 29, 201)),
            ButtonActive = SetColor(new Vector4(130, 24, 24, 241)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(255, 48, 48, 79)),
            HeaderHovered = SetColor(new Vector4(254, 88, 88, 204)),
            HeaderActive = SetColor(new Vector4(220, 78, 78, 226)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(128, 75, 75, 128)),
            SeparatorHovered = SetColor(new Vector4(74, 16, 16, 224)),
            SeparatorActive = SetColor(new Vector4(93, 20, 20, 241)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(96, 33, 33, 224)),
            TabHovered = SetColor(new Vector4(50, 17, 17, 224)),
            TabActive = SetColor(new Vector4(58, 20, 20, 224)),
            TabUnfocused = SetColor(new Vector4(50, 17, 17, 224)),
            TabUnfocusedActive = SetColor(new Vector4(108, 35, 35, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(208, 70, 70, 105)),
            DockingEmptyBg = SetColor(new Vector4(51, 22, 22, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(48, 20, 20, 255)),
            TableBorderStrong = SetColor(new Vector4(89, 40, 40, 255)),
            TableBorderLight = SetColor(new Vector4(64, 30, 30, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(201, 150, 150, 64)),
            ResizeGripHovered = SetColor(new Vector4(199, 150, 150, 171)),
            ResizeGripActive = SetColor(new Vector4(54, 11, 11, 224)),
            PlotLines = SetColor(new Vector4(180, 100, 100, 255)),
            DragDropTarget = SetColor(new Vector4(255, 50, 0, 230)),
            NavHighlight = SetColor(new Vector4(200, 66, 66, 255)),
            NavWindowingDimBg = SetColor(new Vector4(204, 180, 180, 51)),
            NavWindowingHighlight = SetColor(new Vector4(255, 200, 200, 179)),
        },
    };

    public static Theme TransparentRose { get; } = new Theme
    {
        Name = "Rose Pink (Transparent)",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(224, 75, 150, 255)),
            AccentColorLight = SetColor(new Vector4(224, 75, 150, 255)),
            AccentColorStrong = SetColor(new Vector4(224, 75, 150, 255)),
            AccentColorDim = SetColor(new Vector4(224, 75, 150, 255)),
            AccentCheckMark = SetColor(new Vector4(219, 180, 200, 255)),
            AccentButtonHovered = SetColor(new Vector4(170, 29, 100, 201)),
            AccentTabActive = SetColor(new Vector4(58, 20, 38, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(108, 35, 70, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(255, 255, 255, 255)),
            TextDisabled = SetColor(new Vector4(128, 128, 128, 255)),
            TextSelectedBg = SetColor(new Vector4(200, 66, 130, 89)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(108, 29, 65, 229)),
            ChildBg = SetColor(new Vector4(0, 0, 0, 0)),
            PopupBg = SetColor(new Vector4(50, 17, 30, 235)),
            Border = SetColor(new Vector4(174, 17, 90, 113)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 0)),
            TitleBg = SetColor(new Vector4(100, 21, 55, 224)),
            TitleBgActive = SetColor(new Vector4(100, 21, 55, 224)),
            TitleBgCollapsed = SetColor(new Vector4(100, 21, 55, 224)),
            MenuBarBg = SetColor(new Vector4(46, 22, 32, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(96, 33, 58, 224)),
            FrameBgHovered = SetColor(new Vector4(142, 100, 118, 102)),
            FrameBgActive = SetColor(new Vector4(163, 120, 135, 171)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(148, 62, 95, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(199, 90, 135, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(180, 75, 115, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(185, 90, 130, 255)),
            SliderGrabActive = SetColor(new Vector4(171, 130, 150, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(171, 31, 90, 177)),
            ButtonHovered = SetColor(new Vector4(170, 29, 100, 201)),
            ButtonActive = SetColor(new Vector4(130, 24, 70, 241)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(255, 48, 130, 79)),
            HeaderHovered = SetColor(new Vector4(254, 88, 155, 204)),
            HeaderActive = SetColor(new Vector4(220, 78, 135, 226)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(128, 75, 100, 128)),
            SeparatorHovered = SetColor(new Vector4(74, 16, 40, 224)),
            SeparatorActive = SetColor(new Vector4(93, 20, 55, 241)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(96, 33, 58, 224)),
            TabHovered = SetColor(new Vector4(50, 17, 30, 224)),
            TabActive = SetColor(new Vector4(58, 20, 38, 224)),
            TabUnfocused = SetColor(new Vector4(50, 17, 30, 224)),
            TabUnfocusedActive = SetColor(new Vector4(108, 35, 70, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(208, 70, 130, 105)),
            DockingEmptyBg = SetColor(new Vector4(51, 22, 35, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(48, 20, 32, 255)),
            TableBorderStrong = SetColor(new Vector4(89, 40, 60, 255)),
            TableBorderLight = SetColor(new Vector4(64, 30, 45, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(201, 150, 175, 64)),
            ResizeGripHovered = SetColor(new Vector4(199, 150, 170, 171)),
            ResizeGripActive = SetColor(new Vector4(54, 11, 30, 224)),
            PlotLines = SetColor(new Vector4(180, 100, 140, 255)),
            DragDropTarget = SetColor(new Vector4(255, 50, 130, 230)),
            NavHighlight = SetColor(new Vector4(200, 66, 130, 255)),
            NavWindowingDimBg = SetColor(new Vector4(204, 180, 190, 51)),
            NavWindowingHighlight = SetColor(new Vector4(255, 200, 220, 179)),
        },
    };

    public static Theme TransparentTeal { get; } = new Theme
    {
        Name = "Tide Blue (Transparent)",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(75, 210, 200, 255)),
            AccentColorLight = SetColor(new Vector4(75, 210, 200, 255)),
            AccentColorStrong = SetColor(new Vector4(75, 210, 200, 255)),
            AccentColorDim = SetColor(new Vector4(75, 210, 200, 255)),
            AccentCheckMark = SetColor(new Vector4(180, 219, 215, 255)),
            AccentButtonHovered = SetColor(new Vector4(29, 155, 148, 201)),
            AccentTabActive = SetColor(new Vector4(20, 55, 52, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(35, 100, 95, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(255, 255, 255, 255)),
            TextDisabled = SetColor(new Vector4(128, 128, 128, 255)),
            TextSelectedBg = SetColor(new Vector4(66, 185, 178, 89)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(18, 80, 78, 229)),
            ChildBg = SetColor(new Vector4(0, 0, 0, 0)),
            PopupBg = SetColor(new Vector4(12, 45, 44, 235)),
            Border = SetColor(new Vector4(17, 150, 144, 113)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 0)),
            TitleBg = SetColor(new Vector4(14, 68, 65, 224)),
            TitleBgActive = SetColor(new Vector4(14, 68, 65, 224)),
            TitleBgCollapsed = SetColor(new Vector4(14, 68, 65, 224)),
            MenuBarBg = SetColor(new Vector4(16, 42, 40, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(22, 78, 75, 224)),
            FrameBgHovered = SetColor(new Vector4(80, 135, 130, 102)),
            FrameBgActive = SetColor(new Vector4(100, 155, 150, 171)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(35, 120, 115, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(55, 165, 158, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(45, 142, 136, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(55, 165, 158, 255)),
            SliderGrabActive = SetColor(new Vector4(85, 185, 178, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(22, 140, 134, 177)),
            ButtonHovered = SetColor(new Vector4(29, 155, 148, 201)),
            ButtonActive = SetColor(new Vector4(20, 115, 110, 241)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(48, 210, 200, 79)),
            HeaderHovered = SetColor(new Vector4(88, 220, 210, 204)),
            HeaderActive = SetColor(new Vector4(68, 195, 186, 226)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(50, 128, 122, 128)),
            SeparatorHovered = SetColor(new Vector4(16, 68, 65, 224)),
            SeparatorActive = SetColor(new Vector4(20, 88, 84, 241)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(22, 78, 75, 224)),
            TabHovered = SetColor(new Vector4(12, 45, 44, 224)),
            TabActive = SetColor(new Vector4(20, 55, 52, 224)),
            TabUnfocused = SetColor(new Vector4(12, 45, 44, 224)),
            TabUnfocusedActive = SetColor(new Vector4(35, 100, 95, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(70, 195, 186, 105)),
            DockingEmptyBg = SetColor(new Vector4(14, 45, 43, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(18, 50, 48, 255)),
            TableBorderStrong = SetColor(new Vector4(38, 89, 85, 255)),
            TableBorderLight = SetColor(new Vector4(26, 65, 62, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(150, 201, 195, 64)),
            ResizeGripHovered = SetColor(new Vector4(150, 199, 193, 171)),
            ResizeGripActive = SetColor(new Vector4(11, 54, 52, 224)),
            PlotLines = SetColor(new Vector4(100, 185, 178, 255)),
            DragDropTarget = SetColor(new Vector4(0, 210, 200, 230)),
            NavHighlight = SetColor(new Vector4(66, 200, 192, 255)),
            NavWindowingDimBg = SetColor(new Vector4(180, 204, 200, 51)),
            NavWindowingHighlight = SetColor(new Vector4(200, 255, 250, 179)),
        },
    };

    public static Theme TransparentGreen { get; } = new Theme
    {
        Name = "Mimiclot (Green)",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(75, 224, 98, 255)),
            AccentColorLight = SetColor(new Vector4(75, 224, 98, 255)),
            AccentColorStrong = SetColor(new Vector4(75, 224, 98, 255)),
            AccentColorDim = SetColor(new Vector4(75, 224, 98, 255)),
            AccentCheckMark = SetColor(new Vector4(180, 219, 180, 255)),
            AccentButtonHovered = SetColor(new Vector4(29, 140, 60, 201)),
            AccentTabActive = SetColor(new Vector4(20, 58, 20, 224)),
            AccentTabUnfocusedActive = SetColor(new Vector4(35, 108, 48, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(255, 255, 255, 255)),
            TextDisabled = SetColor(new Vector4(128, 128, 128, 255)),
            TextSelectedBg = SetColor(new Vector4(66, 200, 100, 89)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(29, 108, 48, 229)),
            ChildBg = SetColor(new Vector4(0, 0, 0, 0)),
            PopupBg = SetColor(new Vector4(17, 50, 22, 235)),
            Border = SetColor(new Vector4(17, 174, 60, 113)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 0)),
            TitleBg = SetColor(new Vector4(21, 100, 35, 224)),
            TitleBgActive = SetColor(new Vector4(21, 100, 35, 224)),
            TitleBgCollapsed = SetColor(new Vector4(21, 100, 35, 224)),
            MenuBarBg = SetColor(new Vector4(22, 46, 22, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(33, 96, 44, 224)),
            FrameBgHovered = SetColor(new Vector4(100, 142, 100, 102)),
            FrameBgActive = SetColor(new Vector4(120, 163, 120, 171)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(62, 148, 78, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(90, 199, 110, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(75, 180, 95, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(90, 185, 107, 255)),
            SliderGrabActive = SetColor(new Vector4(130, 171, 130, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(31, 171, 63, 177)),
            ButtonHovered = SetColor(new Vector4(29, 140, 60, 201)),
            ButtonActive = SetColor(new Vector4(24, 110, 48, 241)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(48, 255, 90, 79)),
            HeaderHovered = SetColor(new Vector4(88, 254, 121, 204)),
            HeaderActive = SetColor(new Vector4(78, 200, 110, 226)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(75, 128, 75, 128)),
            SeparatorHovered = SetColor(new Vector4(16, 74, 22, 224)),
            SeparatorActive = SetColor(new Vector4(20, 93, 20, 241)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(33, 96, 44, 224)),
            TabHovered = SetColor(new Vector4(17, 50, 22, 224)),
            TabActive = SetColor(new Vector4(20, 58, 20, 224)),
            TabUnfocused = SetColor(new Vector4(17, 50, 22, 224)),
            TabUnfocusedActive = SetColor(new Vector4(35, 108, 48, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(70, 208, 91, 105)),
            DockingEmptyBg = SetColor(new Vector4(22, 51, 22, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(30, 48, 30, 255)),
            TableBorderStrong = SetColor(new Vector4(50, 89, 50, 255)),
            TableBorderLight = SetColor(new Vector4(38, 64, 38, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 15)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(150, 201, 150, 64)),
            ResizeGripHovered = SetColor(new Vector4(150, 199, 150, 171)),
            ResizeGripActive = SetColor(new Vector4(11, 54, 18, 224)),
            PlotLines = SetColor(new Vector4(120, 180, 120, 255)),
            DragDropTarget = SetColor(new Vector4(0, 255, 50, 230)),
            NavHighlight = SetColor(new Vector4(66, 200, 100, 255)),
            NavWindowingDimBg = SetColor(new Vector4(180, 204, 180, 51)),
            NavWindowingHighlight = SetColor(new Vector4(200, 255, 200, 179)),
        },
    };

    public static Theme PineappleAdventure { get; } = new Theme
    {
        Name = "Pineapple Adventure",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(185, 105, 55, 255)),
            AccentColorLight = SetColor(new Vector4(205, 125, 70, 255)),
            AccentColorStrong = SetColor(new Vector4(160, 85, 40, 255)),
            AccentColorDim = SetColor(new Vector4(130, 68, 30, 255)),
            AccentCheckMark = SetColor(new Vector4(215, 175, 55, 255)),
            AccentButtonHovered = SetColor(new Vector4(215, 175, 55, 255)),
            AccentTabActive = SetColor(new Vector4(185, 105, 55, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(150, 82, 40, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(235, 220, 185, 255)),
            TextDisabled = SetColor(new Vector4(120, 100, 65, 255)),
            TextSelectedBg = SetColor(new Vector4(215, 175, 55, 130)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(18, 14, 9, 252)),
            ChildBg = SetColor(new Vector4(24, 18, 11, 120)),
            PopupBg = SetColor(new Vector4(16, 12, 8, 254)),
            Border = SetColor(new Vector4(215, 175, 55, 170)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 100)),
            TitleBg = SetColor(new Vector4(14, 10, 6, 248)),
            TitleBgActive = SetColor(new Vector4(22, 16, 10, 255)),
            TitleBgCollapsed = SetColor(new Vector4(18, 13, 8, 255)),
            MenuBarBg = SetColor(new Vector4(24, 18, 11, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(38, 28, 16, 255)),
            FrameBgHovered = SetColor(new Vector4(215, 175, 55, 55)),
            FrameBgActive = SetColor(new Vector4(215, 175, 55, 90)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(155, 88, 38, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(190, 115, 50, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(215, 175, 55, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(215, 175, 55, 255)),
            SliderGrabActive = SetColor(new Vector4(235, 200, 80, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(185, 105, 55, 180)),
            ButtonHovered = SetColor(new Vector4(215, 175, 55, 235)),
            ButtonActive = SetColor(new Vector4(175, 148, 40, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(185, 105, 55, 50)),
            HeaderHovered = SetColor(new Vector4(215, 175, 55, 90)),
            HeaderActive = SetColor(new Vector4(215, 175, 55, 135)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(185, 105, 55, 150)),
            SeparatorHovered = SetColor(new Vector4(215, 175, 55, 220)),
            SeparatorActive = SetColor(new Vector4(235, 200, 80, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(30, 22, 12, 255)),
            TabHovered = SetColor(new Vector4(45, 165, 145, 120)),
            TabActive = SetColor(new Vector4(185, 105, 55, 255)),
            TabUnfocused = SetColor(new Vector4(24, 18, 10, 255)),
            TabUnfocusedActive = SetColor(new Vector4(150, 82, 40, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(45, 165, 145, 140)),
            DockingEmptyBg = SetColor(new Vector4(14, 10, 6, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(30, 22, 12, 255)),
            TableBorderStrong = SetColor(new Vector4(185, 105, 55, 255)),
            TableBorderLight = SetColor(new Vector4(120, 68, 30, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(215, 175, 55, 16)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(215, 175, 55, 255)),
            PlotLines = SetColor(new Vector4(45, 165, 145, 255)),
            DragDropTarget = SetColor(new Vector4(215, 175, 55, 255)),
            NavHighlight = SetColor(new Vector4(45, 165, 145, 200)),
            NavWindowingDimBg = SetColor(new Vector4(45, 165, 145, 50)),
            NavWindowingHighlight = SetColor(new Vector4(235, 210, 130, 100)),
        },
    };

    public static Theme MonokaiDark { get; } = new Theme
    {
        Name = "Kai Dark M2",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(102, 217, 239, 255)),
            AccentColorLight = SetColor(new Vector4(102, 217, 239, 255)),
            AccentColorStrong = SetColor(new Vector4(102, 217, 239, 255)),
            AccentColorDim = SetColor(new Vector4(72, 158, 175, 255)),
            AccentCheckMark = SetColor(new Vector4(255, 216, 102, 255)),
            AccentButtonHovered = SetColor(new Vector4(169, 220, 118, 255)),
            AccentTabActive = SetColor(new Vector4(255, 97, 136, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(180, 65, 95, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(252, 252, 250, 255)),
            TextDisabled = SetColor(new Vector4(115, 113, 105, 255)),
            TextSelectedBg = SetColor(new Vector4(102, 217, 239, 80)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(25, 24, 22, 252)),
            ChildBg = SetColor(new Vector4(30, 28, 26, 100)),
            PopupBg = SetColor(new Vector4(22, 21, 19, 252)),
            Border = SetColor(new Vector4(55, 53, 48, 200)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 80)),
            TitleBg = SetColor(new Vector4(19, 18, 16, 245)),
            TitleBgActive = SetColor(new Vector4(28, 26, 24, 255)),
            TitleBgCollapsed = SetColor(new Vector4(22, 21, 19, 255)),
            MenuBarBg = SetColor(new Vector4(33, 31, 28, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(40, 38, 35, 255)),
            FrameBgHovered = SetColor(new Vector4(55, 53, 48, 255)),
            FrameBgActive = SetColor(new Vector4(48, 46, 42, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(60, 58, 53, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(80, 77, 70, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(100, 97, 88, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(102, 217, 239, 200)),
            SliderGrabActive = SetColor(new Vector4(102, 217, 239, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(55, 53, 48, 200)),
            ButtonHovered = SetColor(new Vector4(169, 220, 118, 180)),
            ButtonActive = SetColor(new Vector4(130, 170, 90, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(0, 0, 0, 60)),
            HeaderHovered = SetColor(new Vector4(102, 217, 239, 50)),
            HeaderActive = SetColor(new Vector4(102, 217, 239, 90)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(55, 53, 48, 180)),
            SeparatorHovered = SetColor(new Vector4(120, 220, 232, 180)),
            SeparatorActive = SetColor(new Vector4(120, 220, 232, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(35, 33, 30, 255)),
            TabHovered = SetColor(new Vector4(55, 53, 48, 255)),
            TabActive = SetColor(new Vector4(255, 97, 136, 255)),
            TabUnfocused = SetColor(new Vector4(30, 28, 26, 255)),
            TabUnfocusedActive = SetColor(new Vector4(180, 65, 95, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(102, 217, 239, 105)),
            DockingEmptyBg = SetColor(new Vector4(19, 18, 16, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(38, 36, 33, 255)),
            TableBorderStrong = SetColor(new Vector4(65, 63, 57, 255)),
            TableBorderLight = SetColor(new Vector4(48, 46, 42, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 8)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(102, 217, 239, 255)),
            PlotLines = SetColor(new Vector4(120, 220, 232, 255)),
            DragDropTarget = SetColor(new Vector4(102, 217, 239, 230)),
            NavHighlight = SetColor(new Vector4(102, 217, 239, 179)),
            NavWindowingDimBg = SetColor(new Vector4(100, 98, 90, 51)),
            NavWindowingHighlight = SetColor(new Vector4(252, 252, 250, 89)),
        },
    };

    public static Theme MonokaiClassic { get; } = new Theme
    {
        Name = "Kai Classic F",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(102, 217, 239, 255)),
            AccentColorLight = SetColor(new Vector4(130, 230, 245, 255)),
            AccentColorStrong = SetColor(new Vector4(80, 190, 215, 255)),
            AccentColorDim = SetColor(new Vector4(60, 160, 185, 255)),
            AccentCheckMark = SetColor(new Vector4(230, 219, 116, 255)),
            AccentButtonHovered = SetColor(new Vector4(102, 217, 239, 255)),
            AccentTabActive = SetColor(new Vector4(249, 38, 114, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(180, 168, 70, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(248, 248, 242, 255)),
            TextDisabled = SetColor(new Vector4(117, 113, 94, 255)),
            TextSelectedBg = SetColor(new Vector4(102, 217, 239, 110)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(39, 40, 34, 252)),
            ChildBg = SetColor(new Vector4(45, 46, 39, 120)),
            PopupBg = SetColor(new Vector4(35, 36, 31, 254)),
            Border = SetColor(new Vector4(117, 113, 94, 160)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 100)),
            TitleBg = SetColor(new Vector4(30, 31, 26, 248)),
            TitleBgActive = SetColor(new Vector4(45, 46, 39, 255)),
            TitleBgCollapsed = SetColor(new Vector4(35, 36, 31, 255)),
            MenuBarBg = SetColor(new Vector4(48, 49, 41, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(55, 56, 48, 255)),
            FrameBgHovered = SetColor(new Vector4(73, 72, 62, 255)),
            FrameBgActive = SetColor(new Vector4(83, 82, 70, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(73, 72, 62, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(102, 217, 239, 180)),
            ScrollbarGrabActive = SetColor(new Vector4(102, 217, 239, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(102, 217, 239, 255)),
            SliderGrabActive = SetColor(new Vector4(130, 230, 245, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(55, 56, 48, 220)),
            ButtonHovered = SetColor(new Vector4(102, 217, 239, 200)),
            ButtonActive = SetColor(new Vector4(72, 158, 175, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(73, 72, 62, 120)),
            HeaderHovered = SetColor(new Vector4(166, 226, 46, 70)),
            HeaderActive = SetColor(new Vector4(166, 226, 46, 120)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(73, 72, 62, 200)),
            SeparatorHovered = SetColor(new Vector4(102, 217, 239, 200)),
            SeparatorActive = SetColor(new Vector4(249, 38, 114, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(45, 46, 39, 255)),
            TabHovered = SetColor(new Vector4(73, 72, 62, 255)),
            TabActive = SetColor(new Vector4(249, 38, 114, 255)),
            TabUnfocused = SetColor(new Vector4(39, 40, 34, 255)),
            TabUnfocusedActive = SetColor(new Vector4(180, 20, 80, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(102, 217, 239, 120)),
            DockingEmptyBg = SetColor(new Vector4(30, 31, 26, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(48, 49, 41, 255)),
            TableBorderStrong = SetColor(new Vector4(73, 72, 62, 255)),
            TableBorderLight = SetColor(new Vector4(55, 56, 48, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(255, 255, 255, 8)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(102, 217, 239, 255)),
            PlotLines = SetColor(new Vector4(102, 217, 239, 255)),
            DragDropTarget = SetColor(new Vector4(102, 217, 239, 255)),
            NavHighlight = SetColor(new Vector4(102, 217, 239, 200)),
            NavWindowingDimBg = SetColor(new Vector4(39, 40, 34, 120)),
            NavWindowingHighlight = SetColor(new Vector4(248, 248, 242, 89)),
        },
    };

    public static Theme MonokaiLight { get; } = new Theme
    {
        Name = "Kai Light G77",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(200, 155, 20, 255)),
            AccentColorLight = SetColor(new Vector4(220, 175, 40, 255)),
            AccentColorStrong = SetColor(new Vector4(180, 135, 10, 255)),
            AccentColorDim = SetColor(new Vector4(150, 110, 5, 255)),
            AccentCheckMark = SetColor(new Vector4(200, 155, 20, 255)),
            AccentButtonHovered = SetColor(new Vector4(100, 160, 55, 255)),
            AccentTabActive = SetColor(new Vector4(210, 40, 90, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(160, 25, 65, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(35, 33, 28, 255)),
            TextDisabled = SetColor(new Vector4(155, 150, 135, 255)),
            TextSelectedBg = SetColor(new Vector4(200, 155, 20, 100)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(245, 243, 238, 255)),
            ChildBg = SetColor(new Vector4(238, 235, 228, 120)),
            PopupBg = SetColor(new Vector4(250, 248, 244, 255)),
            Border = SetColor(new Vector4(185, 180, 165, 200)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 20)),
            TitleBg = SetColor(new Vector4(225, 222, 214, 255)),
            TitleBgActive = SetColor(new Vector4(215, 211, 202, 255)),
            TitleBgCollapsed = SetColor(new Vector4(220, 217, 208, 255)),
            MenuBarBg = SetColor(new Vector4(230, 227, 218, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(215, 211, 202, 255)),
            FrameBgHovered = SetColor(new Vector4(195, 190, 178, 255)),
            FrameBgActive = SetColor(new Vector4(180, 175, 162, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(185, 180, 165, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(155, 150, 135, 255)),
            ScrollbarGrabActive = SetColor(new Vector4(125, 120, 108, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(200, 155, 20, 255)),
            SliderGrabActive = SetColor(new Vector4(220, 175, 40, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(205, 201, 190, 255)),
            ButtonHovered = SetColor(new Vector4(100, 160, 55, 220)),
            ButtonActive = SetColor(new Vector4(80, 130, 42, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(200, 155, 20, 45)),
            HeaderHovered = SetColor(new Vector4(200, 155, 20, 85)),
            HeaderActive = SetColor(new Vector4(200, 155, 20, 130)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(185, 180, 165, 200)),
            SeparatorHovered = SetColor(new Vector4(60, 175, 190, 200)),
            SeparatorActive = SetColor(new Vector4(40, 155, 170, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(220, 217, 208, 255)),
            TabHovered = SetColor(new Vector4(195, 190, 178, 255)),
            TabActive = SetColor(new Vector4(210, 40, 90, 255)),
            TabUnfocused = SetColor(new Vector4(228, 225, 217, 255)),
            TabUnfocusedActive = SetColor(new Vector4(160, 25, 65, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(200, 155, 20, 120)),
            DockingEmptyBg = SetColor(new Vector4(230, 227, 218, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(210, 206, 196, 255)),
            TableBorderStrong = SetColor(new Vector4(165, 160, 145, 255)),
            TableBorderLight = SetColor(new Vector4(195, 191, 180, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(0, 0, 0, 12)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(200, 155, 20, 255)),
            PlotLines = SetColor(new Vector4(40, 155, 170, 255)),
            DragDropTarget = SetColor(new Vector4(200, 155, 20, 230)),
            NavHighlight = SetColor(new Vector4(200, 155, 20, 179)),
            NavWindowingDimBg = SetColor(new Vector4(155, 150, 135, 60)),
            NavWindowingHighlight = SetColor(new Vector4(35, 33, 28, 89)),
        },
    };

    public static Theme MonokaiClassicLight { get; } = new Theme
    {
        Name = "Kai Classic Light M",
        Accent = new ThemeAccent
        {
            AccentColor = SetColor(new Vector4(175, 162, 45, 255)),
            AccentColorLight = SetColor(new Vector4(195, 182, 65, 255)),
            AccentColorStrong = SetColor(new Vector4(155, 142, 28, 255)),
            AccentColorDim = SetColor(new Vector4(130, 118, 15, 255)),
            AccentCheckMark = SetColor(new Vector4(175, 162, 45, 255)),
            AccentButtonHovered = SetColor(new Vector4(40, 155, 170, 255)),
            AccentTabActive = SetColor(new Vector4(200, 20, 85, 255)),
            AccentTabUnfocusedActive = SetColor(new Vector4(150, 10, 60, 255)),
        },
        Text = new ThemeText
        {
            Text = SetColor(new Vector4(40, 40, 33, 255)),
            TextDisabled = SetColor(new Vector4(158, 154, 132, 255)),
            TextSelectedBg = SetColor(new Vector4(175, 162, 45, 110)),
        },
        Window = new ThemeWindow
        {
            WindowBg = SetColor(new Vector4(242, 242, 234, 255)),
            ChildBg = SetColor(new Vector4(234, 234, 225, 120)),
            PopupBg = SetColor(new Vector4(248, 248, 240, 255)),
            Border = SetColor(new Vector4(175, 172, 148, 180)),
            BorderShadow = SetColor(new Vector4(0, 0, 0, 20)),
            TitleBg = SetColor(new Vector4(220, 220, 210, 255)),
            TitleBgActive = SetColor(new Vector4(210, 210, 198, 255)),
            TitleBgCollapsed = SetColor(new Vector4(215, 215, 204, 255)),
            MenuBarBg = SetColor(new Vector4(225, 225, 214, 255)),
        },
        Frame = new ThemeFrame
        {
            FrameBg = SetColor(new Vector4(210, 210, 198, 255)),
            FrameBgHovered = SetColor(new Vector4(188, 188, 174, 255)),
            FrameBgActive = SetColor(new Vector4(172, 172, 158, 255)),
        },
        Scrollbar = new ThemeScrollbar
        {
            ScrollbarBg = SetColor(new Vector4(0, 0, 0, 0)),
            ScrollbarGrab = SetColor(new Vector4(175, 172, 148, 255)),
            ScrollbarGrabHovered = SetColor(new Vector4(40, 155, 170, 180)),
            ScrollbarGrabActive = SetColor(new Vector4(30, 130, 145, 255)),
        },
        Slider = new ThemeSlider
        {
            SliderGrab = SetColor(new Vector4(175, 162, 45, 255)),
            SliderGrabActive = SetColor(new Vector4(195, 182, 65, 255)),
        },
        Button = new ThemeButton
        {
            Button = SetColor(new Vector4(205, 205, 193, 255)),
            ButtonHovered = SetColor(new Vector4(40, 155, 170, 200)),
            ButtonActive = SetColor(new Vector4(28, 120, 134, 255)),
        },
        Header = new ThemeHeader
        {
            Header = SetColor(new Vector4(175, 172, 148, 100)),
            HeaderHovered = SetColor(new Vector4(110, 175, 20, 80)),
            HeaderActive = SetColor(new Vector4(110, 175, 20, 130)),
        },
        Separator = new ThemeSeparator
        {
            Separator = SetColor(new Vector4(175, 172, 148, 200)),
            SeparatorHovered = SetColor(new Vector4(175, 162, 45, 200)),
            SeparatorActive = SetColor(new Vector4(200, 20, 85, 255)),
        },
        Tab = new ThemeTab
        {
            Tab = SetColor(new Vector4(215, 215, 204, 255)),
            TabHovered = SetColor(new Vector4(188, 188, 174, 255)),
            TabActive = SetColor(new Vector4(200, 20, 85, 255)),
            TabUnfocused = SetColor(new Vector4(225, 225, 214, 255)),
            TabUnfocusedActive = SetColor(new Vector4(150, 10, 60, 255)),
        },
        Docking = new ThemeDocking
        {
            DockingPreview = SetColor(new Vector4(110, 175, 20, 120)),
            DockingEmptyBg = SetColor(new Vector4(225, 225, 214, 255)),
        },
        Table = new ThemeTable
        {
            TableHeaderBg = SetColor(new Vector4(205, 205, 193, 255)),
            TableBorderStrong = SetColor(new Vector4(175, 172, 148, 255)),
            TableBorderLight = SetColor(new Vector4(200, 200, 188, 255)),
            TableRowBg = SetColor(new Vector4(0, 0, 0, 0)),
            TableRowBgAlt = SetColor(new Vector4(0, 0, 0, 12)),
        },
        Misc = new ThemeMisc
        {
            ResizeGrip = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripHovered = SetColor(new Vector4(0, 0, 0, 0)),
            ResizeGripActive = SetColor(new Vector4(175, 162, 45, 255)),
            PlotLines = SetColor(new Vector4(40, 155, 170, 255)),
            DragDropTarget = SetColor(new Vector4(175, 162, 45, 255)),
            NavHighlight = SetColor(new Vector4(175, 162, 45, 200)),
            NavWindowingDimBg = SetColor(new Vector4(155, 150, 130, 60)),
            NavWindowingHighlight = SetColor(new Vector4(40, 40, 33, 89)),
        },
    };

    public static Theme[] Themes { get; } =
    [
        BrioDark,
        BrioLight,
        ArchonBlue,
        Midnight,
        ForestAuraWarm,
        PineappleAdventure,
        MonokaiDark,
        MonokaiLight,
        MonokaiClassic,
        MonokaiClassicLight,
        Slate,
        Sepia,
        CubeOrange,
        CubeIndigo,
        TransparentRed,
        TransparentGreen,
        TransparentTeal,
        TransparentRose,
    ];

    public static void SetThemeByName(string name)
    {
        foreach(var theme in Themes)
        {
            if(theme.Name == name)
            {
                CurrentTheme = theme;
                return;
            }
        }
    }

    static uint SetColor(Vector4 colorVector)
    {
        uint r = (uint)colorVector.X & 0xFF;
        uint g = (uint)colorVector.Y & 0xFF;
        uint b = (uint)colorVector.Z & 0xFF;
        uint a = (uint)colorVector.W & 0xFF;

        return (a << 24) | (b << 16) | (g << 8) | r;
    }
}

public record class Theme
{
    public required string Name;

    public required ThemeAccent Accent;
    public required ThemeText Text;
    public required ThemeWindow Window;
    public required ThemeFrame Frame;
    public required ThemeScrollbar Scrollbar;
    public required ThemeSlider Slider;
    public required ThemeButton Button;
    public required ThemeHeader Header;
    public required ThemeSeparator Separator;
    public required ThemeTab Tab;
    public required ThemeDocking Docking;
    public required ThemeTable Table;
    public required ThemeMisc Misc;
}

public record class ThemeAccent
{
    public uint AccentColor;
    public uint AccentColorLight;
    public uint AccentColorStrong;
    public uint AccentColorDim;
    public uint AccentCheckMark;
    public uint AccentButtonHovered;
    public uint AccentTabActive;
    public uint AccentTabUnfocusedActive;
}

public record class ThemeText
{
    public uint Text;
    public uint TextDisabled;
    public uint TextSelectedBg;
}

public record class ThemeWindow
{
    public uint WindowBg;
    public uint ChildBg;
    public uint PopupBg;
    public uint Border;
    public uint BorderShadow;
    public uint TitleBg;
    public uint TitleBgActive;
    public uint TitleBgCollapsed;
    public uint MenuBarBg;
}

public record class ThemeFrame
{
    public uint FrameBg;
    public uint FrameBgHovered;
    public uint FrameBgActive;
}

public record class ThemeScrollbar
{
    public uint ScrollbarBg;
    public uint ScrollbarGrab;
    public uint ScrollbarGrabHovered;
    public uint ScrollbarGrabActive;
}

public record class ThemeSlider
{
    public uint SliderGrab;
    public uint SliderGrabActive;
}

public record class ThemeButton
{
    public uint Button;
    public uint ButtonHovered;
    public uint ButtonActive;
}

public record class ThemeHeader
{
    public uint Header;
    public uint HeaderHovered;
    public uint HeaderActive;
}

public record class ThemeSeparator
{
    public uint Separator;
    public uint SeparatorHovered;
    public uint SeparatorActive;
}

public record class ThemeTab
{
    public uint Tab;
    public uint TabHovered;
    public uint TabActive;
    public uint TabUnfocused;
    public uint TabUnfocusedActive;
}

public record class ThemeDocking
{
    public uint DockingPreview;
    public uint DockingEmptyBg;
}

public record class ThemeTable
{
    public uint TableHeaderBg;
    public uint TableBorderStrong;
    public uint TableBorderLight;
    public uint TableRowBg;
    public uint TableRowBgAlt;
}

public record class ThemeMisc
{
    public uint ResizeGrip;
    public uint ResizeGripHovered;
    public uint ResizeGripActive;
    public uint PlotLines;
    public uint DragDropTarget;
    public uint NavHighlight;
    public uint NavWindowingDimBg;
    public uint NavWindowingHighlight;
}
