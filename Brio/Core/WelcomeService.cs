using Brio.Config;
using Brio.UI.Windows;
using ImGuiNET;
using System.Numerics;

namespace Brio.Core;

internal class WelcomeService
{
    public WelcomeService(ConfigurationService configService, MainWindow mainWindow, InfoWindow infoWindow)
    {
        ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - 650, (ImGui.GetIO().DisplaySize.Y / 2) - 350));
        infoWindow.IsOpen = true;

        if(configService.Configuration.Interface.OpenBrioBehavior == OpenBrioBehavior.OnPluginStartup)
            mainWindow.IsOpen = true;
    }
}
