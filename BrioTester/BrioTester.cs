using BrioTester.Plugin.UI;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace BrioTester.Plugin;

public class BrioTester : IDalamudPlugin
{
    public const string PluginName = "BrioTester";
    public string Name => PluginName;

    private const string CommandName = "/btw";

    [PluginService]
    public ICommandManager CommandManager { get; init; } = null!;

    [PluginService]
    public IFramework Framework { get; init; } = null!;

    [PluginService]
    public ISigScanner SigScanner { get; init; } = null!;

    [PluginService]
    public IObjectTable ObjectTable { get; init; } = null!;

    [PluginService]
    public IGameInteropProvider GameInteropProvider { get; init; } = null!;

    [PluginService]
    public IClientState ClientState { get; init; } = null!;

    public IDalamudPluginInterface PluginInterface { get; }

    public BrioTesterWindow Window { get; }
    public WindowSystem WindowSystem { get; }

    public bool IsInGPose { get; private set; }

    public BrioTester(IDalamudPluginInterface pluginInterface)
    {
        this.PluginInterface = pluginInterface;
        this.PluginInterface.Inject(this);

        this.PluginInterface.UiBuilder.DisableGposeUiHide = true;
        this.PluginInterface.UiBuilder.DisableCutsceneUiHide = true;
        this.PluginInterface.UiBuilder.Draw += DrawUI;
        this.Framework.Update += Update;

        this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A very useful message!"
        });

        this.Window = new(Framework, pluginInterface);
        this.WindowSystem = new(PluginName);
        this.WindowSystem.AddWindow(Window);

        Window.IsOpen = true;
    }

    private void OnCommand(string command, string args)
    {
        Window? window = this.Window;

        if (window != null)
        {
            if (window.IsOpen)
            {
                window.IsOpen = false;
            }
            else
            {
                window.IsOpen = true;
            }
        }
    }

    private void DrawUI()
    {
        this.WindowSystem.Draw();
    }

    private void Update(IFramework framework)
    {
        if (ClientState.IsGPosing && IsInGPose == false)
        {
            IsInGPose = true;

            Window.IsOpen = true;
        }
        else if (ClientState.IsGPosing == false && IsInGPose)
        {
            IsInGPose = false;
        }
    }

    public void Dispose()
    {
        Framework.Update -= Update;

        this.CommandManager.RemoveHandler(CommandName);

        this.PluginInterface.UiBuilder.Draw -= DrawUI;

        this.WindowSystem.RemoveAllWindows();
    }
}