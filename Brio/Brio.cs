using Brio.Config;
using Brio.Game.Actor;
using Brio.Game.Chat;
using Brio.Game.GPose;
using Brio.Game.Render;
using Brio.UI;

namespace Brio;

public static class Brio
{
    public const string PluginName = "Brio";
    public static string PluginVersion = typeof(Brio).Assembly.GetName().Version!.ToString(fieldCount: 3);

    public static Configuration Configuration { get; private set; } = null!;
    public static GPoseService GPoseService { get; private set; } = null!;
    public static ActorSpawnService ActorSpawnService { get; private set; } = null!;
    public static ActorRedrawService ActorRedrawService { get; private set; } = null!;

    public static UIContainer UI { get; private set; } = null!;
    public static RenderHooks RenderHooks { get; set; } = null!;

    private static CommandHandler CommandHandler { get; set; } = null!;

    public static void Initialize()
    {
        Configuration = Dalamud.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        CommandHandler = new();

        GPoseService = new GPoseService();
        ActorSpawnService  = new ActorSpawnService();
        ActorRedrawService = new ActorRedrawService();
        RenderHooks= new RenderHooks();

        UI = new UIContainer();

        StartupLogic();
    }

    private static void StartupLogic()
    {
        if(Configuration.IsFirstTimeUser)
        {
            UI.InfoWindow.IsOpen = true;
            Configuration.IsFirstTimeUser = false;
        }

        if(Configuration.OpenBrioBehavior == OpenBrioBehavior.OnPluginStartup)
            UI.MainWindow.IsOpen = true;

        if (Configuration.OpenBrioBehavior == OpenBrioBehavior.OnGPoseEnter && GPoseService.IsInGPose)
            UI.MainWindow.IsOpen = true;
    }

    public static void Destroy()
    {
        Dalamud.PluginInterface.SavePluginConfig(Configuration);

        UI.Dispose();

        RenderHooks.Dispose();
        GPoseService.Dispose();
        ActorSpawnService.Dispose();
        ActorRedrawService.Dispose();
        CommandHandler.Dispose();
    }
}