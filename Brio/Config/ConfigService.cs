using Brio.Core;

namespace Brio.Config;

public class ConfigService : ServiceBase<ConfigService>
{
    public static Configuration Configuration { get; private set; } = null!;

    public ConfigService()
    {
        Configuration = Dalamud.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    }

    public override void Stop()
    {
        Dalamud.PluginInterface.SavePluginConfig(Configuration);
    }
}
