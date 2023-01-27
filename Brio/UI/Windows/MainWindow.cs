using Brio.UI.Components;
using Brio.UI.Components.Actor;
using Brio.UI.Components.Debug;
using Brio.UI.Components.World;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Windows;

public class MainWindow : Window
{
    public MainWindow() : base($"{Brio.PluginName} {(Brio.IsDebug ? "(Debug)" : $"v{Brio.PluginVersion}")}", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize )
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MaximumSize = new Vector2(2000, 5000),
            MinimumSize = new Vector2(250, 200)
        };
    }

    public override void Draw()
    {
        DrawWidgets();

        if(ImGui.BeginTabBar("brio_tabs"))
        {
            if(ImGui.BeginTabItem("Actors"))
            {
                ActorTab.Draw();
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("World"))
            {
                WorldTab.Draw();
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("Hooks"))
            {
                HooksTab.Draw();
                ImGui.EndTabItem();
            }

            if(Brio.IsDebug)
            {
                if(ImGui.BeginTabItem("Debug"))
                {
                    DebugTab.Draw();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

           
        }
    }

    private void DrawWidgets()
    {
        var initialPos = ImGui.GetCursorPos();
        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);

        ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
        ImGui.PushFont(UiBuilder.IconFont);

        ImGui.SetCursorPosX(ImGui.GetWindowSize().X - (4 * ImGui.GetFontSize()));
        ImGui.SetCursorPosY(0);
        if(ImGui.Button(FontAwesomeIcon.Cog.ToIconString()))
            UIService.Instance.SettingsWindow.Toggle();

        ImGui.SetCursorPosX(ImGui.GetWindowSize().X - (5.5f * ImGui.GetFontSize()));
        ImGui.SetCursorPosY(0);
        if(ImGui.Button(FontAwesomeIcon.InfoCircle.ToIconString()))
            UIService.Instance.InfoWindow.Toggle();

        ImGui.PopFont();
        ImGui.PopStyleColor();

        ImGui.PopClipRect();
        ImGui.SetCursorPos(initialPos);
    }
}
