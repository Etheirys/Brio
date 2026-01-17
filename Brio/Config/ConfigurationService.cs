using Dalamud.Plugin;
using System;
using System.Reflection;

namespace Brio.Config;

public class ConfigurationService : IDisposable
{

    public const string WorldOfEtheirysRepo = "https://raw.githubusercontent.com/etheirys/worldofetheirys/main/repo.json";
    public const string SeaOfStarsRepo = "https://raw.githubusercontent.com/ottermandias/seaofstars/main/repo.json";
    public const string BrioRepo = "https://raw.githubusercontent.com/etheirys/brio/main/repo.json";

    public Configuration Configuration { get; private set; } = null!;

    private readonly IDalamudPluginInterface _pluginInterface;

    public delegate void OnConfigurationChangedDelegate();
    public event OnConfigurationChangedDelegate? OnConfigurationChanged;

    public static ConfigurationService Instance { get; private set; } = null!;

    public string ConfigDirectory => _pluginInterface.ConfigDirectory.FullName;

    public ConfigurationService(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        _pluginInterface = pluginInterface;
        Configuration = _pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
    }

    public void Save()
    {
        _pluginInterface.SavePluginConfig(Configuration);
    }

    public void ApplyChange(bool save = true)
    {
        if(save)
            Save();

        OnConfigurationChanged?.Invoke();
    }

    public void Reset()
    {
        Configuration = new Configuration();

        ApplyChange();
    }

    public void Dispose()
    {
        Save();
    }

#if DEBUG
    private static bool s_isDebug => true;
#else
    private static bool s_isDebug => false;
#endif

    public static readonly string s_version = typeof(Brio).Assembly.GetName().Version?.ToString() ?? "(Unknown Version)";

    public bool IsDebug => s_isDebug || Configuration.ForceDebug;
    public string Version => IsDebug ? "(Debug)" : $"v{s_version}";
    public string CommitHash => GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";

    public bool IsFromTrustedSource => !IsDebug || IsTrustedRepo(_pluginInterface);

    private static bool IsTrustedRepo(IDalamudPluginInterface pi) =>
        pi.SourceRepository?.Trim().ToLowerInvariant() switch
        {
            null => false,
            WorldOfEtheirysRepo => true,
            SeaOfStarsRepo => true,
            BrioRepo => true,
            _ => false,
        };
}
