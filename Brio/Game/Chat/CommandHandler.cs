using Dalamud.Game.Command;
using System;

namespace Brio.Game.Chat;

public class CommandHandler : IDisposable
{
    private const string CommandName = "/brio";

    public CommandHandler()
    {
        Dalamud.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the Brio window.",
            ShowInHelp = true,
        });
    }

    private void OnCommand(string command, string arguments)
    {
        Brio.Toggle();
    }

    public void Dispose()
    {
        Dalamud.CommandManager.RemoveHandler(CommandName);
    }
}
