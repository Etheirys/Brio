using Brio.Config;
using Brio.UI.Windows;

namespace Brio.Core;

internal class WelcomeService
{
    public WelcomeService(ConfigurationService configService, MainWindow mainWindow, InfoWindow infoWindow)
    {
        if (configService.Configuration.PopupKey != Configuration.CurrentPopupKey)
        {
            infoWindow.IsOpen = true;
            configService.Configuration.PopupKey = Configuration.CurrentPopupKey;
        }

        if (configService.Configuration.Interface.OpenBrioBehavior == OpenBrioBehavior.OnPluginStartup)
            mainWindow.IsOpen = true;
    }
}
