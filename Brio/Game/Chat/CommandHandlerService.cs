using Brio.UI;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.Chat;

public class CommandHandlerService : IDisposable
{
    private const string BrioCommandName = "/brio";
    private const string XATCommandName = "/xat";
    private const string MCDFCommandName = "/mcdf";

    private readonly ICommandManager _commandManager;
    private readonly IChatGui _chatGui;
    private readonly UIManager _uiManager;

    public CommandHandlerService(ICommandManager commandManager, IChatGui chatGui, UIManager uiManager)
    {
        _commandManager = commandManager;
        _chatGui = chatGui;
        _uiManager = uiManager;

        _commandManager.AddHandler(BrioCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the Brio window.",
            ShowInHelp = true,
        });
        _commandManager.AddHandler(XATCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the Brio window.",
            ShowInHelp = false,
        });
        _commandManager.AddHandler(MCDFCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles Brio's MCDF window.",
            ShowInHelp = true,
        });
    }

    private void OnCommand(string command, string arguments)
    {
        if(arguments.Length == 0)
            arguments = "window";

        var argumentList = arguments.Split(' ', 2);

        switch(argumentList[0].ToLowerInvariant())
        {
            case "window":
                _uiManager.ToggleMainWindow();
                break;

            case "settings":
                _uiManager.ToggleSettingsWindow();
                break;

            case "about":
                _uiManager.ToggleInfoWindow();
                break;

            case "help":
            default:
                PrintHelp();
                break;
        }

    }

    private void PrintHelp()
    {
        _chatGui.Print("Valid Brio Commands Are:");
        _chatGui.Print("<none> - Toggle main Brio window");
        _chatGui.Print("window - Toggle main Brio window");
        _chatGui.Print("settings - Toggle Brio settings window");
        _chatGui.Print("about - Toggle Brio info window");
        _chatGui.Print("help - Print this help prompt");
    }

    public void Dispose()
    {
        _commandManager.RemoveHandler(BrioCommandName);
        _commandManager.RemoveHandler(XATCommandName);
    }
}
