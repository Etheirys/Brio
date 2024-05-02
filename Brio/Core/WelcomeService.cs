﻿using Brio.Config;
using Brio.UI.Windows;
using ImGuiNET;
using System.Numerics;

namespace Brio.Core;

internal class WelcomeService
{
    public WelcomeService(ConfigurationService configService, MainWindow mainWindow, InfoWindow infoWindow, UpdateWindow updateWindow)
    {
        ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - 630, (ImGui.GetIO().DisplaySize.Y / 2) - 535));
       
        infoWindow.IsOpen = true;

        if(configService.Configuration.PopupKey == -1) // New User
        {
            configService.Configuration.PopupKey = Configuration.CurrentPopupKey;
            //infoWindow.IsOpen = true;
        }
        else if(configService.Configuration.PopupKey != Configuration.CurrentPopupKey)
        {
            //ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - 630, (ImGui.GetIO().DisplaySize.Y / 2) - 535));
            //updateWindow.IsOpen = true;
          
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
         
            //
            //// KENTODO FIX
            //

            //configService.Configuration.Library.Files.Clear();
            //configService.Configuration.Library.ReEstablishDefaultPaths();
            //configService.Configuration.Version = Configuration.CurrentVersion;

            Brio.Log.Warning($"Library sources have been re-established!");
        }

        configService.ApplyChange();

        #endregion

    }
}
