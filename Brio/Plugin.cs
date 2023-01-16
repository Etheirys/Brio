using Dalamud.Plugin;

namespace Brio;

public class Plugin : IDalamudPlugin
{
    public string Name => Brio.PluginName;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Dalamud.Initialize(pluginInterface);
        Brio.Initialize();
    }
    public void Dispose()
    {
        Brio.Destroy();
        Dalamud.Destroy();
    }
}