using Brio.Game.GPose;
using Dalamud.Logging;
using Penumbra.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using System;

namespace Brio.IPC;

public class PenumbraIPC : IDisposable
{
    public bool IsPenumbraEnabled { get; private set; } = false;

    public delegate void OnPenumbraStateChangeDelegate(bool isActive);
    public event OnPenumbraStateChangeDelegate? OnPenumbraStateChange;

    private EventSubscriber _penumbraInitializedSubscriber;
    private EventSubscriber _penumbraDisposedSubscriber;

    public PenumbraIPC()
    {
        _penumbraInitializedSubscriber = Ipc.Initialized.Subscriber(Dalamud.PluginInterface, RefreshPenumbraStatus);
        _penumbraDisposedSubscriber = Ipc.Disposed.Subscriber(Dalamud.PluginInterface, RefreshPenumbraStatus);

        RefreshPenumbraStatus();

        Brio.GPoseService.OnGPoseStateChange += GPoseService_OnGPoseStateChange;
    }

    public void RefreshPenumbraStatus() 
    {
        var wasEnabled = IsPenumbraEnabled;

        if (Brio.Configuration.AllowPenumbraIntegration)
        {
            IsPenumbraEnabled = CanConnect();
        }
        else
        {
            IsPenumbraEnabled = false;
        }

        if (wasEnabled != IsPenumbraEnabled)
            OnPenumbraStateChange?.Invoke(IsPenumbraEnabled);
    }

    private bool CanConnect()
    {
        try
        {
            bool penumInstalled = Dalamud.PluginInterface.PluginNames.Contains("Penumbra");
            if (!penumInstalled)
            {
                PluginLog.Information("Penumbra not present");
                return false;
            }

            var (major, minor) = Ipc.ApiVersions.Subscriber(Dalamud.PluginInterface).Invoke();
            if (major != 4 || minor < 18)
            {
                PluginLog.Information("Penumbra API mismatch");
                return false;
            }

            PluginLog.Information("Penumbra integration initialized");

            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Information(ex, "Penumbra initialize error");
            return false;
        }
    }

    private void GPoseService_OnGPoseStateChange(GPoseState state)
    {

        switch (state)
        {
            case GPoseState.Inside:
                RefreshPenumbraStatus();
                break;
        }
    }


    public void Dispose()
    {
        Brio.GPoseService.OnGPoseStateChange -= GPoseService_OnGPoseStateChange;
        _penumbraDisposedSubscriber.Dispose();
        _penumbraInitializedSubscriber.Dispose();
        IsPenumbraEnabled = false;
    }
}
