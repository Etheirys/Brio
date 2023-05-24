using Brio.Config;
using Brio.Core;
using Brio.Game.GPose;
using Dalamud.Logging;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using System;
using System.Linq;

namespace Brio.IPC;

public class PenumbraIPCService : ServiceBase<PenumbraIPCService>
{
    public bool IsPenumbraEnabled { get; private set; } = false;

    public delegate void OnPenumbraStateChangeDelegate(bool isActive);
    public event OnPenumbraStateChangeDelegate? OnPenumbraStateChange;

    private EventSubscriber _penumbraInitializedSubscriber;
    private EventSubscriber _penumbraDisposedSubscriber;

    public PenumbraIPCService()
    {
        _penumbraInitializedSubscriber = Ipc.Initialized.Subscriber(Dalamud.PluginInterface, RefreshPenumbraStatus);
        _penumbraDisposedSubscriber = Ipc.Disposed.Subscriber(Dalamud.PluginInterface, RefreshPenumbraStatus);
    }

    public override void Start()
    {
        RefreshPenumbraStatus();

        GPoseService.Instance.OnGPoseStateChange += GPoseService_OnGPoseStateChange;

        base.Start();
    }

    public void RefreshPenumbraStatus()
    {
        var wasEnabled = IsPenumbraEnabled;

        if(ConfigService.Configuration.AllowPenumbraIntegration)
        {
            IsPenumbraEnabled = CanConnect();
        }
        else
        {
            IsPenumbraEnabled = false;
        }

        if(wasEnabled != IsPenumbraEnabled)
            OnPenumbraStateChange?.Invoke(IsPenumbraEnabled);
    }

    private bool CanConnect()
    {
        try
        {
            bool penumInstalled = Dalamud.PluginInterface.InstalledPlugins.Count(x => x.Name == "Penumbra") > 0;
            if(!penumInstalled)
            {
                PluginLog.Information("Penumbra not present");
                return false;
            }

            var (major, minor) = Ipc.ApiVersions.Subscriber(Dalamud.PluginInterface).Invoke();
            if(major != 4 || minor < 18)
            {
                PluginLog.Information("Penumbra API mismatch");
                return false;
            }

            PluginLog.Information("Penumbra integration initialized");

            return true;
        }
        catch(Exception ex)
        {
            PluginLog.Information(ex, "Penumbra initialize error");
            return false;
        }
    }

    private void GPoseService_OnGPoseStateChange(GPoseState state)
    {

        switch(state)
        {
            case GPoseState.Inside:
                RefreshPenumbraStatus();
                break;
        }
    }

    public override void Stop()
    {
        GPoseService.Instance.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
        IsPenumbraEnabled = false;
    }

    public override void Dispose()
    {
        _penumbraDisposedSubscriber.Dispose();
        _penumbraInitializedSubscriber.Dispose();
    }
}
