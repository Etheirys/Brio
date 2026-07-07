using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Game.GPose;
using Brio.Services;
using Brio.Services.MediatorMessages;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;

namespace Brio.Game.Core;

public unsafe class TargetService : MediatorSubscriberBase
{
    private readonly EntityManager _entityManager;
    private readonly ITargetManager _targetManager;
    private readonly IFramework _framework;
    private readonly ConfigurationService _configService;
    private readonly GPoseService _gPoseService;
    private readonly DalamudService _dalamudService;

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

    public bool HasGPoseTarget => GPoseTarget is not null;

    public IGameObject? GPoseTarget
    {
        get => _targetManager.GPoseTarget;
        set => _targetManager.GPoseTarget = value;
    }

    private nint _lastBrioTarget = 0;
    private nint _lastGPoseTarget = 0;

    public TargetService(EntityManager entityManager, Mediator mediator, DalamudService dalamudService, GPoseService gPoseService, ITargetManager targetManager, IFramework framework, ConfigurationService configService) : base(mediator)
    {
        _entityManager = entityManager;
        _targetManager = targetManager;
        _framework = framework;
        _configService = configService;
        _gPoseService = gPoseService;
        _dalamudService = dalamudService;

        mediator.Subscribe<FrameworkUpdateMessage>(this, (state) => OnFrameworkUpdate());
    }

    private void OnFrameworkUpdate()
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


    public bool IsSelf(IGameObject gameObject)
    {
        var playerChar = _dalamudService.GetPlayerCharacter();
        return playerChar is not null && gameObject is not null && string.Equals(playerChar.Name.TextValue, gameObject.Name.TextValue, StringComparison.Ordinal);
    }

    public (bool CanApply, string TargetName, IGameObject GameObject) CanApplyMCDFToTarget()
    {
        var targetName = "Invalid Target";
        var canApply = _gPoseService.IsGPosing && HasGPoseTarget
            && GPoseTarget!.ObjectKind == ObjectKind.Pc;

        if(canApply)
        {
            targetName = GPoseTarget!.Name.TextValue;
        }

        return (canApply, targetName, GPoseTarget!);
    }

    public override void Dispose()
    {
        base.Dispose();

        GC.SuppressFinalize(this);
    }
}
