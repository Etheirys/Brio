using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.Core;

public class TargetService : IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly ITargetManager _targetManager;
    private readonly IFramework _framework;
    private readonly ConfigurationService _configService;

    public IGameObject? BrioTarget
    {
        get
        {
            if(_entityManager.SelectedEntity is ActorEntity actorEntity)
                return actorEntity.GameObject;
            return null;
        }
        set
        {
            if(value != null)
                _entityManager.SetSelectedEntity(value);
        }
    }

    public IGameObject? GPoseTarget
    {
        get => _targetManager.GPoseTarget;
        set => _targetManager.GPoseTarget = value;
    }

    private nint _lastBrioTarget = 0;
    private nint _lastGPoseTarget = 0;


    public TargetService(EntityManager entityManager, ITargetManager targetManager, IFramework framework, ConfigurationService configService)
    {
        _entityManager = entityManager;
        _targetManager = targetManager;
        _framework = framework;
        _configService = configService;

        _framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var currentBrioAddr = BrioTarget?.Address ?? 0;
        if(currentBrioAddr != 0 && _lastBrioTarget != currentBrioAddr)
        {
            if(_configService.Configuration.Posing.GPoseTargetChangesWithBrio)
                GPoseTarget = BrioTarget;
        }

        var currentGPoseAddr = GPoseTarget?.Address ?? 0;
        if(currentGPoseAddr != 0 && _lastGPoseTarget != currentGPoseAddr)
        {
            if(_configService.Configuration.Posing.BrioTargetChangesWithGPose)
                BrioTarget = GPoseTarget;
        }

        _lastBrioTarget = currentBrioAddr;
        _lastGPoseTarget = currentGPoseAddr;
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
    }
}
