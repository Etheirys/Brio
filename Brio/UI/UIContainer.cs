using Brio.Config;
using Brio.Core;
using Brio.UI.Windows;
using Dalamud.Interface.Windowing;

namespace Brio.UI;

public class UIService : ServiceBase<UIService>
{
    public WindowSystem WindowSystem { get; private set; } = null!;
    public MainWindow MainWindow { get; private set; } = null!;
    public InfoWindow InfoWindow { get; private set; } = null!;
    public SettingsWindow SettingsWindow { get; private set; } = null!;

    public override void Start()
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

        base.Start();
    }

    public void ApplyUISettings()
    {
        Dalamud.PluginInterface.UiBuilder.DisableCutsceneUiHide = ConfigService.Configuration.ShowInCutscene;
        Dalamud.PluginInterface.UiBuilder.DisableUserUiHide = ConfigService.Configuration.ShowWhenUIHidden;
    }

    private void UiBuilder_Draw()
    {
        WindowSystem.Draw();
    }
    private void UiBuilder_OpenConfigUi()
    {
        SettingsWindow.Toggle();
    }

    public override void Dispose()
    {
        Dalamud.PluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
        Dalamud.PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
        WindowSystem.RemoveAllWindows();
    }
}
