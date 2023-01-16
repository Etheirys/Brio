using Brio.UI.Windows;
using Dalamud.Interface.Windowing;
using System;

namespace Brio.UI;

public class UIContainer : IDisposable
{
    public WindowSystem WindowSystem { get; private set; } = null!;
    public MainWindow MainWindow { get; private set; } = null!;
    public InfoWindow InfoWindow { get; private set; } = null!;
    public SettingsWindow SettingsWindow { get; private set; } = null!;

    public UIContainer()
    {
        WindowSystem= new WindowSystem(Brio.PluginName);
        MainWindow = new MainWindow();
        InfoWindow = new InfoWindow();
        SettingsWindow = new SettingsWindow();

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(InfoWindow);
        WindowSystem.AddWindow(SettingsWindow);

        Dalamud.PluginInterface.UiBuilder.Draw += UiBuilder_Draw;
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;

        Dalamud.PluginInterface.UiBuilder.DisableGposeUiHide = true;

        ApplyUISettings();
    }

    public void ApplyUISettings()
    {
        Dalamud.PluginInterface.UiBuilder.DisableCutsceneUiHide = Brio.Configuration.ShowInCutscene;
        Dalamud.PluginInterface.UiBuilder.DisableUserUiHide = Brio.Configuration.ShowWhenUIHidden;
    }

    private void UiBuilder_Draw()
    {
        WindowSystem.Draw();
    }
    private void UiBuilder_OpenConfigUi()
    {
        SettingsWindow.Toggle();
    }

    public void Dispose()
    {
        Dalamud.PluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
        WindowSystem.RemoveAllWindows();
    }
}
