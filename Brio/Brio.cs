using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Files;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.Chat;
using Brio.Game.Core;
using Brio.Game.Cutscene;
using Brio.Game.GPose;
using Brio.Game.Input;
using Brio.Game.Posing;
using Brio.Game.Scene;
using Brio.Game.World;
using Brio.Input;
using Brio.IPC;
using Brio.IPC.API;
using Brio.Library;
using Brio.Library.Sources;
using Brio.MCDF.Game.FileCache;
using Brio.MCDF.Game.Services;
using Brio.Resources;
using Brio.Services;
using Brio.UI;
using Brio.UI.Windows;
using Brio.UI.Windows.Specialized;
using Brio.Web;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using static Brio.Core.Win32;

namespace Brio;

public class Brio : IDalamudPlugin
{
    public const int MajorAPIVersion = 3;
    public const int MinorAPIVersion = 0;

    public const string Name = "BRIO";

    private static ServiceProvider? _services = null;

    public static IPluginLog Log { get; private set; } = null!;
    public static IFramework Framework { get; private set; } = null!;

    public Brio(IDalamudPluginInterface pluginInterface)
    {
        // Setup dalamud services
        var dalamudServices = new DalamudPluginService(pluginInterface);
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
                        Log.Debug($"Initializing {service.ServiceType}...");
                        _services.GetRequiredService(service.ServiceType);
                    }
                }

                // Setup default entities
                Log.Debug($"Setting up default entitites...");
                _services.GetRequiredService<EntityManager>().SetupDefaultEntities();
                _services.GetRequiredService<EntityActorManager>().AttachContainer();
                _services.GetRequiredService<Mediator>().StartAsync(CancellationToken.None);

                // Trigger GPose events to ensure the plugin is in the correct state
                Log.Debug($"Triggering initial GPose state...");
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

    private static ServiceCollection SetupServices(DalamudPluginService dalamudServices)
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
        serviceCollection.AddSingleton(dalamudServices.Conditions);
        serviceCollection.AddSingleton(dalamudServices.GameGui);
        serviceCollection.AddSingleton(dalamudServices.GameConfig);

        // Core / Misc
        serviceCollection.AddSingleton<Mediator>();
        serviceCollection.AddSingleton<EventBus>();
        serviceCollection.AddSingleton<DalamudService>();
        serviceCollection.AddSingleton<ConfigurationService>();
        serviceCollection.AddSingleton<ResourceProvider>();
        serviceCollection.AddSingleton<GameDataProvider>();
        serviceCollection.AddSingleton<WelcomeService>();
        serviceCollection.AddSingleton<InputManagerService>();
        serviceCollection.AddSingleton<SceneService>();
        serviceCollection.AddSingleton<ProjectSystem>();
        serviceCollection.AddSingleton<AutoSaveService>();
        serviceCollection.AddSingleton<HistoryService>();
        serviceCollection.AddSingleton<FileCacheService>();
        serviceCollection.AddSingleton<MCDFService>();
        serviceCollection.AddSingleton<TransientResourceService>();
        serviceCollection.AddSingleton<ActorLookAtService>();
        serviceCollection.AddSingleton<CharacterHandlerService>();
        serviceCollection.AddSingleton<LightingService>();

        // API & Web
        serviceCollection.AddSingleton<BrioAPIService>();
        serviceCollection.AddSingleton<IPCProviders>();
        serviceCollection.AddSingleton<WebService>();

        serviceCollection.AddSingleton<StateAPI>();
        serviceCollection.AddSingleton<ActorAPI>();
        serviceCollection.AddSingleton<EnvironmentAPI>();
        serviceCollection.AddSingleton<PosingAPI>();
        serviceCollection.AddSingleton<AnimationAPI>();

        // IPC
        serviceCollection.AddSingleton<IPCManager>();
        serviceCollection.AddSingleton<DynamisService>();
        serviceCollection.AddSingleton<PenumbraService>();
        serviceCollection.AddSingleton<GlamourerService>();
        serviceCollection.AddSingleton<CustomizePlusService>();
        serviceCollection.AddSingleton<KtisisService>();

        // Entity
        serviceCollection.AddSingleton<EntityManager>();
        serviceCollection.AddSingleton<EntityActorManager>();

        // Game
        serviceCollection.AddSingleton<TargetService>();
        serviceCollection.AddSingleton<ActorSpawnService>();
        serviceCollection.AddSingleton<ActorRedrawService>();
        serviceCollection.AddSingleton<ActorAppearanceService>();
        serviceCollection.AddSingleton<ActorVFXService>();
        serviceCollection.AddSingleton<ActionTimelineService>();
        serviceCollection.AddSingleton<GPoseService>();
        serviceCollection.AddSingleton<CommandHandlerService>();
        serviceCollection.AddSingleton<ModelTransformService>();
        serviceCollection.AddSingleton<TimeService>();
        serviceCollection.AddSingleton<EnvironmentService>();
        serviceCollection.AddSingleton<FestivalService>();
        serviceCollection.AddSingleton<WorldRenderingService>();
        serviceCollection.AddSingleton<SkeletonService>();
        serviceCollection.AddSingleton<PosingService>();
        serviceCollection.AddSingleton<IKService>();
        serviceCollection.AddSingleton<CameraService>();
        serviceCollection.AddSingleton<ObjectMonitorService>();
        serviceCollection.AddSingleton<PhysicsService>();
        serviceCollection.AddSingleton<GameInputService>();
        serviceCollection.AddSingleton<VirtualCameraManager>();
        serviceCollection.AddSingleton<CutsceneManager>();

        // Library
        serviceCollection.AddSingleton<FileTypeInfoBase, AnamnesisCharaFileInfo>();
        serviceCollection.AddSingleton<FileTypeInfoBase, CMToolPoseFileInfo>();
        serviceCollection.AddSingleton<FileTypeInfoBase, PoseFileInfo>();
        serviceCollection.AddSingleton<FileTypeInfoBase, MareCharacterDataFileInfo>();
        serviceCollection.AddSingleton<FileTypeInfoBase, SceneFileInfo>();
        serviceCollection.AddSingleton<FileService>();

        serviceCollection.AddSingleton<SourceBase, GameDataNpcSource>();
        serviceCollection.AddSingleton<SourceBase, GameDataMountSource>();
        serviceCollection.AddSingleton<SourceBase, GameDataOrnamentSource>();
        serviceCollection.AddSingleton<SourceBase, GameDataCompanionSource>();

        serviceCollection.AddSingleton<LibraryManager>();

        // UI
        serviceCollection.AddSingleton<UIManager>();

        serviceCollection.AddSingleton<MainWindow>();
        serviceCollection.AddSingleton<SettingsWindow>();
        serviceCollection.AddSingleton<ProjectWindow>();
        serviceCollection.AddSingleton<UpdateWindow>();
        serviceCollection.AddSingleton<LibraryWindow>();
        serviceCollection.AddSingleton<ActorAppearanceWindow>();
        serviceCollection.AddSingleton<ActionTimelineWindow>();
        serviceCollection.AddSingleton<PosingOverlayWindow>();
        serviceCollection.AddSingleton<KeyBindPromptWindow>();
        serviceCollection.AddSingleton<PosingOverlayToolbarWindow>();
        serviceCollection.AddSingleton<PosingTransformWindow>();
        serviceCollection.AddSingleton<CameraWindow>();
        serviceCollection.AddSingleton<AutoSaveWindow>();
        serviceCollection.AddSingleton<MCDFWindow>();
        serviceCollection.AddSingleton<PosingGraphicalWindow>();
        serviceCollection.AddSingleton<LightWindow>();

        return serviceCollection;
    }

    public static bool TryGetService<T>(out T Tvalue) where T : notnull
    {
        if(_services is not null)
        {
            try
            {
                Tvalue = _services.GetRequiredService<T>();
                return true;
            }
            catch
            {
            }
        }

        Tvalue = default!;
        return false;
    }

    public static void NotifyError(string message)
    {
        EventBus.Instance.NotifyError(message);
    }

    //
    // The following methods are inspired by similar methods in Penumbra for gathering debug info.
    public static string GetDebugInfo()
    {
        var configService = _services!.GetService<ConfigurationService>();
        var config = configService!.Configuration;

        var plugininterface = _services!.GetService<IDalamudPluginInterface>();
        var gamedata = _services!.GetService<IDataManager>();

        if(configService is null && config is null && gamedata is null && plugininterface is null)
            return "Could not retrieve debug information.";

        StringBuilder sb = new();

        sb.AppendLine("## Brio Support Information");
        sb.AppendLine();
        sb.AppendLine($"> **`Brio Version:              `** {ConfigurationService.s_version}");
        sb.AppendLine($"> **`API Version:               `** {MajorAPIVersion}.{MinorAPIVersion}");
        sb.AppendLine($"> **`Dalamud Version:           `** {plugininterface!.GetDalamudVersion().ToString()}");
        sb.AppendLine($"> **`Game Data:                 `** {(gamedata!.HasModifiedGameDataFiles ? "Modified" : "Unmodified")}");
        sb.AppendLine($"> **`Operating System:          `** {(Dalamud.Utility.Util.IsWine() ? "Wine" : "Windows")}");
        sb.AppendLine($"> **`CommitHash:                `** {configService!.CommitHash}");
        sb.AppendLine($"> **`IsFromTrustedSource:       `** {configService!.IsFromTrustedSource}");
        sb.AppendLine($"> **`IsDevPlugin:               `** {plugininterface!.IsDev}");
        sb.AppendLine($"> **`IsTesting:                 `** {plugininterface!.IsTesting}");
        sb.AppendLine($"> **`IsDebug:                   `** {configService!.IsDebug}");

        sb.AppendLine("### Display");
        GatherDisplayInfo(sb);

        sb.AppendLine("### Plugins");
        GatherRelevantPlugins(sb);

        sb.AppendLine("### Configuration");
        if(config is not null)
        {
            sb.AppendLine($"> **`Version:                   `** {config!.Version}");
            sb.AppendLine($"> **`PopupKey:                  `** {config!.PopupKey}");
            sb.AppendLine($"> **`ForceDebug:                `** {config!.ForceDebug}");

            sb.AppendLine($"> **`AutoSave Enabled:          `** {config!.AutoSave.AutoSaveSystemEnabled}");

            sb.AppendLine($"> **`AllowWebAPI:               `** {config!.IPC.AllowWebAPI}");
            sb.AppendLine($"> **`IPC Enabled:               `** {config!.IPC.EnableBrioIPC}");

            sb.AppendLine($"> **`BrioStyle:                 `** {config!.Appearance.EnableBrioStyle}");
            sb.AppendLine($"> **`BrioColor:                 `** {config!.Appearance.EnableBrioColor}");
            sb.AppendLine($"> **`BrioScale:                 `** {config!.Appearance.EnableBrioScale}");

            sb.AppendLine($"> **`Saved Library:             `** {config!.Library.Files.Count}");
        }
        else
        {
            sb.AppendLine("> Could not retrieve configuration.");
        }

        return sb.ToString();
    }

    private static void GatherDisplayInfo(StringBuilder sb)
    {
        try
        {
            var screens = new List<(int Width, int Height, bool IsPrimary)>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX info = new()
                {
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFOEX>()
                };

                if(GetMonitorInfo(hMonitor, ref info))
                {
                    int width = info.rcMonitor.right - info.rcMonitor.left;
                    int height = info.rcMonitor.bottom - info.rcMonitor.top;
                    bool isPrimary = (info.dwFlags & 1) != 0;
                    screens.Add((width, height, isPrimary));
                }

                return true;
            }, IntPtr.Zero);

            sb.AppendLine($"> **`Screen Count:              `** {screens.Count}");

            for(int i = 0; i < screens.Count; i++)
            {
                var (Width, Height, IsPrimary) = screens[i];
                var isPrimary = IsPrimary ? " (Primary)" : "";
                sb.AppendLine($"> **`Screen {i + 1}:                 `** {Width}x{Height}{isPrimary}");
            }
        }
        catch(Exception ex)
        {
            sb.AppendLine($"> **`Screen Count:              `** Error retrieving display info: {ex.Message}");
        }
    }
    private static void GatherRelevantPlugins(StringBuilder sb)
    {
        ReadOnlySpan<string> relevantPlugins =
        [
            "Glamourer", "Penumbra", "CustomizePlus", "VfxEditor", "Ktisis",
            "IllusioVitae", "Aetherment", "GagSpeak", "ProjectGagSpeak", "RoleplayingVoiceDalamud", "AQuestReborn",
            "LoporritSync", "HonseFarm.Client", "LightlessSync", "Snowcloak", "MareSempiterne"
        ];

        var plugins = _services?.GetService<IDalamudPluginInterface>()?.InstalledPlugins
            .GroupBy(p => p.InternalName)
            .ToDictionary(g => g.Key, g =>
            {
                var item = g.OrderByDescending(p => p.IsLoaded).ThenByDescending(p => p.Version).First();
                return (item.IsLoaded, item.Version, item.Name);
            });

        if(plugins is not null)
        {
            foreach(var plugin in relevantPlugins)
            {
                if(plugins.TryGetValue(plugin, out var data))
                    sb.Append($"> **`{data.Name + ':',-29}`** {data.Version}{(data.IsLoaded ? string.Empty : " (Disabled)")}\n");
            }
        }
    }

    public void Dispose()
    {
        _services?.Dispose();

        GC.SuppressFinalize(this);
    }
}



