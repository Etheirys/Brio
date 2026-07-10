using Brio.Config;
using Brio.Game.GPose;
using Brio.Services.Timeline;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace Brio.UI.Windows.Specialized;

public class TimelineWindow : Window, IDisposable
{
    private static readonly int[] _fpsOptions = [24, 30, 60, 120];

    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;
    private readonly TimelineService _timelineService;
    private readonly TimelineSequencerEditor _editor;

    public TimelineWindow(GPoseService gPoseService, ConfigurationService configurationService, TimelineService timelineService) : base($"{Brio.Name} - VIVACITY TIMELINE BETA ###brio_timeline_window")
    {
        Namespace = "brio_timeline_namespace";

        _gPoseService = gPoseService;
        _configurationService = configurationService;
        _timelineService = timelineService;
        _editor = new TimelineSequencerEditor(timelineService, configurationService);

        this.AllowBackgroundBlur = false;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(700, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    public override bool DrawConditions()
    {

        return _gPoseService.IsGPosing && base.DrawConditions();
    }

    public override void Draw()
    {
        ImBrio.BlurWindow();

        DrawToolbar();
        ImGui.Separator();

        using var tabs = ImRaii.TabBar("##timeline_tabs");
        if(!tabs.Success)
            return;

        ITimelineHost? toRemove = null;

        var index = 0;
        foreach(var host in _timelineService.ActiveHosts)
        {
            var open = true;
            using(var tab = ImRaii.TabItem($"{host.Name}###timeline_tab_{index++}", ref open))
            {
                if(tab.Success)
                    _editor.Draw(host);
            }

            if(!open)
            {
                toRemove = host;
            }
        }

        if(toRemove != null)
            _timelineService.RemoveFromTimeline(toRemove);
    }

    private void DrawToolbar()
    {
        if(ImBrio.FontIconButton("##timeline_add", FontAwesomeIcon.Plus, "Add..."))
            ImGui.OpenPopup("##timeline_add_popup");
        DrawAddPopup();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(120f * ImGuiHelpers.GlobalScale);
        var max = _timelineService.FrameMax;
        if(ImGui.DragInt("Length", ref max, 1f, _timelineService.FrameMin + 1, 10000))
            _timelineService.FrameMax = Math.Max(_timelineService.FrameMin + 1, max);
        ImBrio.AttachToolTip("Length in Frames");

        var style = ImGui.GetStyle();
        var buttonWidth = 25f * ImGuiHelpers.GlobalScale;
        var centerWidth = (buttonWidth * 6) + (style.ItemSpacing.X * 5);

        var loopWidth = ImGui.GetFrameHeight() + style.ItemInnerSpacing.X + ImGui.CalcTextSize("Loop").X;
        var resetWidth = 25f * ImGuiHelpers.GlobalScale;
        var fpsWidth = (65f * ImGuiHelpers.GlobalScale) + style.ItemInnerSpacing.X + ImGui.CalcTextSize("FPS").X;
        var rightWidth = loopWidth + style.ItemSpacing.X + resetWidth + style.ItemSpacing.X + fpsWidth;

        ImGui.SameLine();

        var offset = Math.Max(0f, (ImBrio.GetRemainingWidth() - centerWidth - rightWidth) * 0.5f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        if(ImBrio.FontIconButton("##timeline_first_frame", FontAwesomeIcon.FastBackward, "Jump to First Frame"))
        {
            _timelineService.CurrentFrame = _timelineService.FrameMin;
            _timelineService.ApplyCurrentFrame(true);
        }

        ImGui.SameLine();
        if(ImBrio.FontIconButton("##timeline_prev_frame", FontAwesomeIcon.StepBackward, "Go to Previous Frame"))
        {
            _timelineService.CurrentFrame = Math.Clamp(_timelineService.CurrentFrame - 1, _timelineService.FrameMin, _timelineService.FrameMax);
            _timelineService.ApplyCurrentFrame(true);
        }

        ImGui.SameLine();
        var playIcon = _timelineService.IsPlaying ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
        if(ImBrio.FontIconButton("##timeline_play", playIcon, "Play / Pause"))
            _timelineService.TogglePlay();

        ImGui.SameLine();
        if(ImBrio.FontIconButton("##timeline_stop", FontAwesomeIcon.Stop, "Stop"))
            _timelineService.Stop();

        ImGui.SameLine();
        if(ImBrio.FontIconButton("##timeline_next_frame", FontAwesomeIcon.StepForward, "Go to Next Frame"))
        {
            _timelineService.CurrentFrame = Math.Clamp(_timelineService.CurrentFrame + 1, _timelineService.FrameMin, _timelineService.FrameMax);
            _timelineService.ApplyCurrentFrame(true);
        }

        ImGui.SameLine();
        if(ImBrio.FontIconButton("##timeline_last_frame", FontAwesomeIcon.FastForward, "Jump to Last Frame"))
        {
            _timelineService.CurrentFrame = _timelineService.FrameMax;
            _timelineService.ApplyCurrentFrame(true);
        }

        ImGui.SameLine();
        ImBrio.RightAlign(rightWidth);

        var loop = _configurationService.Configuration.Timeline.Loop;
        if(ImGui.Checkbox("Loop", ref loop))
        {
            _configurationService.Configuration.Timeline.Loop = loop;
            _configurationService.ApplyChange();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(90f * ImGuiHelpers.GlobalScale);
        var fps = (int)_configurationService.Configuration.Timeline.PlaybackFramesPerSecond;
        using(var combo = ImRaii.Combo("###fps", $"{fps}"))
        {
            if(combo.Success)
            {
                foreach(var option in _fpsOptions)
                {
                    if(ImGui.Selectable($"{option}", fps == option))
                    {
                        _configurationService.Configuration.Timeline.PlaybackFramesPerSecond = option;
                        _configurationService.ApplyChange();
                    }
                }
            }
        }
        ImBrio.AttachToolTip("FPS");

        ImGui.SameLine();
        if(ImBrio.HoldButton("##timeline_reset_all", string.Empty, FontAwesomeIcon.TrashAlt, 1f, new Vector2(resetWidth, 0), tooltip: "[HOLD]\nClears all keyframe data on every open Timeline tab", onlyIcon: true))
        {
            foreach(var host in _timelineService.ActiveHosts)
                host.Tracks.Clear();

            _timelineService.ApplyCurrentFrame(true);
        }
    }

    private void DrawAddPopup()
    {
        using var popup = ImRaii.Popup("##timeline_add_popup");
        if(!popup.Success)
            return;

        var any = false;
        foreach(var host in _timelineService.AvailableHosts)
        {
            any = true;
            if(ImGui.Selectable(host.Name))
                _timelineService.AddToTimeline(host);
        }

        if(!any)
        {
            ImGui.TextDisabled("Nothing available to add.");
        }
    }

    private void OnGPoseStateChange(bool newState)
    {
        IsOpen = newState ? _configurationService.Configuration.Timeline.OpenWithGPose : false;
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;
    }
}
