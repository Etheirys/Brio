using Brio.Config;
using Brio.Game.GPose;
using Brio.UI.Windows;
using System;

namespace Brio.Core;

public class WelcomeService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly UpdateWindow _updateWindow;

    public WelcomeService(ConfigurationService configService, MainWindow mainWindow, UpdateWindow updateWindow, GPoseService gPoseService)
    {
        _gPoseService = gPoseService;
        _updateWindow = updateWindow;

        if(configService.Configuration.PopupKey == -1) // New User
        {
            updateWindow.IsOpen = true;
            configService.Configuration.PopupKey = Configuration.CurrentPopupKey;
        }
        else if(configService.Configuration.PopupKey != Configuration.CurrentPopupKey)
        {
            updateWindow.IsOpen = true;
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

            Brio.Log.Warning($"Library sources have been re-established!");
        }

        if(configService.Configuration.Version <= 2)
        {
            updateWindow.IsOpen = true;

            configService.Configuration.Version = Configuration.CurrentVersion;
        }

        configService.ApplyChange();

        #endregion

        _gPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }

    private void GPoseService_OnGPoseStateChange(bool newState)
    {
        if(newState)
        {

        }
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
        GC.SuppressFinalize(this);
    }
}
