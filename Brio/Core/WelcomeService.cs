using Brio.Config;
using Brio.UI.Windows;

namespace Brio.Core;

internal class WelcomeService
{
    public WelcomeService(ConfigurationService configService, MainWindow mainWindow, InfoWindow infoWindow)
    {
        if(configService.Configuration.PopupKey != Configuration.CurrentPopupKey)
        {
            infoWindow.IsOpen = true;
            configService.Configuration.PopupKey = Configuration.CurrentPopupKey;
        }

        if(configService.Configuration.Interface.OpenBrioBehavior == OpenBrioBehavior.OnPluginStartup)
        {
            mainWindow.IsOpen = true;
        }

        #region Configuration Reestablishment

        // Version 1
        if(configService.Configuration.Version == 1)
        {
            Brio.Log.Error($"Library sources need to be re-established! oldVerion:{configService.Configuration.Version} newVerion:{Configuration.CurrentVersion}");

            configService.Configuration.Library.Files.Clear();

            configService.Configuration.Library.ReEstablishDefaultPaths();

            configService.Configuration.Version = Configuration.CurrentVersion;

            configService.ApplyChange();

            Brio.Log.Warning($"Library sources have been re-established!");
        }

        #endregion

    }
}
