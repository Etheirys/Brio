using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Brio.Config;
using System.Diagnostics;
using System.Numerics;

namespace Brio.UI.Windows;

internal class InfoWindow : Window
{
    private readonly ConfigurationService _configurationService;

    public InfoWindow(ConfigurationService configurationService) : base($"{Brio.Name} Info###brio_info_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_info_namespace";

        _configurationService = configurationService;

        Size = new Vector2(580, -1);
    }

    public override void Draw()
    {
        var segmentSize = ImGui.GetWindowSize().X / 3;
        var buttonSize = new Vector2(segmentSize, ImGui.GetTextLineHeight() * 1.8f);

        using (var textGroup = ImRaii.Group())
        {
            if (textGroup.Success)
            {
                string text = $"Welcome to Brio v{_configurationService.Version}!";
                text += "\n\n";
                text += "Brio is a suite of tools to enhance your GPosing experience, primarily developed by Asgard.";
                text += "\n\n";
                text += "Brio is currently in alpha, and as such, there may be bugs. If you find any, please report them.";
                text += "\n\n";
                text += "Happy Posing!";
                text += "\n";
                text += " - Asgard";

                ImGui.PushTextWrapPos(segmentSize * 2);
                ImGui.TextWrapped(text);
                ImGui.PopTextWrapPos();
            }
        }

        ImGui.SameLine();

        using (var buttonGroup = ImRaii.Group())
        {
            if (buttonGroup.Success)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(86, 98, 246, 255) / 255);
                if (ImGui.Button("Discord", buttonSize))
                    Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/KvGJCCnG8t", UseShellExecute = true });
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 0, 0, 255) / 255);
                if (ImGui.Button("Report Issue", buttonSize))
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/AsgardXIV/Brio/issues", UseShellExecute = true });
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
                if (ImGui.Button("GitHub Repository", buttonSize))
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/AsgardXIV/Brio", UseShellExecute = true });
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 91, 94, 255) / 255);
                if (ImGui.Button("Donate on Ko-Fi", buttonSize))
                    Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/asgard", UseShellExecute = true });
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(29, 161, 242, 255) / 255);
                if (ImGui.Button("Asgard's Twitter", buttonSize))
                    Process.Start(new ProcessStartInfo { FileName = "https://twitter.com/AsgardXIV", UseShellExecute = true });
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 70, 0, 255) / 255);
                if (ImGui.Button("Licenses", buttonSize))
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/AsgardXIV/Brio/blob/main/Acknowledgements.md", UseShellExecute = true });
                ImGui.PopStyleColor();

            }
        }
    }
}
