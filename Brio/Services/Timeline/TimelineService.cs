using Brio.Config;
using Brio.Game.GPose;
using Brio.Services.MediatorMessages;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Services.Timeline;

public sealed record TimelineCaptureChannel(string Name, string Tooltip, Action<int> Capture);

public interface ITimelineHost
{
    string Name { get; }
    string CaptureHint { get; }

    EntityId OwnerId { get; }

    List<TimelineTrack> Tracks { get; }
    IReadOnlyList<TimelineCaptureChannel> CaptureChannels { get; }

    void Apply(float frame);
    void RemoveTrack(TimelineTrack track);
}

public class TimelineService : MediatorSubscriberBase
{
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;

    public int CurrentFrame
    {
        get => (int)Math.Round(_currentTime);
        set => _currentTime = value;
    }

    public int FrameMin { get; set; }
    public int FrameMax { get; set; } = 100;
    public bool IsPlaying { get; private set; }

    public InterpolationMode NewKeyframeMode { get; set; } = InterpolationMode.Bezier;

    public IReadOnlyList<ITimelineHost> ActiveHosts => _activeHosts;
    public IEnumerable<ITimelineHost> AvailableHosts => _hosts.Where(h => !_activeHosts.Contains(h));

    private readonly HashSet<ITimelineHost> _hosts = [];
    private readonly List<ITimelineHost> _activeHosts = [];

    private double _currentTime;
    private double _lastAppliedTime = double.NaN;

    public TimelineService(Mediator mediator, GPoseService gPoseService, ConfigurationService configurationService) : base(mediator)
    {
        _gPoseService = gPoseService;
        _configurationService = configurationService;

        mediator.Subscribe<FrameworkUpdateMessage>(this, msg => OnFrameworkUpdate(msg.Framework));
        mediator.Subscribe<GposeStateChangedMessage>(this, change => OnGPoseStateChange(change.NewState));
    }

    public void Register(ITimelineHost host)
        => _hosts.Add(host);
    public void Unregister(ITimelineHost host)
    {
        _hosts.Remove(host);
        _activeHosts.Remove(host);
    }

    public void AddToTimeline(ITimelineHost host)
    {
        if(!_activeHosts.Contains(host))
            _activeHosts.Add(host);
    }
    public void RemoveFromTimeline(ITimelineHost host)
        => _activeHosts.Remove(host);

    public void TogglePlay()
        => IsPlaying = !IsPlaying;
    public void Stop()
    {
        IsPlaying = false;
        _currentTime = FrameMin;
        ApplyCurrentFrame(true);
    }

    public void ApplyCurrentFrame(bool force = false)
    {
        if(!force && _lastAppliedTime == _currentTime)
            return;

        foreach(var host in _activeHosts)
            host.Apply((float)_currentTime);

        _lastAppliedTime = _currentTime;
    }

    //

    private void OnFrameworkUpdate(IFramework framework)
    {
        if(!_gPoseService.IsGPosing)
        {
            if(IsPlaying)
                Stop();
            return;
        }

        if(!IsPlaying)
            return;

        var fps = _configurationService.Configuration.Timeline.PlaybackFramesPerSecond;
        _currentTime += framework.UpdateDelta.TotalSeconds * fps;

        if(_currentTime > FrameMax)
        {
            if(_configurationService.Configuration.Timeline.Loop)
            {
                var span = FrameMax - FrameMin;
                _currentTime = span <= 0 ? FrameMin : FrameMin + ((_currentTime - FrameMin) % span);
            }
            else
            {
                _currentTime = FrameMax;

                IsPlaying = false;
            }
        }

        ApplyCurrentFrame();
    }
    private void OnGPoseStateChange(bool newState)
    {
        if(newState)
            return;

        IsPlaying = false;

        _currentTime = FrameMin;
        _lastAppliedTime = double.NaN;
    }
}
