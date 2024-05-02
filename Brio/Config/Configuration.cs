using Dalamud.Configuration;

namespace Brio.Config;

internal class Configuration : IPluginConfiguration
{
    public const int CurrentVersion = 2;
    public const int CurrentPopupKey = 4;

    public int Version { get; set; } = CurrentVersion;

    // First Time User
    public int PopupKey { get; set; } = -1;

    // Interface
    public InterfaceConfiguration Interface { get; set; } = new InterfaceConfiguration();

    // Posing
    public PosingConfiguration Posing { get; set; } = new PosingConfiguration();

    // IPC
    public IPCConfiguration IPC { get; set; } = new IPCConfiguration();

    // Appearance
    public AppearanceConfiguration Appearance { get; set; } = new AppearanceConfiguration();

    // Environment
    public EnvironmentConfiguration Environment { get; set; } = new EnvironmentConfiguration();

    // Input
    public InputConfiguration Input { get; set; } = new InputConfiguration();
 
    //
    //// KENTODO REMOVE
    //
    public PathsConfiguration Paths { get; set; } = new PathsConfiguration();

    // Developer
    public bool ForceDebug { get; set; } = false;
}

public enum OpenBrioBehavior
{
    Manual,
    OnGPoseEnter,
    OnPluginStartup
}

public enum ApplyNPCHack
{
    Disabled,
    InGPose,
    Always
}
