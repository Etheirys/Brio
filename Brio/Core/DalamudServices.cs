using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Brio.Core;

public class DalamudServices
{
    [PluginService] public IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public IFramework Framework { get; private set; } = null!;
    [PluginService] public IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] public IClientState ClientState { get; private set; } = null!;
    [PluginService] public ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public IDataManager DataManager { get; private set; } = null!;
    [PluginService] public ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public IToastGui ToastGui { get; private set; } = null!;
    [PluginService] public ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public IPluginLog Log { get; private set; } = null!;
    [PluginService] public IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public IKeyState KeyState { get; private set; } = null!;
    [PluginService] public ICondition Conditions { get; private set; } = null!;
    [PluginService] public IGameConfig GameConfig { get; private set; } = null!;


    public DalamudServices(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Inject(this);
    }
}
