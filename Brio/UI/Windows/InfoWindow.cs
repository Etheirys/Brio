using Brio.Config;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;

namespace Brio.UI.Windows;

internal class InfoWindow : Window
{
    private readonly ConfigurationService _configurationService;

    public InfoWindow(ConfigurationService configurationService) : 
        base($"{Brio.Name}, Welcome###brio_info_window", ImGuiWindowFlags.NoCollapse| ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize| ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings)
    {
        Namespace = "brio_info_namespace";

        _configurationService = configurationService;

        Size = new Vector2(650, 350);
    }

    public override void Draw()
    {

        using(var textGroup = ImRaii.Group())
        {
            if(textGroup.Success)
            {
                var text = $"""
                   
                    Welcome to the Brio Crash Test, 2.3.2!

                    While Testing, use Brio as you normally would,

                    if you encounter a crash while using this version of Brio,
                    make sure to report it to me on GitHub, Discord, or Twitter 
                    with the following info:

                    A complete list of ***ALL*** you're Penumbra mods installed &...
                    A complete list of ***ALL*** installed Dalamud plugins 


                    Thank You!


                    """;

                ImGui.TextWrapped(text);
            }
        }
      
        var segmentSize = ImGui.GetWindowSize().X / 4f;
        var buttonSize = new Vector2(segmentSize - 6, 32);

        using var buttonGroup = ImRaii.Group();
        if(buttonGroup.Success)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(86, 98, 246, 255) / 255);
            if(ImGui.Button("Join the Discord", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/KvGJCCnG8t", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 0, 0, 255) / 255);
            if(ImGui.Button("View the Issue on GitHub", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/Etheirys/Brio/issues/46", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 100, 255, 255) / 255);
            if(ImGui.Button("View my Twitter", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://twitter.com/MiniatureMoosey", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 70, 0, 255) / 255);
            if(ImGui.Button("License & Attributions", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/Etheirys/Brio/blob/main/Acknowledgements.md", UseShellExecute = true });
            ImGui.PopStyleColor();
        }
    }
}
