using Brio.Core;
using Brio.UI;
using Dalamud.Game.Command;
using System;

namespace Brio.Game.Chat;

public class CommandHandlerService : ServiceBase<CommandHandlerService>
{
    private const string CommandName = "/brio";

    public override void Start()
    {
        Dalamud.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggles the Brio window.",
            ShowInHelp = true,
        });

        base.Start();
    }

    private void OnCommand(string command, string arguments)
    {
        UIService.Instance.MainWindow.Toggle();
    }

    public override void Dispose()
    {
        Dalamud.CommandManager.RemoveHandler(CommandName);
    }
}
