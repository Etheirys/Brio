using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
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

    public delegate nint ActorLookAtLoopDelegate(nint args);
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
        _actorLookAtLoop = hooks.HookFromAddress<ActorLookAtLoopDelegate>(actorLookAtLoopAddress, ActorLookAtLoopDetour);
        _actorLookAtLoop.Enable();

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(newState == false)
        {
            _lookAtHandles.Clear();
        }
    }

    public unsafe IntPtr ActorLookAtLoopDetour(nint a1)
    {
        if(_gPoseService.IsGPosing)
        {
            var lookAtContainer = (ContainerInterface*)a1;
            var obj = _objectTable.CreateObjectReference((nint)lookAtContainer->OwnerObject);
            if(obj is not null && obj.IsValid() && obj.IsGPose() && _lookAtHandles.ContainsKey(obj.GameObjectId))
            {
                actorLook(obj, _lookAtHandles[obj.GameObjectId]);
            }
        }

        return _actorLookAtLoop.Original(a1);
    }

    public unsafe void actorLook(IGameObject targetActor, LookAtDataHolder lookAtDataHolder)
    {
        LookAtSource lookAt = lookAtDataHolder.Target;

        lookAt.Eyes.LookAtTarget.LookMode = (uint)lookAtDataHolder.LookAtMode;
        lookAt.Head.LookAtTarget.LookMode = (uint)lookAtDataHolder.LookAtMode;
        lookAt.Body.LookAtTarget.LookMode = (uint)lookAtDataHolder.LookAtMode;

        if(lookAtDataHolder.LookAtType == LookAtTargetMode.Camera)
        {
            var camera = _virtualCameraManager?.CurrentCamera;

            if(camera is null)
                return;

            if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Eyes))
                lookAt.Eyes.LookAtTarget.Position = camera.RealPosition;
            if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Body))
                lookAt.Head.LookAtTarget.Position = camera.RealPosition;
            if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Head))
                lookAt.Body.LookAtTarget.Position = camera.RealPosition;
        }
        else if(lookAtDataHolder.LookAtType == LookAtTargetMode.None)
            return;

        var lookAtController = &((Character*)targetActor.Address)->LookAt.Controller;

        if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Eyes))
            _updateLookAt(lookAtController, &lookAt.Eyes.LookAtTarget, (uint)LookEditType.Eyes, 0);
        if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Body))
            _updateLookAt(lookAtController, &lookAt.Body.LookAtTarget, (uint)LookEditType.Body, 0);
        if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Head))
            _updateLookAt(lookAtController, &lookAt.Head.LookAtTarget, (uint)LookEditType.Head, 0);
    }

    public unsafe void TESTactorlookClear(IGameObject gameobj)
    {
        if(_lookAtHandles.TryGetValue(gameobj.GameObjectId, out var obj))
        {
            obj.LookAtMode = LookMode.None;
            obj.lookAtTargetType = LookAtTargetType.All;
            obj.LookAtType = LookAtTargetMode.None;
        }
        else
        {

        }
    }

    public unsafe void TESTactorlook(IGameObject gameobj)
    {
        var camera = _virtualCameraManager?.CurrentCamera;

        if(camera is null)
            return;

        _lookAtHandles.TryGetValue(gameobj.GameObjectId, out LookAtDataHolder? obj);

        if(obj is null)
        {
            obj = new LookAtDataHolder();
            _lookAtHandles.Add(gameobj.GameObjectId, obj);
        }

        obj.LookAtMode = LookMode.Position;
        obj.lookAtTargetType = LookAtTargetType.All;
        obj.LookAtType = LookAtTargetMode.Camera;
        obj.Target = new()
        {
            Body = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = (uint)LookMode.Position,
                    Position = camera.RealPosition
                }
            },
            Eyes = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = (uint)LookMode.Position,
                    Position = camera.RealPosition
                }
            },
            Head = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = (uint)LookMode.Position,
                    Position = camera.RealPosition
                }
            },
        };
    }

    public void RemoveFromLook(IGameObject gameobj)
    {
        if(_lookAtHandles.ContainsKey(gameobj.GameObjectId))
        {
            _lookAtHandles.Remove(gameobj.GameObjectId);
        }
    }

    public void Dispose()
    {
        _actorLookAtLoop.Dispose();

        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;

        GC.SuppressFinalize(this);
    }
}

public record class LookAtDataHolder
{
    public LookAtTargetMode LookAtType;

    public LookAtTargetType lookAtTargetType;
    public LookMode LookAtMode;

    public LookAtSource Target;

    public void SetTarget(Vector3 vector, LookAtTargetType targetType)
    {
        if(targetType.HasFlag(LookAtTargetType.Eyes))
            Target.Eyes.LookAtTarget.Position = vector;
        if(targetType.HasFlag(LookAtTargetType.Body))
            Target.Head.LookAtTarget.Position = vector;
        if(targetType.HasFlag(LookAtTargetType.Head))
            Target.Body.LookAtTarget.Position = vector;
    }
}

public enum LookAtTargetMode
{
    None,
    Forward,
    Camera,
    Position
}

public class LookAtData
{
    public uint EntityIdSource;
    public LookAtSource LookAtSource;

    public LookMode LookMode;
    public LookAtTargetType TargetType;

    public uint TargetEntityId;
    public Vector3 TargetPosition;
}

[Flags]
public enum LookAtTargetType
{
    Body = 0,
    Head = 1,
    Eyes = 2,

    Face = Head | Eyes,
    All = Body | Head | Eyes
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
    [FieldOffset(0x08)] public uint LookMode;
    [FieldOffset(0x10)] public Vector3 Position;
}

public enum LookEditType
{
    Body = 0,
    Head = 1,
    Eyes = 2
}

public enum LookMode
{
    None = 0,
    Frozen = 1,
    Pivot = 2,
    Position = 3,
}
