using Brio.Config;
using Brio.Core;
using Brio.Game.Actor;
using Brio.Game.Chat;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.Render;
using Brio.IPC;
using Brio.UI;
using System;

namespace Brio;

public class Brio : IDisposable
{
    public const string PluginName = "Brio";
    public static string PluginVersion = typeof(Brio).Assembly.GetName().Version!.ToString(fieldCount: 3);

    private ServiceManager _serviceManager;

    public Brio()
    {
        _serviceManager = new ServiceManager();

        // Services
        _serviceManager.Add<ConfigService>();
        _serviceManager.Add<FrameworkService>();
        _serviceManager.Add<CommandHandlerService>();
        _serviceManager.Add<GPoseService>();
        _serviceManager.Add<RenderHookService>();
        _serviceManager.Add<ActorService>();
        _serviceManager.Add<ActorRedrawService>();
        _serviceManager.Add<ActorSpawnService>();
        _serviceManager.Add<PenumbraIPCService>();
        _serviceManager.Add<PenumbraCollectionService>();
        _serviceManager.Add<UIService>();

        _serviceManager.Add<WelcomeService>();

        // Start Everything
        _serviceManager.Start();

        Dalamud.Framework.Update += Framework_Update;
    }

    private void Framework_Update(global::Dalamud.Game.Framework framework)
    {
        _serviceManager.Tick();
    }


    public void Dispose()
    {
        Dalamud.Framework.Update -= Framework_Update;
        _serviceManager.Dispose();
    }
}