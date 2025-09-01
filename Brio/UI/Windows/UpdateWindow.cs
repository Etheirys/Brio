using Brio.Config;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace Brio.UI.Windows;

public class UpdateWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly List<string> _changelogTest = [];

    private float _closeButtonWidth => 310f * ImGuiHelpers.GlobalScale;
   
    //
    // Some code found here is inspired by CharacterSelect+
    //

    // Welcome to {Brio.Name} version [{configurationService.Version}] 
    public UpdateWindow(ConfigurationService configurationService) : base($"###brio_welcomewindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDecoration)
    {
        Namespace = "brio_welcomewindow_namespace";

        _configurationService = configurationService;
   
        Size = new Vector2(710, 745);

        ShowCloseButton = false;
        AllowClickthrough = false;
        AllowPinning = false;

        var logStream = ResourceProvider.Instance.GetRawResourceStream("Data.Changelog.txt");

        using var streamReader = new StreamReader(logStream, Encoding.UTF8, true, 128);
        string? line;
        while((line = streamReader.ReadLine()) is not null)
        {
            _changelogTest.Add(line);
        }
    }

    bool _scrollToTop = false;
    public override void OnOpen()
    {
        _scrollToTop = true;
    }

    public override void Draw()
    {
        var windowPos = ImGui.GetWindowPos();
        var windowPadding = ImGui.GetStyle().WindowPadding;

        var headerWidth = 1000f - (windowPadding.X * 2) * ImGuiHelpers.GlobalScale;
        var headerHeight = 500f * ImGuiHelpers.GlobalScale;

        var headerStart = windowPos - new Vector2(0, 0);

        //

        var image = ResourceProvider.Instance.GetResourceImage($"Images.Update.brio-artbk-aug-25-01.png");

        // Calculate scaling to fill width and maintain aspect ratio
        var imageAspect = (float)image.Width / image.Height;
        var scaledWidth = headerWidth / 1.4f * ImGuiHelpers.GlobalScale;
        var scaledHeight = scaledWidth / imageAspect;

        var imagePos = headerStart;

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddImage(image.Handle, imagePos, imagePos + new Vector2(scaledWidth, scaledHeight));

        headerStart = new Vector2(headerStart.X, headerStart.Y + scaledHeight);
        var headerEnd = headerStart + new Vector2(headerWidth, headerHeight);

        DrawBackground(headerStart, headerEnd);

        ImGui.SetCursorScreenPos(headerStart);

        //

        var segmentSize = ImGui.GetWindowSize().X / 4.15f;

        var buttonSize = new Vector2(segmentSize, ImGui.GetTextLineHeight() * 1.7f);

        ImGui.Separator();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 224, 148, 200) / 255);
        if(ImGui.Button("Support on KoFi", buttonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/minmoosexiv", UseShellExecute = true });
        ImGui.PopStyleColor();
        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(65, 90, 240, 200) / 255);
        if(ImGui.Button("Brio Community Discord", buttonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/GCb4srgEaH ", UseShellExecute = true });
        ImGui.PopStyleColor();
        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(96, 108, 246, 200) / 255);
        if(ImGui.Button("Aetherworks Discord", buttonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/KvGJCCnG8t", UseShellExecute = true });
        ImGui.PopStyleColor();
        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(29, 161, 242, 200) / 255);
        if(ImGui.Button("More Links", buttonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://etheirystools.carrd.co", UseShellExecute = true });
        ImGui.PopStyleColor();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
   
        ImGui.TextColored(new Vector4(0.4f, 0.9f, 0.4f, 1.0f), $"Whats New in Brio v0.6.0");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.75f, 0.75f, 0.85f, 1.0f), $"  -  MCDFs, Dynamic Face Control, & ?????");

        using(ImRaii.PushColor(ImGuiCol.ChildBg, 0))
        using(var c = ImRaii.Child("###brio_update_text", new Vector2(ImGui.GetWindowHeight() - 55 * ImGuiHelpers.GlobalScale, ImBrio.GetRemainingHeight() - 35f), false, Flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            if(c.Success)
            {
                ImGui.Text("To open this window again click on the `Information` button on the Scene Manager!");

                if(_scrollToTop)
                {
                    _scrollToTop = false;
                    ImGui.SetScrollHereY(0);
                }

                ImBrio.VerticalPadding(10);

                if(CollapsingHeader(" v0.6.0 – September ?? 2025", "  -  MCDFs, Dynamic Face Control, & ????? ", new Vector4(0.5f, 0.9f, 0.5f, 1.0f), true))
                {
                    ImGui.BulletText("Lorem ipsum dolor sit amet, consectetur adipiscing elit.");
                    ImGui.BulletText("Nullam vitae vulputate felis, in eleifend mi. Integer sollicitudin et turpis vel");
                    ImGui.BulletText("Nulla turpis odio, sodales efficitur convallis a, volutpat vel augue.");
                    ImGui.BulletText("Cras ac dictum eros, at tristique nisi. Cras at dictum leo. Morbi");
                    ImGui.BulletText("vestibulum a tellus. Vestibulum lacus ante, fringilla non nulla id, mattis placerat nisl.");
                }
               
                if(CollapsingHeader(" v0.5.3 – August 30 2025", "  -  That damned boat ", new Vector4(0.75f, 0.75f, 0.85f, 1.0f), false))
                {
                    DrawFeature(FontAwesomeIcon.None, "0.5.3.1", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

                    ImGui.BulletText("- Fix being able to select things in the Scene Manager (AHAHH)");

                    DrawFeature(FontAwesomeIcon.None, "0.5.3", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

                    ImGui.BulletText("Added Moonfire Faire festival's 2024 & 2025 to the festival list");
                    ImGui.BulletText("Fixed an issue where you could sometimes not interact with the Scene Manager");
                    ImGui.BulletText("Reenable double click to rename an actor ");
                    ImGui.BulletText("Facewear now properly displays in the advance appearance window (Thank you sparqle)");
                    ImGui.BulletText("Fixed an issue where, certain clothing did not parent skeletons correctly (Thank you sparqle)");
                    ImGui.BulletText("Fix pose preview image not shown on second viewing in the Library (Thank you sparqle)");
              
                }
              
                if(CollapsingHeader(" v0.5.2 – August 15 2025", "  - 7.3 Support  ", new Vector4(0.75f, 0.75f, 0.85f, 1.0f), false))
                {
                    DrawFeature(FontAwesomeIcon.None, "0.5.2.2", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

                    ImGui.BulletText("Fixed a rare crash ");
                    ImGui.BulletText("Fixed ImGUI assertion errors");
                    ImGui.BulletText("Disabled double click on actors and camera to rename them to fix a bug (temporarily) ");
             
                    DrawFeature(FontAwesomeIcon.None, "0.5.2", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));
                   
                    ImGui.BulletText("Update Brio to support FFXIV 7.3 (Thanks for the help Asgard!)");
                }

                if(CollapsingHeader(" v0.5.1 – March 27 2025", "  - 7.2 Support ", new Vector4(0.75f, 0.75f, 0.85f, 1.0f), false))
                {
                    DrawFeature(FontAwesomeIcon.None, "0.5.1.1", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));
                 
                    ImGui.BulletText("You can now double-click and Actor or Camera to rename them (Thanks @Bronya-Rand)");

                    DrawFeature(FontAwesomeIcon.None, "0.5.1", new Vector4(0.5f, 0.9f, 0.5f, 1.0f));

                    ImGui.BulletText("Fixed new Facewear being unable to be equipped");
                    ImGui.BulletText("Added the ability to rotate Free Cameras! (Thanks @Bronya-Rand)");

                    ImGui.BulletText("Fixed saving a project with props or mounts would cause the project not to load (Thanks @Bronya-Rand)");
                    ImGui.BulletText("Fixed the formant of `Time of Day` slider in Environment so it can be edited");
                    ImGui.BulletText("Fixed a crash when training your chocobo ");
                    ImGui.BulletText("Fixed a crash when in certain cutscenes");
                    ImGui.BulletText("Fixed the camera from snaping on gpose enter/exit");
                    ImGui.BulletText("Fixed a potential memory leak ");
                }

                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
            }

        // 

        ImGui.SetCursorPosX(((ImGui.GetWindowSize().Y - _closeButtonWidth) / 2));

        if(ImBrio.Button("Close", FontAwesomeIcon.SquareXmark, new Vector2(_closeButtonWidth, 0), centerTest: true))
        {
            this.IsOpen = false;
        }
    }

    //
    // some code found here is modified and CharacterSelect+
    // https://github.com/IcarusXIV/Character-Select- (link is includes the -)
    //

    private static void DrawBackground(Vector2 headerStart, Vector2 headerEnd)
    {
        var drawList = ImGui.GetWindowDrawList();
        uint gradientTop = ImGui.GetColorU32(new Vector4(0.2f, 0.4f, 0.8f, 0.15f));
        uint gradientBottom = ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.2f, 0.05f));
        drawList.AddRectFilledMultiColor(headerStart, headerEnd * ImGuiHelpers.GlobalScale, gradientTop, gradientTop, gradientBottom, gradientBottom);
    }

    private static bool CollapsingHeader(string title, string subTitle, Vector4 titleColor, bool defaultOpen)
    {
        var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;

        bool isOpen = false;
        using(ImRaii.PushColor(ImGuiCol.Text, titleColor))
        {
            isOpen = ImGui.CollapsingHeader(title, flags);
        }

        ImGui.SameLine();

        ImGui.TextColored(new Vector4(0.75f, 0.75f, 0.85f, 1.0f), subTitle);

        return isOpen;
    }

  
    private static void DrawFeature(FontAwesomeIcon icon, string title, Vector4 accentColor)
    {
        var drawList = ImGui.GetWindowDrawList();
        var startPos = ImGui.GetCursorScreenPos();

        // Feature section background
        var backgroundMin = startPos + new Vector2(-10, -5);
        var backgroundMax = startPos + new Vector2(ImGui.GetContentRegionAvail().X + 10, 25);
        drawList.AddRectFilled(backgroundMin, backgroundMax, ImGui.GetColorU32(new Vector4(0.12f, 0.12f, 0.15f, 0.6f)), 4f);
        drawList.AddRectFilled(backgroundMin, backgroundMin + new Vector2(3, backgroundMax.Y - backgroundMin.Y), ImGui.GetColorU32(accentColor), 2f);

        ImGui.Spacing();
        ImBrio.Icon(icon);
        ImGui.SameLine();
        ImGui.TextColored(accentColor, title);
        ImGui.Spacing();
    }
}
