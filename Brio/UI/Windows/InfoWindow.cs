using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;

namespace Brio.UI.Windows;

public class InfoWindow : Window
{
    private static Vector2 ButtonSize = new Vector2(150, 25);

    public InfoWindow() : base($"{Brio.PluginName} Information", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Size = new Vector2(580, -1);
    }

    public override void Draw()
    {
        ImGui.BeginGroup();

        ImGui.Text($"Welcome to Brio v{Brio.PluginVersion}!");

        ImGui.Spacing();

        ImGui.Text("Brio is a small set of utilities from Asgard.");
        ImGui.Text("It is designed to enhance the experience of using GPose.");
        ImGui.Text("It is not a posing tool (like Anamnesis or Ktisis).");

        ImGui.Spacing();

        ImGui.Text("Brio is still very early in development so issues are expected.");
        ImGui.Text("Report an issue or join our Discord for assistance.");

        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 100, 0, 255) / 255);
        if(ImGui.Button("Get Started", ButtonSize))
        {
            IsOpen = false;
            UIService.Instance.MainWindow.IsOpen = true;
        }
        ImGui.PopStyleColor();

        ImGui.EndGroup();

        ImGui.SameLine(ImGui.GetItemRectSize().X + 50);

        ImGui.BeginGroup();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(86, 98, 246, 255) / 255);
        if(ImGui.Button("Discord", ButtonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/xivtools", UseShellExecute = true });
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 0, 0, 255) / 255);
        if(ImGui.Button("Report Issue", ButtonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/AsgardXIV/Brio/issues", UseShellExecute = true });
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
        if(ImGui.Button("GitHub Repository", ButtonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/AsgardXIV/Brio", UseShellExecute = true });
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 91, 94, 255) / 255);
        if(ImGui.Button("Donate on Ko-Fi", ButtonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/asgard", UseShellExecute = true });
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(29, 161, 242, 255) / 255);
        if(ImGui.Button("Asgard's Twitter", ButtonSize))
            Process.Start(new ProcessStartInfo { FileName = "https://twitter.com/AsgardXIV", UseShellExecute = true });
        ImGui.PopStyleColor();

        ImGui.EndGroup();
    }
}
