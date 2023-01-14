using Dalamud.Plugin;

namespace Brio;

public class Plugin : IDalamudPlugin
{
    public string Name => Brio.PluginName;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Dalamud.Initialize(pluginInterface);
        Brio.Initialize();
        Dalamud.PluginInterface.UiBuilder.Draw += Brio.WindowSystem.Draw;

        Dalamud.PluginInterface.UiBuilder.DisableGposeUiHide = true;
        Dalamud.PluginInterface.UiBuilder.DisableCutsceneUiHide = true;
        Dalamud.PluginInterface.UiBuilder.DisableUserUiHide = true;
    }

    public void Dispose()
    {
        Dalamud.PluginInterface.UiBuilder.Draw -= Brio.WindowSystem.Draw;
        Brio.Destroy();
        Dalamud.Destroy();
    }
}