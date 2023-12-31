using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using Brio.UI.Windows;
using System;
using Brio.UI;

namespace Brio.Game.Chat;

internal class CommandHandlerService : IDisposable
{
    private const string CommandName = "/brio";

    private readonly ICommandManager _commandManager;
    private readonly IChatGui _chatGui;
    private readonly UIManager _uiManager;

    public CommandHandlerService(ICommandManager commandManager, IChatGui chatGui, UIManager uiManager)
    {
        _commandManager = commandManager;
        _chatGui = chatGui;
        _uiManager = uiManager;

        _commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the Brio window.",
            ShowInHelp = true,
        });
    }

    private void OnCommand(string command, string arguments)
    {
        if (arguments.Length == 0)
            arguments = "window";

        var argumentList = arguments.Split(' ', 2);
        arguments = argumentList.Length == 2 ? argumentList[1] : string.Empty;

        switch (argumentList[0].ToLowerInvariant())
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
        _commandManager.RemoveHandler(CommandName);
    }
}
