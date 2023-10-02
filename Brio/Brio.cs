using Brio.Config;
using Brio.Core;
using Brio.Game.Actor;
using Brio.Game.Chat;
using Brio.Game.GPose;
using Brio.Game.Render;
using Brio.Game.World;
using Brio.IPC;
using Brio.UI;
using Brio.Web;
using Dalamud.Plugin.Services;
using System;

namespace Brio;

public class Brio : IDisposable
{
    public const string PluginName = "Brio";
    public static readonly string PluginVersion = typeof(Brio).Assembly.GetName().Version?.ToString() ?? "(Unknown Version)";

    private static ServiceManager _serviceManager { get; set; } = null!;

    public Brio()
    {
        _serviceManager = new ServiceManager();

        // Services
        _serviceManager.Add<ConfigService>();
        _serviceManager.Add<CommandHandlerService>();
        _serviceManager.Add<GPoseService>();
        _serviceManager.Add<RenderHookService>();
        _serviceManager.Add<ActorService>();
        _serviceManager.Add<ActorRedrawService>();
        _serviceManager.Add<ActorSpawnService>();
        _serviceManager.Add<ActionTimelineService>();
        _serviceManager.Add<TimeService>();
        _serviceManager.Add<WeatherService>();
        _serviceManager.Add<FestivalService>();
        _serviceManager.Add<PenumbraIPCService>();
        _serviceManager.Add<PenumbraCollectionService>();
        _serviceManager.Add<BrioIPCService>();

        // Presentation
        _serviceManager.Add<UIService>();
        _serviceManager.Add<WebService>();
        _serviceManager.Add<WelcomeService>();

        Dalamud.Framework.RunOnFrameworkThread(() =>
        {
            _serviceManager.Start();
            Dalamud.Framework.Update += Framework_Update;
        });
    }

    private void Framework_Update(IFramework framework)
    {
        _serviceManager.Tick();
    }

    public void Dispose()
    {
        Dalamud.Framework.Update -= Framework_Update;
        _serviceManager.Dispose();
    }

#if DEBUG
    public static bool IsDebug => true;
#else
    public static bool IsDebug => _serviceManager.IsStarted && ConfigService.Configuration.ForceDebug;
#endif
}
