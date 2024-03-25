using Brio.Config;
using Brio.UI.Windows;

namespace Brio.Core;

internal class WelcomeService
{
    public WelcomeService(ConfigurationService configService, MainWindow mainWindow, InfoWindow infoWindow, UpdateWindow updateWindow)
    {
        if(configService.Configuration.PopupKey != Configuration.CurrentPopupKey)
        {
            updateWindow.IsOpen = true;
            configService.Configuration.PopupKey = Configuration.CurrentPopupKey;
        }

        if(configService.Configuration.Interface.OpenBrioBehavior == OpenBrioBehavior.OnPluginStartup)
            mainWindow.IsOpen = true;
    }
}
