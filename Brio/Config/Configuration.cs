using Dalamud.Configuration;

namespace Brio.Config;

public class Configuration : IPluginConfiguration
{
    public const int CurrentVersion = 3;
    public const int CurrentPopupKey = 16;

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

    public SceneImportConfiguration Import { get; set; } = new SceneImportConfiguration();

    // Library
    public LibraryConfiguration Library { get; set; } = new LibraryConfiguration();
  
    // MCDF Data
    public MCDFConfiguration MCDF { get; set; } = new MCDFConfiguration();

    public string LastMCDFPath { get; set; } = string.Empty;
    public string LastExportPath { get; set; } = string.Empty;
    public string LastXATPath { get; set; } = string.Empty;

    public string LastScenePath { get; set; } = string.Empty;

    public bool UseLibraryWhenImporting { get; set; } = true;

    public bool SceneDestoryActorsBeforeImport { get; set; } = false;

    // Input
    public InputManagerConfiguration InputManager { get; set; } = new InputManagerConfiguration();

    // AutoSave
    public AutoSaveConfiguration AutoSave { get; set; } = new AutoSaveConfiguration();

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
