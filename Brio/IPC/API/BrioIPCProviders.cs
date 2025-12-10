using Brio.API;
using Brio.API.Helpers;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace Brio.IPC.API;

public class BrioIPCProviders : IDisposable
{
    private readonly List<IDisposable> _providers;

    private readonly EventProvider _disposedProvider;
    private readonly EventProvider _initializedProvider;

    public BrioIPCProviders(IDalamudPluginInterface pi, BrioAPIService brioAPI)
    {
        _disposedProvider = Disposed.Provider(pi);
        _initializedProvider = Initialized.Provider(pi);

        _providers = [
            ApiVersion.Provider(pi, brioAPI.State),
            IsAvailable.Provider(pi, brioAPI.State),

            SpawnActor.Provider(pi, brioAPI.Actor),
            DespawnActor.Provider(pi, brioAPI.Actor),
            ActorExists.Provider(pi, brioAPI.Actor),
            GetAllActors.Provider(pi, brioAPI.Actor),

            SetActorSpeed.Provider(pi, brioAPI.Animation),
            GetActorSpeed.Provider(pi, brioAPI.Animation),
            FreezeActor.Provider(pi, brioAPI.Animation),
            UnFreezeActor.Provider(pi, brioAPI.Animation),

            FreezePhysics.Provider(pi, brioAPI.Environment),
            UnFreezePhysics.Provider(pi, brioAPI.Environment),

            SetModelTransform.Provider(pi, brioAPI.Posing),
            GetModelTransform.Provider(pi, brioAPI.Posing),
            ResetModelTransform.Provider(pi, brioAPI.Posing),
            LoadPoseFromFile.Provider(pi, brioAPI.Posing),
            LoadPoseFromJson.Provider(pi, brioAPI.Posing),
            GetPoseAsJson.Provider(pi, brioAPI.Posing),
            ResetPose.Provider(pi, brioAPI.Posing)
        ];

        _initializedProvider.Invoke();
    }

    public void Dispose()
    {
        foreach(var provider in _providers)
        {
            provider.Dispose();
        }

        _initializedProvider.Dispose();
        _disposedProvider.Invoke();
        _disposedProvider.Dispose();
    }
}
