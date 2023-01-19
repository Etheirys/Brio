using Brio.Core;

namespace Brio.Config;

public class ConfigService : ServiceBase<ConfigService>
{
    public static Configuration Configuration { get; private set; } = null!;

    public ConfigService()
    {
        Configuration = Dalamud.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    }

    public static void Save()
    {
        Dalamud.PluginInterface.SavePluginConfig(Configuration);
    }

    public override void Stop()
    {
        Save();
    }
}
