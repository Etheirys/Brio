using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using Brio.Entities;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using System;
using static Brio.Game.Actor.ActorRedrawService;
using StructsGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Brio.Capabilities.Posing;

namespace Brio.Game.Posing;

internal unsafe class ModelTransformService : IDisposable
{
    public delegate void SetPositionDelegate(StructsGameObject* gameObject, float x, float y, float z);
    private readonly Hook<SetPositionDelegate> _setPositionHook = null!;

    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly ActorRedrawService _actorRedrawService;

    public ModelTransformService(EntityManager entityManager, GPoseService gPoseService, ActorRedrawService actorRedrawService, IGameInteropProvider hooking)
    {
        _entityManager = entityManager;
        _gPoseService = gPoseService;
        _actorRedrawService = actorRedrawService;

        _setPositionHook = hooking.HookFromAddress<SetPositionDelegate>((nint)StructsGameObject.Addresses.SetPosition.Value, UpdatePositionDetour);
        _setPositionHook.Enable();

        _actorRedrawService.ActorRedrawEvent += OnActorRedraw;
    }

    public unsafe Transform GetTransform(GameObject go)
    {
        var native = go.Native();
        var drawObject = native->DrawObject;
        if (drawObject != null)
        {
            return *(Transform*)(&drawObject->Object.Position);
        }
        else
        {
            return new Transform()
            {
                Position = native->Position
            };
        };
    }

    public unsafe void SetTransform(GameObject go, Transform transform) => SetTransform(go.Native(), transform);

    public unsafe void SetTransform(StructsGameObject* native, Transform transform)
    {
        var drawObject = native->DrawObject;
        if (drawObject != null)
        {
            *(Transform*)(&drawObject->Object.Position) = transform;
        }
    }

    private void UpdatePositionDetour(StructsGameObject* gameObject, float x, float y, float z)
    {
        if (_gPoseService.IsGPosing)
        {
            if (_entityManager.TryGetEntity(gameObject, out var entity))
            {
                if (entity.TryGetCapability<ModelPosingCapability>(out var transformCapability))
                {
                    if (transformCapability.OverrideTransform.HasValue)
                    {
                        var transform = transformCapability.OverrideTransform.Value;
                        SetTransform(gameObject, transform);
                        return;
                    }
                }
            }
        }

        _setPositionHook.Original(gameObject, x, y, z);
    }

    private void OnActorRedraw(GameObject go, RedrawStage stage)
    {
        if (stage == RedrawStage.After)
            UpdatePositionDetour((StructsGameObject*)go.Address, go.Position.X, go.Position.Y, go.Position.Z);
    }


    public void Dispose()
    {
        _setPositionHook.Dispose();
        _actorRedrawService.ActorRedrawEvent -= OnActorRedraw;
    }
}
