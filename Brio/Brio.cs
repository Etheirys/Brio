using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Game.Actor;
using Brio.Game.Chat;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Game.World;
using Brio.IPC;
using Brio.Resources;
using Brio.UI;
using Brio.UI.Windows;
using Brio.Web;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Brio.Game.Camera;
using Brio.UI.Windows.Specialized;
using Brio.Game.Core;
using System;

namespace Brio;

public class Brio : IDalamudPlugin
{
    public const string Name = "Brio";

    private readonly ServiceProvider _services;

    public static IPluginLog Log { get; private set; } = null!;

    public Brio(DalamudPluginInterface pluginInterface)
    {
        // Setup dalamud services
        var dalamudServices = new DalamudServices(pluginInterface);
        Log = dalamudServices.Log;

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Log.Info($"Starting {Name}...");

        // Setup plugin services
        var serviceCollection = SetupServices(dalamudServices);
        _services = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        dalamudServices.Framework.RunOnTick(() =>
        {
            try
            {
                // Initialize the singletons
                foreach (var service in serviceCollection)
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
            catch (Exception e)
            {
                Log.Error(e, $"Failed to start {Name} in {stopwatch.ElapsedMilliseconds}ms");
                _services.Dispose();
                throw;
            }
        }, delayTicks: 30); // TODO: Why do we need to wait several frames for some users?
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

        // Core / Misc
        serviceCollection.AddSingleton<EventBus>();
        serviceCollection.AddSingleton<ConfigurationService>();
        serviceCollection.AddSingleton<ResourceProvider>();
        serviceCollection.AddSingleton<GameDataProvider>();
        serviceCollection.AddSingleton<WelcomeService>();

        // IPC
        serviceCollection.AddSingleton<PenumbraService>();
        serviceCollection.AddSingleton<GlamourerService>();

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
        serviceCollection.AddSingleton<SkeletonService>();
        serviceCollection.AddSingleton<PosingService>();
        serviceCollection.AddSingleton<CameraService>();
        serviceCollection.AddSingleton<ObjectMonitorService>();

        // UI
        serviceCollection.AddSingleton<UIManager>();
        serviceCollection.AddSingleton<MainWindow>();
        serviceCollection.AddSingleton<SettingsWindow>();
        serviceCollection.AddSingleton<InfoWindow>();
        serviceCollection.AddSingleton<ActorAppearanceWindow>();
        serviceCollection.AddSingleton<ActionTimelineWindow>();
        serviceCollection.AddSingleton<PosingOverlayWindow>();
        serviceCollection.AddSingleton<PosingOverlayToolbarWindow>();
        serviceCollection.AddSingleton<PosingTransformWindow>();
        serviceCollection.AddSingleton<CameraWindow>();
        serviceCollection.AddSingleton<PosingGraphicalWindow>();

        return serviceCollection;
    }

    public void Dispose()
    {
        _services.Dispose();
    }
}