// -------------------------------------------------------------
//
//  Brio was made by Asgard, Thank you advancement of posing tools in FFXIV!
//  Happy Third Anniversary Brio!
//
// -------------------------------------------------------------

/*                                                                                                                                                
                                                                                                                                                 
                                                                                                 +#                                              
                                                                                              ++++#                                              
                                                                      -+++++++ +           ++++++##                                              
                                              +-                  -++++--+++++#++++  +   +++++++##                                               
                                              +++++++        +++++++--+++++++++++##+++#++++++#####                                               
                                              +#+++++++++  ++-++++++##+++++++++#####+###+#########                                               
                                               +#+++++++--+++++++++###++++++++++#####+#+#########                                                
                                                +#++++++++++++++++#++++-++++++########++#++#####                                                 
                                                 ##+++++++++++++++++---++++++#+######++++##++###                                                 
                                                  ###+++++++++++++---+++++++###+#####++++#######                                                 
                                                    ##+++++++++++--++++++++##+++######++#######                                                  
                                                    -+#++##+++++-++++++++##++++####++#+#######                                                   
                                                      +++#+-+++++##+++++##+++++###++++########                                                   
                                                     +++#+-+++++###+++###++++++###++++########                                                   
                                                    -++++--+++++##++#####+++++###++++++#######                                                   
                                                   -+++++-+++++##+++##+#++++++###++++++########                                                  
                                                   ++ +++-++#++#+++#####+++#+########+++#######                                                  
                                                      +++++##++#+++#####++#++##+###+++++##### +                                                  
                                                      ++++###+##++####+#++#++++++++++++++####                                                    
                                                      ++++###+##++#++++#+#+++++++++++++++####                                                    
                                                     ++++#+#####+#++++++++++++++++++++++++###                                                    
                                                    ++++##++####+#++++++++++++++++++++++++###                                                    
                                                    +  ####+####+++++++++++++++++++++++++####                                                    
                                                       +###+####++++++++++++++++++++++++###                                                      
                                                       +#  +####++++++++++##++##++++++++###                                                      
                                                           +#####+++++++###+#+++++++++++###                                                      
                                         --...--.....-....--######++++++++++++++++++++++###                                                      
                                       -.......--.....--.......-------+++++++++++++++++++#                                                       
                                      -..---...-+--....-----..---.--..-++++++++++++++++++#                                                       
                                     -.---+--..--++--...-+-----++++----+++######+++++++++#####                                                   
                                   -..---++++---++++----+++---+++++---++++++##+++++++++++######+                                                 
                                   -.---+++++-++++++---++++---++++---+++++++++++-------..---+###++                                               
                                  -..---++++++++++++-++++++++++++++++++++++-----------...-----.......--                                          
                                  -..---++++++++++++++++++++++++++++++++++--++++++--.-----+-...----......--                                      
                                 -..----+++++++++++++++++++++++++++++++++++-+++-.---++++++-..---+--..-.------                                    
                                -...---+++++++++++++++++++++++++++++++#++#+-+++--+++++++-..---+++-.--+++---++++                                  
                              -....----+++++++++++#####++--++++++++++++##--++++--++++++-.--+++++---++++++++++++#####++                           
                             -...------+++++++++#######++++++++++++++++##+++##++-++++----++++++.--++++++++++++++###++++++                        
                           +-....-----++++++++++######+++++++++++++#++#########+++++--+++++++----++++++++++++++++#####+++++                      
                         ++-.....----++++++++++######++++++##+#++###############++++++++++++---++++++++++++++++++#######++++                     
                        ++-....-----++++++++++######+++###++######################++++++++++++++++++++++++++++++++######+++++                    
                     ++++-....-----++++++++++#######+++####################+++++########+++++#######+++++++++++++++####+++++++                   
                    ++++-....-----++++++++++################################+----+++######++#########+++++++++++++++++++++++++##                 
                   ++++-....-----++++++++++##################################+-----+########+#########++++++++++++++++++++++++#+                 
                 +++++-....------+++++++++####################################+--+++#+++++############++++++++++++++++++++++####+                
           +++-++++++-....------+++++++++######################################+-++++++###+--+#########+++++++++++++++++#########                
        -+++++++++++.....------+++++++++#######################################++--#+++++++---++#######++++++++++++++++++#######++               
      -+++++++++++-.....------+++++++++################################+++#####+++-+++++++-------++#####+++++++++++++++++########++              
    +++++++++++++-.....------++++++++++########################++#############+++++++-++###+#+---++#####+++++++++++++++++-##########             
  ++++++++++++++-.....-------+++++++++##################++######++++#+########+++++#+++#++##++---+++#####+++++++++++++++++-+#########            
 ++++++++######-.....--------+++++++++########################################++++++++#++##++-----++#####+++++++++++++++++++++++++##++           
 +++++#+++##++.....---------+++++++++##############################+###+######++++++++++##++------+++####++++++++++++++++++++++++++++++++        
 ++++++###+#+....----------+++++++++###################################+++####++++++++++++++------+++#####+++++++++++++++++++++##########+++-    
 +#+++++#+++-..-----------++++++++++####################+##############++++###+++---++++++++------++#######++++++++++++++++++++############+++   
++#++++#++++++----------+++++++++++####################+++####################+++------++++++----+++#######++++++++++++++++++++++############++  
++++++#+++++++++-------+++++++++++######################+#####################++++--------++++-++++#########++++++++++++++++++++-+###########++# 
+++++++++++++++##+++-+++++++++++++############################################++++-----------+-++++#########++++++++++++++++++++++######++###+###
 #####+++++++++#+#####+++++++++++#############################################++++--------------+++#########+++++++++++++++++++++++########++++##
  ##++++++++++#############++++###############################################+++++-------------+++######+###+++++++++++++++++++++++++######+++##
   +++++++++##################################################################+++++------------++++##########++++++++++++++++++++++++++#####+### 
    ++###++###################################################################+++++------------++++###########++++++++++++++++++++##+########### 
     ######+##################################################################+++++----------++++++###########+++++++++++++++++++#####+###+####+ 
      +#####++########################### ####################################++++++--++----+++++++#####+######+++++++++++++++########+######### 
         ######+########################  ####################################+++++--------++++-++++#############+++++++++#############+#######  
           #######+++#################    #######################################+#+---++-+++++++#+++#################################+#######   
              #####################+      +###################################++#+#+#+#-+++--#+##++#+################################++######    
                 ###############          +###################################+#+++++--------------++++#############################+######      
                                           ###################################+++++++++--+++-++++++++++++########################++#######       
                                           ###################################++++++++++++++-+++++++++++++####################++########         
                                           +##############################+###++++++++++++++--+++++++++++++###############+#########             
                                           ###################################++++++++++++++--+++++++++++++#####################                 
                                           ####################################++++++++++++++-+++++++++++++#################                     
                                           #+##################################+++++++++++++++++++++++++++++##########+                          
                                           +###################################+++++++++++++++++++++++++++++##########+                          
                                          ++###################################+++++++++++++++++++++++++++++###########                          
                                          ++###################################++++++++++++++++++++++++++++++##########+                         
                                          ++###################################++++++++++++++++++++++++++++++##########+                         
                                                                                                                                                                                                                                                                                                  
*/
