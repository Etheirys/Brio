using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Game.Cutscene.Files;
using Brio.Game.GPose;
using Brio.Input;
using Brio.UI;
using Brio.UI.Controls.Editors;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Brio.Game.Cutscene;

internal class CutsceneManager : IDisposable
{
    private const double FRAME_STEP = 33.33333333333333;

    private readonly EntityManager _entityManager;
    private readonly GPoseService _gPoseService;
    private readonly ITargetManager _targetManager;

    private bool _animationStarted = false;

    //

    private Vector3 BasePosition { get; set; }
    private Quaternion BaseRotation { get; set; }

    private readonly Stopwatch Stopwatch = new();

    //

    public bool CloseWindowsOnPlay = false;
    public bool StartAllActorAnimationsOnPlay = false;

    public bool IsRunning => Stopwatch.IsRunning;

    public bool DelayStart = false;
    public int DelayTime = 0;

    public bool DelayAnimationStart = false;
    public int DelayAnimationTime = 0;

    public CutsceneCameraSettings CameraSettings { get; } = new();
    public VirtualCamera VirtualCamera { get; }

    public XATCameraPathFile? CameraPath { get; set; }

    public CutsceneManager(GPoseService gPoseService, VirtualCamera virtualCamera, EntityManager entityManager, ITargetManager targetManager, IFramework framework)
    {
        VirtualCamera = virtualCamera;

        _targetManager = targetManager;
        _gPoseService = gPoseService;
        _entityManager = entityManager;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;

        InputService.Instance.AddListener(KeyBindEvents.Interface_StopCutscene, StopPlayback);
        InputService.Instance.AddListener(KeyBindEvents.Interface_StartAllActorsAnimations, StartAllActors);
        InputService.Instance.AddListener(KeyBindEvents.Interface_StopAllActorsAnimations, StopAllActors);
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(newState == false && IsRunning)
            StopPlayback();
    }

    private void StartAllActors()
    {
        foreach(var actor in _entityManager.TryGetAllActors())
        {
            if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
            {
                if(atCap is null)
                    return;
            
                ActionTimelineEditor.ApplyBaseOverride(atCap, true);
            }
        }
    }
    private void StopAllActors()
    {
        foreach(var actor in _entityManager.TryGetAllActors())
        {
            if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
            {
                if(atCap is null)
                    return;

                atCap.Stop();
            }
        }
    }

    public void StartPlayback()
    {
        if(CloseWindowsOnPlay)
        {
            UIManager.Instance.TemporarilyHideAllOpenWindows();
        }

        unsafe
        {
            IGameObject? gameObject = _targetManager.GPoseTarget;

            if(gameObject == null)
                return;
            var nativeGameObj = gameObject.Native();

            if(nativeGameObj == null)
                return;

            BasePosition = new Vector3(nativeGameObj->DrawObject->Object.Position.X, nativeGameObj->DrawObject->Object.Position.Y, nativeGameObj->DrawObject->Object.Position.Z);
            BaseRotation = new Quaternion(nativeGameObj->DrawObject->Object.Rotation.X, nativeGameObj->DrawObject->Object.Rotation.Y, nativeGameObj->DrawObject->Object.Rotation.Z, nativeGameObj->DrawObject->Object.Rotation.W);
        }

        Stopwatch.Reset();
        Stopwatch.Start();
        VirtualCamera.IsActive = true;
    }
    public void StopPlayback()
    {
        if(IsRunning)
        {
            if(CloseWindowsOnPlay)
            {
                UIManager.Instance.ReopenAllTemporarilyHiddenWindows();
            }

            Stopwatch.Reset();
            _animationStarted = false;
            VirtualCamera.IsActive = false;
            if(StartAllActorAnimationsOnPlay)
            {
                StopAllActors();
            }
        }
    }

    public Matrix4x4? UpdateCamera()
    {
        if(IsRunning is false || CameraPath is null)
            return null;

        if(DelayStart)
        {
            if(Stopwatch.ElapsedMilliseconds > DelayTime)
            {
                DelayStart = false;
                Stopwatch.Restart();
            }
            else
            {
                return null;
            }
        }

        double totalMillis = Stopwatch.ElapsedMilliseconds;

        if(DelayAnimationStart)
        {
            if(totalMillis > DelayAnimationTime)
            {
                DelayAnimationStart = false;
            }
        }

        if(_animationStarted == false && DelayAnimationStart == false)
        {
            _animationStarted = true;

            if(StartAllActorAnimationsOnPlay)
            {
                StartAllActors();
            }
        }

        CameraKeyframe? previousKey = CameraPath.CameraFrames[0];
        CameraKeyframe? nextKey = null;

        foreach(var key in CameraPath.CameraFrames)
        {
            double frameStart = key.Frame * FRAME_STEP;
            if(frameStart > totalMillis)
            {
                nextKey = key;
                break;
            }
            else
            {
                previousKey = key;
            }
        }

        if(previousKey == null || nextKey == null)
        {
            if(CameraSettings.Loop)
            {
                Stopwatch.Restart();
                UpdateCamera();
            }
            else
            {
                StopPlayback();
            }

            return null;
        }

        double previousFrameStart = previousKey.Frame * FRAME_STEP;
        double nextFrameStart = nextKey.Frame * FRAME_STEP;
        double blendLength = nextFrameStart - previousFrameStart;
        double pastPreviousKey = totalMillis - previousFrameStart;
        float frameProgress = (float)(pastPreviousKey / blendLength);

        // First we calculate the raw position/rotation/fov based on the frame progress
        var rawPosition = Vector3.Lerp(previousKey.Position, nextKey.Position, frameProgress);
        var rawRotation = Quaternion.Lerp(previousKey.Rotation, nextKey.Rotation, frameProgress);
        float rawFoV = previousKey.FoV + (nextKey.FoV - previousKey.FoV) * frameProgress;

        // Apply the user adjustments for the position 
        var adjustedPosition = (rawPosition * CameraSettings.Scale) + CameraSettings.Offset;

        // Now we apply the rotation from the base to the raw values and get a matrix for each
        Vector3 rotatedLocalPosition = BaseRotation.RotatePosition(adjustedPosition);
        Quaternion localRotation = BaseRotation * rawRotation;
        var localRotationMatrix = Matrix4x4.CreateFromQuaternion(localRotation);
        Matrix4x4.Invert(localRotationMatrix, out Matrix4x4 invertedLocalRotationMatrix);
        var localTranslationMatrix = Matrix4x4.CreateTranslation(-rotatedLocalPosition);

        // Create a matrix with the base position
        var basePositionMatrix = Matrix4x4.CreateTranslation(-BasePosition);

        // Create the final matrix
        var finalMat = basePositionMatrix * (localTranslationMatrix * invertedLocalRotationMatrix);

        this.VirtualCamera.State = new
        (
            ViewMatrix: finalMat,
            FoV: rawFoV
        );

        return finalMat;
    }

    public void Dispose()
    {
        InputService.Instance.RemoveListener(KeyBindEvents.Interface_StopCutscene, StopPlayback);
        InputService.Instance.RemoveListener(KeyBindEvents.Interface_StartAllActorsAnimations, StartAllActors);
        InputService.Instance.RemoveListener(KeyBindEvents.Interface_StopAllActorsAnimations, StopAllActors);

        if(IsRunning)
            StopPlayback();
    }
}
