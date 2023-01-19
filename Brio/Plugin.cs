using Dalamud.Plugin;

namespace Brio;

public class Plugin : IDalamudPlugin
{
    public string Name => Brio.PluginName;

    private Brio _brio;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Dalamud.Initialize(pluginInterface);
        _brio = new Brio();
    }
    public void Dispose()
    {
        _brio.Dispose();
        Dalamud.Destroy();
    }
}
