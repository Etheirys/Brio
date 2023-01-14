using Brio.Game.Actor;
using Brio.Game.Chat;
using Brio.Game.GPose;
using Brio.UI.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using System;

namespace Brio;

public static class Brio
{
    public const string PluginName = "Brio";

    public static WindowSystem WindowSystem { get; private set; } = null!;

    public static GPoseService GPoseService { get; private set; } = null!;
    public static ActorSpawnService ActorSpawnService { get; private set; } = null!;


    private static CommandHandler CommandHandler { get; set; } = null!;
    private static BrioWindow BrioWindow { get; set; } = null!;

    public static void Initialize()
    {
        WindowSystem = new(PluginName);

        CommandHandler = new();

        GPoseService = new GPoseService();
        ActorSpawnService  = new ActorSpawnService();


        BrioWindow = new();
        WindowSystem.AddWindow(BrioWindow);
    }

    public static void Destroy()
    {
        CommandHandler.Dispose();

        ActorSpawnService.Dispose();
        GPoseService.Dispose();

        WindowSystem.RemoveWindow(BrioWindow);
    }

    public static void Toggle()
    {
        BrioWindow.Toggle();
    }
}