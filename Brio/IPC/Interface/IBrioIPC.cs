using Dalamud.Plugin;
using System;
using System.Linq;

namespace Brio.IPC;

public interface IBrioIPC : IDisposable
{
    string Name { get; }
    bool IsAvailable { get; }
    bool AllowIntegration { get; }

    public IPCStatus CheckStatus(bool force = false);
    (int Major, int Minor) GetAPIVersion();
    IDalamudPluginInterface GetPluginInterface();

    int APIMajor { get; }
    int APIMinor { get; }
}

public enum IPCStatus
{
    None = 0,
    Available = 1,
    Disabled = 2,
    NotInstalled = 3,
    VersionMismatch = 4,
    Error = 5,

    Unavailable = None | Disabled | NotInstalled | VersionMismatch | Error
}

public abstract class BrioIPC : IBrioIPC
{
    public abstract string Name { get; }
    public abstract bool IsAvailable { get; }
    public abstract bool AllowIntegration { get; }

    public virtual bool Disabled { get; set; } = false;

    public abstract void Dispose();
    public abstract (int Major, int Minor) GetAPIVersion();
    public abstract IDalamudPluginInterface GetPluginInterface();

    public abstract int APIMajor { get; }
    public abstract int APIMinor { get; }

    TimeSpan _interval = TimeSpan.FromSeconds(10);
    DateTime _lastCheckTime = DateTime.MinValue;
    IPCStatus _lastIPCStatus = IPCStatus.None;
    public IPCStatus CheckStatus(bool force = false)
    {
        if(force == false)
        {
            if(AllowIntegration == false || Disabled)
            {
                return IPCStatus.Disabled;
            }

            if(_lastIPCStatus != IPCStatus.None && (DateTime.Now - _lastCheckTime < _interval))
            {
                return _lastIPCStatus;
            }
        }
        _lastCheckTime = DateTime.Now;

        try
        {
            bool installed = GetPluginInterface().InstalledPlugins.Any(x => x.Name == Name && x.IsLoaded == true);
            if(!installed)
            {
                Brio.Log.Debug($"{Name} not present");
                return _lastIPCStatus = IPCStatus.NotInstalled;
            }

            var (major, minor) = GetAPIVersion();
            if(major != APIMajor || minor < APIMinor)
            {
                Brio.Log.Warning($"{Name} API Version mismatch, found v{major}.{minor}");
                return _lastIPCStatus = IPCStatus.VersionMismatch;
            }
            return _lastIPCStatus = IPCStatus.Available;
        }
        catch(Exception ex)
        {
            Brio.Log.Debug(ex, $"{Name} initialize error");
            return _lastIPCStatus = IPCStatus.Error;
        }
    }
}
