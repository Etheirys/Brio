using Brio.UI.Components;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Drawing;
using System.Numerics;

namespace Brio.UI.Windows;

public class MainWindow : Window
{
    public MainWindow() : base(Brio.PluginName, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Size = new Vector2(250, -1);
    }

    public override void Draw()
    {
        DrawWidgets();

        if(ImGui.BeginTabBar("brio_tabs"))
        {
            if (ImGui.BeginTabItem("Actors"))
            {
                ActorTabControls.Draw();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Global"))
            {
                GlobalTabControls.Draw();
                ImGui.EndTabItem();
            }

#if DEBUG
            if (ImGui.BeginTabItem("Debug"))
            {
                DebugTabControls.Draw();
                ImGui.EndTabItem();
            }
#endif

            ImGui.EndTabBar();
        }
    }

    private void DrawWidgets()
    {
        var initialPos = ImGui.GetCursorPos();
        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);

        ImGui.PushStyleColor(ImGuiCol.Button, 0x00000000);
        ImGui.PushFont(UiBuilder.IconFont);

        ImGui.SetCursorPosX(initialPos.X + 170f);
        ImGui.SetCursorPosY(initialPos.Y - ImGui.GetTextLineHeight() * 2f);
        if(ImGui.Button(FontAwesomeIcon.Cog.ToIconString()))
            Brio.UI.SettingsWindow.Toggle();

        ImGui.SetCursorPosX(initialPos.X + 145f);
        ImGui.SetCursorPosY(initialPos.Y - ImGui.GetTextLineHeight() * 2f);
        if (ImGui.Button(FontAwesomeIcon.InfoCircle.ToIconString()))
            Brio.UI.InfoWindow.Toggle();

        ImGui.PopFont();
        ImGui.PopStyleColor();

        ImGui.PopClipRect();
        ImGui.SetCursorPos(initialPos);
    }
}
