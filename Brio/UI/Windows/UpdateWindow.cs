using Brio.Config;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Windows;

internal class UpdateWindow : Window
{
    private readonly ConfigurationService _configurationService;

    public UpdateWindow(ConfigurationService configurationService) : 
        base($"{Brio.Name} Update###brio_update_window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse)
    {
        Namespace = "brio_update_namespace";

        _configurationService = configurationService;

        Size = new Vector2(600, -1);
    }

    public override void Draw()
    {
        var segmentSize = ImGui.GetWindowSize().X / 1f;

        using var textGroup = ImRaii.Group();

        if(textGroup.Success)
        {
            var text = $"""
                    Welcome to Brio, Version {_configurationService.Version}!

                    Lorem ipsum dolor sit amet, consectetur adipiscing elit, 
                    sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
                    Ut tortor pretium viverra suspendisse potenti nullam ac. 
                    Iaculis at erat pellentesque adipiscing commodo elit.
                    Egestas purus viverra accumsan in. Pharetra magna ac placerat 
                    vestibulum lectus mauris ultrices eros in.
                    """;

            ImGui.PushTextWrapPos(segmentSize);
            ImGui.TextWrapped(text);
            ImGui.PopTextWrapPos();
        }
    }

}
