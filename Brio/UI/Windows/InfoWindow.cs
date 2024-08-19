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
    private readonly UpdateWindow _updateWindow;

    public InfoWindow(ConfigurationService configurationService, UpdateWindow updateWindow) : base($"{Brio.Name} Welcome###brio_info_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
    {
        Namespace = "brio_info_namespace";

        _configurationService = configurationService;
        _updateWindow = updateWindow;

        Size = new Vector2(580, -1);
    }

    public override void Draw()
    {
        var segmentSize = ImGui.GetWindowSize().X / 3.1f;
        var buttonSize = new Vector2(segmentSize, ImGui.GetTextLineHeight() * 1.8f);

        using(var textGroup = ImRaii.Group())
        {
            if(textGroup.Success)
            {
                var text = $"""
                    Welcome to Brio, Version {_configurationService.Version}!

                    Brio is a suite of tools to enhance your GPosing experience,
                    Brio is currently in alpha, as such, there may be bugs,
                    if you find any, please report them.

                    A Big Thank you to (Night Song), (Alvar Valo), (Sufferhymn) & (Yasumi)
                    for their support on Kofi!
                                        
                    Thank you to the following for their code contributions to Brio!  
                    (@Yuki-Codes), (@danma3x), (@WorstAquaPlayer), 
                    (@snaeling), (@Caraxi) & (@gris-fuego);

                    Maintained & Developed by: Minmoose.
                    Originally Developed by: Asgard.
                    
                    Happy Posing!


                    """;

                ImGui.PushTextWrapPos(segmentSize * 3);
                ImGui.TextWrapped(text);
                ImGui.PopTextWrapPos();
            }
        }

        using var buttonGroup = ImRaii.Group();
        if(buttonGroup.Success)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 100, 255, 255) / 255);
            if(ImGui.Button("Support Development", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/minmoosexiv", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(86, 98, 246, 255) / 255);
            if(ImGui.Button("Join the Discord", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/KvGJCCnG8t", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 0, 0, 255) / 255);
            if(ImGui.Button("Report an Issue", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/Etheirys/Brio/issues", UseShellExecute = true });
            ImGui.PopStyleColor();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
            if(ImGui.Button("View the Changelog", buttonSize))
            {
                ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X - 630) / 2, (ImGui.GetIO().DisplaySize.Y - 535) / 2));
                _updateWindow.IsOpen = true;
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();
          
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 70, 0, 255) / 255);
            if(ImGui.Button("License & Attributions", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/Etheirys/Brio/blob/main/Acknowledgements.md", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(29, 161, 242, 255) / 255);
            if(ImGui.Button("More Links", buttonSize))
                Process.Start(new ProcessStartInfo { FileName = "https://etheirystools.carrd.co", UseShellExecute = true });
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }
    }
}
