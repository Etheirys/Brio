using Brio.Core;
using Brio.UI;
using Dalamud.Game.Command;
using System;

namespace Brio.Game.Chat;

public class CommandHandlerService : ServiceBase<CommandHandlerService>
{
    private const string CommandName = "/brio";

    public CommandHandlerService()
    {
        Dalamud.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the Brio window.",
            ShowInHelp = true,
        });

    }

    private void OnCommand(string command, string arguments)
    {
        if(arguments.Length == 0)
            arguments = "window";

        var argumentList = arguments.Split(' ', 2);
        arguments = argumentList.Length == 2 ? argumentList[1] : string.Empty;

        switch(argumentList[0].ToLowerInvariant())
        {
            case "window":
                UIService.Instance.MainWindow.Toggle();
                break;

            case "settings":
                UIService.Instance.SettingsWindow.Toggle();
                break;

            case "about":
                UIService.Instance.InfoWindow.Toggle();
                break;

            case "help":
            default:
                PrintHelp();
                break;
        }
            
    }

    private void PrintHelp()
    {
        Dalamud.ChatGui.Print("Valid Brio Commands Are:");
        Dalamud.ChatGui.Print("<none> - Toggle main Brio window");
        Dalamud.ChatGui.Print("window - Toggle main Brio window");
        Dalamud.ChatGui.Print("settings - Toggle Brio settings window");
        Dalamud.ChatGui.Print("about - Toggle Brio info window");
        Dalamud.ChatGui.Print("help - Print this help prompt");
    }

    public override void Dispose()
    {
        Dalamud.CommandManager.RemoveHandler(CommandName);
    }
}
