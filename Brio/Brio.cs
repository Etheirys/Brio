using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.Chat;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Game.World;
using Brio.Input;
using Brio.IPC;
using Brio.Resources;
using Brio.UI;
using Brio.UI.Windows;
using Brio.UI.Windows.Specialized;
using Brio.Web;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

namespace Brio;

public class Brio : IDalamudPlugin
{
    public const string Name = "Brio Crash Test";

    private ServiceProvider? _services = null;

    public static IPluginLog Log { get; private set; } = null!;
    public static IFramework Framework { get; private set; } = null!;

    public Brio(DalamudPluginInterface pluginInterface)
    {
        // Setup dalamud services
        var dalamudServices = new DalamudServices(pluginInterface);
        Log = dalamudServices.Log;
        Framework = dalamudServices.Framework;

        dalamudServices.Framework.RunOnTick(() =>
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Log.Info($"Starting {Name}...");

            try
            {
                // Setup plugin services
                var serviceCollection = SetupServices(dalamudServices);

                _services = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

                // Initialize the singletons
                foreach(var service in serviceCollection)
                {
                    if(service.Lifetime == ServiceLifetime.Singleton)
                    {
                        Brio.Log.Debug($"Initializing {service.ServiceType}...");
                        _services.GetRequiredService(service.ServiceType);
                    }
                }

                // Setup default entities
                Brio.Log.Debug($"Setting up default entitites...");
                _services.GetRequiredService<EntityManager>().SetupDefaultEntities();
                _services.GetRequiredService<EntityActorManager>().AttachContainer();

                // Trigger GPose events to ensure the plugin is in the correct state
                Brio.Log.Debug($"Triggering initial GPose state...");
                _services.GetRequiredService<GPoseService>().TriggerGPoseChange();

                Log.Info($"Started {Name} in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch(Exception e)
            {
                Log.Error(e, $"Failed to start {Name} in {stopwatch.ElapsedMilliseconds}ms");
                _services?.Dispose();
                throw;
            }
        }, delayTicks: 2); // TODO: Why do we need to wait several frames for some users?
    }

    private IServiceCollection SetupServices(DalamudServices dalamudServices)
    {
        ServiceCollection serviceCollection = new();

        // Dalamud
        serviceCollection.AddSingleton(dalamudServices.PluginInterface);
        serviceCollection.AddSingleton(dalamudServices.Framework);
        serviceCollection.AddSingleton(dalamudServices.GameInteropProvider);
        serviceCollection.AddSingleton(dalamudServices.ClientState);
        serviceCollection.AddSingleton(dalamudServices.SigScanner);
        serviceCollection.AddSingleton(dalamudServices.ObjectTable);
        serviceCollection.AddSingleton(dalamudServices.DataManager);
        serviceCollection.AddSingleton(dalamudServices.CommandManager);
        serviceCollection.AddSingleton(dalamudServices.ToastGui);
        serviceCollection.AddSingleton(dalamudServices.TargetManager);
        serviceCollection.AddSingleton(dalamudServices.TextureProvider);
        serviceCollection.AddSingleton(dalamudServices.Log);
        serviceCollection.AddSingleton(dalamudServices.ChatGui);
        serviceCollection.AddSingleton(dalamudServices.KeyState);

        // Core / Misc
        serviceCollection.AddSingleton<EventBus>();
        serviceCollection.AddSingleton<ConfigurationService>();
        serviceCollection.AddSingleton<ResourceProvider>();
        serviceCollection.AddSingleton<GameDataProvider>();
        serviceCollection.AddSingleton<WelcomeService>();
        serviceCollection.AddSingleton<InputService>();

        // IPC
        serviceCollection.AddSingleton<BrioIPCService>();
        serviceCollection.AddSingleton<PenumbraService>();
        serviceCollection.AddSingleton<GlamourerService>();
        serviceCollection.AddSingleton<MareService>();

        // Web
        serviceCollection.AddSingleton<WebService>();

        // Entity
        serviceCollection.AddSingleton<EntityManager>();
        serviceCollection.AddSingleton<EntityActorManager>();

        // Game
        serviceCollection.AddSingleton<TargetService>();
        serviceCollection.AddSingleton<ActorSpawnService>();
        serviceCollection.AddSingleton<ActorRedrawService>();
        serviceCollection.AddSingleton<ActorAppearanceService>();
        serviceCollection.AddSingleton<ActionTimelineService>();
        serviceCollection.AddSingleton<GPoseService>();
        serviceCollection.AddSingleton<CommandHandlerService>();
        serviceCollection.AddSingleton<ModelTransformService>();
        serviceCollection.AddSingleton<TimeService>();
        serviceCollection.AddSingleton<WeatherService>();
        serviceCollection.AddSingleton<FestivalService>();
        serviceCollection.AddSingleton<WorldRenderingService>();
        serviceCollection.AddSingleton<SkeletonService>();
        serviceCollection.AddSingleton<PosingService>();
        serviceCollection.AddSingleton<IKService>();
        serviceCollection.AddSingleton<CameraService>();
        serviceCollection.AddSingleton<ObjectMonitorService>();


        // UI
        serviceCollection.AddSingleton<UIManager>();
        serviceCollection.AddSingleton<MainWindow>();
        serviceCollection.AddSingleton<SettingsWindow>();
        serviceCollection.AddSingleton<InfoWindow>();
        serviceCollection.AddSingleton<UpdateWindow>();
        serviceCollection.AddSingleton<ActorAppearanceWindow>();
        serviceCollection.AddSingleton<ActionTimelineWindow>();
        serviceCollection.AddSingleton<PosingOverlayWindow>();
        serviceCollection.AddSingleton<KeyBindPromptWindow>();
        serviceCollection.AddSingleton<PosingOverlayToolbarWindow>();
        serviceCollection.AddSingleton<PosingTransformWindow>();
        serviceCollection.AddSingleton<CameraWindow>();
        serviceCollection.AddSingleton<PosingGraphicalWindow>();

        return serviceCollection;
    }

    public static void NotifyError(string message)
    {
        EventBus.Instance.NotifyError(message);
    }

    public void Dispose()
    {
        _services?.Dispose();
    }
}
