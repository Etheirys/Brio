
//
// Some code in this file is based on and inspired from FFXIVClientStructs
// https://github.com/aers/FFXIVClientStructs/blob/5da3308ee50128f7de894793e41231f48e1a1d0c/FFXIVClientStructs/FFXIV/Client/Game/Character/LookAtContainer.cs
// https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/Control/CharacterLookAtTargetParam.cs
//

#nullable disable

using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brio.Game.Actor;

public unsafe class ActorLookAtService : IDisposable
{
    private readonly IFramework _framework;
    private readonly IObjectTable _objectTable;

    private readonly ActorRedrawService _redrawService;
    private readonly GPoseService _gPoseService;
    private readonly VirtualCameraManager _virtualCameraManager;

    private unsafe delegate* unmanaged<CharacterLookAtController*, LookAtTarget*, uint, nint, void> _updateLookAt;

    public delegate nint ActorLookAtLoopDelegate(ContainerInterface* args);
    public Hook<ActorLookAtLoopDelegate> _actorLookAtLoop = null!;

    Dictionary<ulong, LookAtDataHolder> _lookAtHandles = [];

    public ActorLookAtService(IFramework framework, IObjectTable objectTable, VirtualCameraManager virtualCameraManager, GPoseService gPoseService, ActorRedrawService redrawService, ISigScanner sigScanner, IGameInteropProvider hooks)
    {
        _framework = framework;
        _redrawService = redrawService;
        _gPoseService = gPoseService;
        _objectTable = objectTable;
        _virtualCameraManager = virtualCameraManager;

        var updateFaceTrackerAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 8B D7 48 8B CB E8 ?? ?? ?? ?? 41 ?? ?? 8B D7 48 ?? ?? 48 ?? ?? ?? ?? 48 83 ?? ?? 5F");
        _updateLookAt = (delegate* unmanaged<CharacterLookAtController*, LookAtTarget*, uint, nint, void>)updateFaceTrackerAddress;

        var actorLookAtLoopAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 C3 08 48 83 EF 01 75 CF 48 ?? ?? ?? ?? 48");
        _actorLookAtLoop = hooks.HookFromAddress<ActorLookAtLoopDelegate>(actorLookAtLoopAddress, ActorLookAtDetour);
        _actorLookAtLoop.Enable();

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public unsafe nint ActorLookAtDetour(ContainerInterface* args)
    {
        if(_gPoseService.IsGPosing)
        {
            var targetActor = _objectTable.CreateObjectReference((nint)args->OwnerObject);
            if(targetActor is not null && targetActor.IsValid() && targetActor.IsGPose()
                && _lookAtHandles.TryGetValue(targetActor.GameObjectId, out LookAtDataHolder lookAtDataHolder))
            {
                LookAtSource lookAt = lookAtDataHolder.Target;

                if(lookAtDataHolder.TargetMode == LookAtTargetMode.Camera)
                {
                    var camera = _virtualCameraManager?.CurrentCamera;

                    if(camera is null)
                        return _actorLookAtLoop.Original(args);

                    if(lookAtDataHolder.TargetType.HasFlag(LookAtTargetType.Eyes) && lookAtDataHolder.EyesTargetLock is false)
                        lookAt.Eyes.LookAtTarget.Position = camera.RealPosition;
                    if(lookAtDataHolder.TargetType.HasFlag(LookAtTargetType.Body) && lookAtDataHolder.BodyTargetLock is false)
                        lookAt.Body.LookAtTarget.Position = camera.RealPosition;
                    if(lookAtDataHolder.TargetType.HasFlag(LookAtTargetType.Head) && lookAtDataHolder.HeadTargetLock is false)
                        lookAt.Head.LookAtTarget.Position = camera.RealPosition;
                }
                else if(lookAtDataHolder.TargetMode == LookAtTargetMode.None)
                    return _actorLookAtLoop.Original(args);

                var lookAtController = &((Character*)targetActor.Address)->LookAt.Controller;

                if(lookAtDataHolder.TargetType.HasFlag(LookAtTargetType.Body))
                    _updateLookAt(lookAtController, &lookAt.Body.LookAtTarget, 0, 0); // 0 == Body
                if(lookAtDataHolder.TargetType.HasFlag(LookAtTargetType.Head))
                    _updateLookAt(lookAtController, &lookAt.Head.LookAtTarget, 1, 0); // 1 == Head
                if(lookAtDataHolder.TargetType.HasFlag(LookAtTargetType.Eyes))
                    _updateLookAt(lookAtController, &lookAt.Eyes.LookAtTarget, 2, 0); // 2 == Eyes
            }
        }

        return _actorLookAtLoop.Original(args);
    }

    public unsafe void StopLookAt(IGameObject gameobj)
    {
        if(_lookAtHandles.TryGetValue(gameobj.GameObjectId, out var obj))
        {
            obj.LookMode = LookMode.None;
            obj.TargetType = LookAtTargetType.None;
            obj.TargetMode = LookAtTargetMode.None;
        }
    }

    public unsafe void AddObjectToLook(IGameObject gameobj)
    {
        var camera = _virtualCameraManager?.CurrentCamera;

        if(camera is null)
            return;

        if(!_lookAtHandles.TryGetValue(gameobj.GameObjectId, out LookAtDataHolder obj))
        {
            obj = new LookAtDataHolder();
            _lookAtHandles.Add(gameobj.GameObjectId, obj);
        }

        obj.LookMode = LookMode.Position;
        obj.TargetType = LookAtTargetType.None;
        obj.TargetMode = LookAtTargetMode.None;
        obj.Target = new()
        {
            Body = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = LookMode.Position,
                    Position = camera.RealPosition
                }
            },
            Eyes = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = LookMode.Position,
                    Position = camera.RealPosition
                }
            },
            Head = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = LookMode.Position,
                    Position = camera.RealPosition
                }
            },
        };
    }
    public void RemoveObjectFromLook(IGameObject gameobj)
    {
        if(_lookAtHandles.ContainsKey(gameobj.GameObjectId))
        {
            _lookAtHandles.Remove(gameobj.GameObjectId);
        }
    }

    public unsafe void SetTargetType(IGameObject obj, LookAtTargetType lookAtTarget)
    {
        if(obj is not null && _lookAtHandles.TryGetValue(obj.GameObjectId, out LookAtDataHolder value))
        {
            value.TargetType = lookAtTarget;
        }
    }
    public unsafe void SetTargetMode(IGameObject obj, LookAtTargetMode lookAtTargetMode)
    {
        if(obj is not null && _lookAtHandles.TryGetValue(obj.GameObjectId, out LookAtDataHolder value))
        {
            value.TargetMode = lookAtTargetMode;
        }
    }
    public unsafe void SetTargetLock(IGameObject obj, bool doLock, LookAtTargetType targetType, Vector3 Target)
    {
        if(obj is not null && _lookAtHandles.TryGetValue(obj.GameObjectId, out LookAtDataHolder value))
        {
            value.SetTargetLock(doLock, targetType, Target);
        }
    }

