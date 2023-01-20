using Brio.UI.Components;
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
            MaximumSize = new Vector2(250, 5000),
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
                ActorTabControls.Draw();
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("World"))
            {
                WorldTabControls.Draw();
                ImGui.EndTabItem();
            }

            if(ImGui.BeginTabItem("Hooks"))
            {
                HooksTabControls.Draw();
                ImGui.EndTabItem();
            }

            if(Brio.IsDebug)
            {
                if(ImGui.BeginTabItem("Debug"))
                {
                    DebugTabControls.Draw();
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

        ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 70f);
        ImGui.SetCursorPosY(initialPos.Y - ImGui.GetTextLineHeight() * 2f);
        if(ImGui.Button(FontAwesomeIcon.Cog.ToIconString()))
            UIService.Instance.SettingsWindow.Toggle();

        ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 100f);
        ImGui.SetCursorPosY(initialPos.Y - ImGui.GetTextLineHeight() * 2f);
        if(ImGui.Button(FontAwesomeIcon.InfoCircle.ToIconString()))
            UIService.Instance.InfoWindow.Toggle();

        ImGui.PopFont();
        ImGui.PopStyleColor();

        ImGui.PopClipRect();
        ImGui.SetCursorPos(initialPos);
    }
}