#nullable enable
    public unsafe LookAtDataHolder? GetTargetDataHolder(IGameObject obj)
    {
        if(obj is not null && _lookAtHandles.TryGetValue(obj.GameObjectId, out LookAtDataHolder? value))
        {
            return value;
        }

        return null;
    }
#nullable disable

    private void OnGPoseStateChange(bool newState)
    {
        if(newState == false)
        {
            _lookAtHandles.Clear();
        }
    }

    public void Dispose()
    {
        _actorLookAtLoop.Dispose();

        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;

        GC.SuppressFinalize(this);
    }
}

public class LookAtDataHolder
{
    public LookAtTargetMode TargetMode;
    public LookAtTargetType TargetType;

    public LookAtSource Target;

    public LookMode LookMode
    {
        set
        {
            Target.Eyes.LookAtTarget.LookMode = value;
            Target.Head.LookAtTarget.LookMode = value;
            Target.Body.LookAtTarget.LookMode = value;
        }
    }

    public Vector3 EyesTarget { get => Target.Eyes.LookAtTarget.Position; set => Target.Eyes.LookAtTarget.Position = value; }
    public Vector3 BodyTarget { get => Target.Body.LookAtTarget.Position; set => Target.Body.LookAtTarget.Position = value; }
    public Vector3 HeadTarget { get => Target.Head.LookAtTarget.Position; set => Target.Head.LookAtTarget.Position = value; }

    public bool EyesTargetLock;
    public bool BodyTargetLock;
    public bool HeadTargetLock;

    public void SetTargetLock(bool doLock, LookAtTargetType targetType, Vector3 Target)
    {
        if(targetType.HasFlag(LookAtTargetType.Eyes))
        {
            EyesTarget = Target;
            EyesTargetLock = doLock;
        }
        if(targetType.HasFlag(LookAtTargetType.Body))
        {
            BodyTarget = Target;
            BodyTargetLock = doLock;
        }
        if(targetType.HasFlag(LookAtTargetType.Head))
        {
            HeadTarget = Target;
            HeadTargetLock = doLock;
        }
    }
}

public enum LookAtTargetMode
{
    None,
    Forward,
    Camera,
    Position
}

[Flags]
public enum LookAtTargetType
{
    None = 0,
    Body = 1,
    Head = 4,
    Eyes = 8,

    All = (Eyes | Head | Body)
}

[StructLayout(LayoutKind.Sequential)]
public struct LookAtSource
{
    public LookAtType Body;
    public LookAtType Head;
    public LookAtType Eyes;
    public LookAtType Unknown;
}

[StructLayout(LayoutKind.Explicit)]
public struct LookAtType
{
    [FieldOffset(0x30)] public LookAtTarget LookAtTarget;
}

[StructLayout(LayoutKind.Explicit)]
public struct LookAtTarget
{
    [FieldOffset(0x08)] public LookMode LookMode;
    [FieldOffset(0x10)] public Vector3 Position;
    [FieldOffset(0x10)] public GameObjectId Target;
}

public enum LookMode
{
    None = 0,
    Frozen = 1,
    Pivot = 2,
    Position = 3,
}
