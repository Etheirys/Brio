using Brio.Capabilities.Posing;
using Brio.Capabilities.Timeline;
using Brio.Config;
using Brio.Core;
using Brio.Game.Posing;
using Brio.Services.Timeline;
using Brio.UI.Controls.Stateless;
using Brio.UI.Controls.Timeline;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class TimelineSequencerEditor(TimelineService timelineService, ConfigurationService configurationService)
{
    private readonly TimelineService _timelineService = timelineService;
    private readonly ConfigurationService _configurationService = configurationService;

    private readonly ImSequencer _sequencer = new();

    private readonly Dictionary<ITimelineHost, ImSequencerState> _states = [];

    private static readonly string[] _modeNames = ["Blend", "Step"];
    private static readonly string[] _modeLetters = ["B", "S"];
    private static readonly string[] _presetNames = ["Linear", "Ease In", "Ease Out", "Ease In Out", "Custom"];

    private static readonly Vector2[] _presetP1 = [new(0.25f, 0.25f), new(0.42f, 0f), new(0f, 0f), new(0.42f, 0f)];
    private static readonly Vector2[] _presetP2 = [new(0.75f, 0.75f), new(1f, 1f), new(0.58f, 1f), new(0.58f, 1f)];

    private int _draggingPoint = -1;
    public void Draw(ITimelineHost host)
    {
        if(!_states.TryGetValue(host, out var state))
            _states[host] = state = new ImSequencerState();

        foreach(var channel in host.CaptureChannels)
        {
            if(ImGui.Button($"{channel.Name}##capture_{channel.Name}"))
                channel.Capture(_timelineService.CurrentFrame);
            ImBrio.AttachToolTip(channel.Tooltip);

            ImGui.SameLine();
        }

        ImGui.TextDisabled(host.CaptureHint);

        ImGui.SameLine();
        var newMode = (int)_timelineService.NewKeyframeMode;
        ImGui.BeginGroup();
        if(ImBrio.ButtonSelectorStrip("new_keyframe_mode", new Vector2(50f * ImGuiHelpers.GlobalScale, ImGui.GetFrameHeight()), ref newMode, _modeLetters))
            _timelineService.NewKeyframeMode = (InterpolationMode)newMode;
        ImGui.EndGroup();
        ImBrio.AttachToolTip("New keyframes are created as Blend (smooth interpolation) or Step (instant, no interpolation).");

        ImGui.SameLine();
        var showInspector = _configurationService.Configuration.Timeline.ShowInspector;
        if(ImBrio.ToggelFontIconButtonRight("##toggle_inspector", FontAwesomeIcon.SlidersH, 1f, showInspector, tooltip: showInspector ? "Hide Inspector" : "Show Inspector"))
        {
            _configurationService.Configuration.Timeline.ShowInspector = !showInspector;
            _configurationService.ApplyChange();
        }

        var avail = ImGui.GetContentRegionAvail();
        var inspectorWidth = showInspector ? 260f * ImGuiHelpers.GlobalScale : 0f;
        var spacing = showInspector ? ImGui.GetStyle().ItemSpacing.X : 0f;
        var seqWidth = avail.X - inspectorWidth - spacing;

        PosingCapability? posing = null;
        if(host is ActorTimelineCapability actorHost && actorHost.Actor.TryGetCapability<PosingCapability>(out var foundPosing))
            posing = foundPosing;

        using(var seq = ImRaii.Child("###sequencerChild", new Vector2(seqWidth, avail.Y)))
        {
            if(seq.Success)
            {
                if(posing is not null)
                {
                    var matchIndex = posing.Selected.Value switch
                    {
                        BonePoseInfoId bone => host.Tracks.FindIndex(t => t.BoneId.Equals(bone)),
                        ModelTransformSelection => host.Tracks.FindIndex(t => t.BoneId.Equals(ActorTimelineCapability.ModelTransformTrackId)),
                        _ => -1
                    };
                    if(matchIndex >= 0)
                        state.SelectedEntry = matchIndex;
                }

                var entryBeforeDraw = state.SelectedEntry;

                var frame = _timelineService.CurrentFrame;
                var changed = _sequencer.Draw("##sequencer", state, host.Tracks, _timelineService.FrameMin, _timelineService.FrameMax, ref frame, ref state.SelectedEntry);

                if(posing is not null && state.SelectedEntry != entryBeforeDraw && state.SelectedEntry >= 0 && state.SelectedEntry < host.Tracks.Count)
                {
                    var boneId = host.Tracks[state.SelectedEntry].BoneId;
                    if(boneId.Equals(ActorTimelineCapability.ModelTransformTrackId))
                        posing.Selected = PosingSelectionType.ModelTransform;
                    else
                        posing.SetBoneSelection(boneId, ImGui.GetIO().KeyCtrl || ImGui.GetIO().KeyShift);
                }

                if(frame != _timelineService.CurrentFrame)
                {
                    _timelineService.CurrentFrame = frame;
                    _timelineService.ApplyCurrentFrame();
                }
                else if(changed && !_timelineService.IsPlaying)
                {
                    _timelineService.ApplyCurrentFrame(true);
                }

                DrawContextMenu(host, state);
            }
        }

        if(showInspector)
        {
            ImGui.SameLine();

            using(var inspector = ImRaii.Child("###sequencerInspector", new Vector2(inspectorWidth, avail.Y), true))
            {
                if(inspector.Success && DrawInspector(host, state) && !_timelineService.IsPlaying)
                    _timelineService.ApplyCurrentFrame(true);
            }
        }
    }

    private void DrawContextMenu(ITimelineHost host, ImSequencerState state)
    {
        using var popup = ImRaii.Popup("SequencerContextMenu");
        if(!popup.Success)
            return;

        var modifier = ImGui.GetIO().KeyCtrl;
        var selected = GetSelectedKeyframes(host, state);
        var hasSelection = selected.Count > 0;
        var canUseKeyframe = state.ContextKeyframe != null || hasSelection;

        if(ImGui.MenuItem("Duplicate Keyframe", string.Empty, false, canUseKeyframe))
        {
            var newSelection = new HashSet<SelectedKeyframe>();

            if(hasSelection)
            {
                foreach(var sk in state.SelectedKeyframes)
                {
                    var duplicate = DuplicateKeyframe(host, sk.TrackIndex, sk.KeyframeId);
                    if(duplicate != null)
                        newSelection.Add(new SelectedKeyframe(sk.TrackIndex, duplicate.Id));
                }
            }
            else if(state.ContextKeyframe != null)
            {
                var duplicate = DuplicateKeyframe(host, state.ContextTrackIndex, state.ContextKeyframe.Id);
                if(duplicate != null)
                    newSelection.Add(new SelectedKeyframe(state.ContextTrackIndex, duplicate.Id));
            }

            state.SelectedKeyframes.Clear();
            foreach(var sk in newSelection)
                state.SelectedKeyframes.Add(sk);

            ImGui.CloseCurrentPopup();
        }

        using(ImRaii.Disabled(!modifier))
        {
            if(ImGui.MenuItem("Delete Keyframe", string.Empty, false, canUseKeyframe))
            {
                if(hasSelection)
                {
                    foreach(var track in host.Tracks)
                        track.Keyframes.RemoveAll(k => selected.Contains(k));
                    state.SelectedKeyframes.Clear();
                }
                else if(state.ContextKeyframe != null && state.ContextTrackIndex >= 0 && state.ContextTrackIndex < host.Tracks.Count)
                {
                    host.Tracks[state.ContextTrackIndex].DeleteKeyframe(state.ContextKeyframe.Id);
                }

                ImGui.CloseCurrentPopup();
            }
        }

        ImGui.Separator();

        using(ImRaii.Disabled(!modifier))
        {
            var hasTrack = state.ContextTrackIndex >= 0 && state.ContextTrackIndex < host.Tracks.Count;
            if(ImGui.MenuItem("Delete Track", string.Empty, false, hasTrack))
            {
                host.RemoveTrack(host.Tracks[state.ContextTrackIndex]);
                state.SelectedEntry = -1;
                state.SelectedKeyframes.Clear();
                ImGui.CloseCurrentPopup();
            }
        }
        if(!modifier)
            ImBrio.AttachToolTip("Hold Ctrl to delete.");
    }
    private bool DrawInspector(ITimelineHost host, ImSequencerState state)
    {
        var selected = GetSelectedKeyframes(host, state);
        if(selected.Count == 0)
        {
            ImGui.TextWrapped("Select a Keyframe to edit it.");
            return false;
        }

        var keyframe = selected[0];
        var changed = false;

        ImBrio.SeparatorText(selected.Count == 1 ? $"Keyframe at frame {keyframe.Frame}" : $"{selected.Count} Keyframes selected");
        ImBrio.VerticalSeparator(5);

        ImBrio.SeparatorText("Components");
        changed |= DrawComponents(host, selected, keyframe);

        if(selected.Count == 1)
        {
            ImBrio.SeparatorText("Transform");
            changed |= DrawKeyframeTransform(keyframe);
        }

        ImBrio.SeparatorText("Easing");
        changed |= DrawEasingEditor(selected, keyframe);

        return changed;
    }

    //

    private static bool DrawComponents(ITimelineHost host, List<TrackKeyframe> selected, TrackKeyframe representative)
    {
        var changed = false;

        if(representative.Camera.HasValue)
        {
            changed |= DrawCameraComponentToggle(selected, representative, "Position", CameraComponents.Position);
            ImGui.SameLine();
            changed |= DrawCameraComponentToggle(selected, representative, "Rotation", CameraComponents.Rotation);
            ImGui.SameLine();
            changed |= DrawCameraComponentToggle(selected, representative, "Lens", CameraComponents.Lens);
        }
        else if(representative.Light.HasValue)
        {
            changed |= DrawLightComponentToggle(selected, representative, "Position", LightComponents.Position);
            ImGui.SameLine();
            changed |= DrawLightComponentToggle(selected, representative, "Rendering", LightComponents.Rendering);
        }
        else if(representative.WorldObject.HasValue)
        {
            changed |= DrawWorldObjectComponentToggle(selected, representative, "Transform", WorldObjectComponents.Transform);

            if(host.CaptureChannels.Any(c => c.Name == "Color"))
            {
                ImGui.SameLine();
                changed |= DrawWorldObjectComponentToggle(selected, representative, "Color", WorldObjectComponents.Color);
            }
        }
        else
        {
            changed |= DrawTransformComponentToggle(selected, representative, "Position", TransformComponents.Position);
            ImGui.SameLine();
            changed |= DrawTransformComponentToggle(selected, representative, "Rotation", TransformComponents.Rotation);
            ImGui.SameLine();
            changed |= DrawTransformComponentToggle(selected, representative, "Scale", TransformComponents.Scale);
        }

        return changed;
    }

    private static bool DrawKeyframeTransform(TrackKeyframe keyframe)
    {
        if(keyframe.Camera.HasValue)
        {
            var camera = keyframe.Camera.Value;

            var posChanged = ImBrio.DragFloat3("###inspector_cam_position", ref camera.Position, 0.01f, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position").didChange;
            ImBrio.VerticalPadding(2);
            var rotChanged = ImBrio.DragFloat3("###inspector_cam_rotation", ref camera.Rotation, 1f, FontAwesomeIcon.ArrowsSpin, "Rotation").didChange;

            if(posChanged || rotChanged)
            {
                keyframe.Camera = camera;
            }

            return posChanged || rotChanged;
        }
        else if(keyframe.Light.HasValue)
        {
            var light = keyframe.Light.Value;
            var euler = light.Rotation.ToEuler();

            var posChanged = ImBrio.DragFloat3("###inspector_light_position", ref light.Position, 0.01f, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position").didChange;
            ImBrio.VerticalPadding(2);
            var rotChanged = ImBrio.DragFloat3("###inspector_light_rotation", ref euler, 1f, FontAwesomeIcon.ArrowsSpin, "Rotation").didChange;
            ImBrio.VerticalPadding(2);
            var scaleChanged = ImBrio.DragFloat3("###inspector_light_scale", ref light.Scale, 0.01f, FontAwesomeIcon.ExpandAlt, "Scale").didChange;

            if(rotChanged)
                light.Rotation = euler.ToQuaternion();

            if(posChanged || rotChanged || scaleChanged)
                keyframe.Light = light;

            return posChanged || rotChanged || scaleChanged;
        }
        else if(keyframe.WorldObject.HasValue)
        {
            var worldObject = keyframe.WorldObject.Value;
            var euler = worldObject.Rotation.ToEuler();

            var posChanged = ImBrio.DragFloat3("###inspector_wo_position", ref worldObject.Position, 0.01f, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position").didChange;
            ImBrio.VerticalPadding(2);
            var rotChanged = ImBrio.DragFloat3("###inspector_wo_rotation", ref euler, 1f, FontAwesomeIcon.ArrowsSpin, "Rotation").didChange;
            ImBrio.VerticalPadding(2);
            var scaleChanged = ImBrio.DragFloat3("###inspector_wo_scale", ref worldObject.Scale, 0.01f, FontAwesomeIcon.ExpandAlt, "Scale").didChange;

            if(rotChanged)
                worldObject.Rotation = euler.ToQuaternion();

            if(posChanged || rotChanged || scaleChanged)
                keyframe.WorldObject = worldObject;

            return posChanged || rotChanged || scaleChanged;
        }
        else
        {
            var transform = keyframe.Transform;
            var euler = transform.Rotation.ToEuler();

            var posChanged = ImBrio.DragFloat3("###inspector_position", ref transform.Position, 0.01f, FontAwesomeIcon.ArrowsUpDownLeftRight, "Position").didChange;
            ImBrio.VerticalPadding(2);
            var rotChanged = ImBrio.DragFloat3("###inspector_rotation", ref euler, 1f, FontAwesomeIcon.ArrowsSpin, "Rotation").didChange;
            ImBrio.VerticalPadding(2);
            var scaleChanged = ImBrio.DragFloat3("###inspector_scale", ref transform.Scale, 0.01f, FontAwesomeIcon.ExpandAlt, "Scale").didChange;

            if(rotChanged)
                transform.Rotation = euler.ToQuaternion();

            if(posChanged || rotChanged || scaleChanged)
                keyframe.Transform = transform;

            return posChanged || rotChanged || scaleChanged;
        }
    }

    private static bool DrawTransformComponentToggle(List<TrackKeyframe> selected, TrackKeyframe representative, string label, TransformComponents flag)
    {
        var enabled = representative.Components.HasFlag(flag);
        if(ImGui.Checkbox($"{label}##tcomp_{label}", ref enabled))
        {
            foreach(var kf in selected)
                kf.Components = enabled ? kf.Components | flag : kf.Components & ~flag;
            return true;
        }
        return false;
    }
    private static bool DrawLightComponentToggle(List<TrackKeyframe> selected, TrackKeyframe representative, string label, LightComponents flag)
    {
        var enabled = representative.LightComponents.HasFlag(flag);
        if(ImGui.Checkbox($"{label}##lcomp_{label}", ref enabled))
        {
            foreach(var kf in selected)
                kf.LightComponents = enabled ? kf.LightComponents | flag : kf.LightComponents & ~flag;
            return true;
        }
        return false;
    }
    private static bool DrawWorldObjectComponentToggle(List<TrackKeyframe> selected, TrackKeyframe representative, string label, WorldObjectComponents flag)
    {
        var enabled = representative.WorldObjectComponents.HasFlag(flag);
        if(ImGui.Checkbox($"{label}##wcomp_{label}", ref enabled))
        {
            foreach(var kf in selected)
                kf.WorldObjectComponents = enabled ? kf.WorldObjectComponents | flag : kf.WorldObjectComponents & ~flag;
            return true;
        }
        return false;
    }
    private static bool DrawCameraComponentToggle(List<TrackKeyframe> selected, TrackKeyframe representative, string label, CameraComponents flag)
    {
        var enabled = representative.CameraComponents.HasFlag(flag);
        if(ImGui.Checkbox($"{label}##ccomp_{label}", ref enabled))
        {
            foreach(var kf in selected)
                kf.CameraComponents = enabled ? kf.CameraComponents | flag : kf.CameraComponents & ~flag;
            return true;
        }
        return false;
    }

    //

    private bool DrawEasingEditor(List<TrackKeyframe> selected, TrackKeyframe representative)
    {
        var changed = false;

        var mode = (int)representative.InterpolationMode;
        if(ImBrio.ButtonSelectorStrip("###interpolation_mode", Vector2.Zero, ref mode, _modeNames))
        {
            var newMode = (InterpolationMode)mode;
            foreach(var kf in selected)
                kf.InterpolationMode = newMode;
            changed = true;
        }

        if(representative.InterpolationMode == InterpolationMode.Step)
            return changed;

        var preset = MatchPreset(representative.P1, representative.P2);
        if(ImGui.Combo("Preset", ref preset, _presetNames, _presetNames.Length) && preset < _presetP1.Length)
        {
            foreach(var kf in selected)
            {
                kf.P1 = _presetP1[preset];
                kf.P2 = _presetP2[preset];
            }
            changed = true;
        }

        var p1 = representative.P1;
        var p2 = representative.P2;

        var canvasPos = ImGui.GetCursorScreenPos();
        var canvasSize = new Vector2(ImGui.GetContentRegionAvail().X, 140f * ImGuiHelpers.GlobalScale);
        ImGui.InvisibleButton("###easingInvis", canvasSize);

        var drawList = ImGui.GetWindowDrawList();
        var io = ImGui.GetIO();

        var margin = canvasSize.X * 0.05f;
        var plotMin = canvasPos + new Vector2(margin, margin);
        var plotMax = canvasPos + canvasSize - new Vector2(margin, margin);
        var plotSize = plotMax - plotMin;

        Vector2 ToScreen(Vector2 norm) => new(plotMin.X + norm.X * plotSize.X, plotMin.Y + (1f - norm.Y) * plotSize.Y);
        Vector2 ToNorm(Vector2 screen)
        {
            var norm = (screen - plotMin) / plotSize;
            return new Vector2(norm.X, 1f - norm.Y);
        }

        drawList.AddRectFilled(plotMin, plotMax, ImGui.GetColorU32(ImGuiCol.FrameBg));
        drawList.AddRect(plotMin, plotMax, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f));

        var p0Screen = ToScreen(new Vector2(0, 0));
        var p1Screen = ToScreen(p1);
        var p2Screen = ToScreen(p2);
        var p3Screen = ToScreen(new Vector2(1, 1));

        if(ImGui.IsItemActivated())
        {
            var handleHitRadius = 12f * ImGuiHelpers.GlobalScale;
            if(Vector2.Distance(io.MousePos, p1Screen) < handleHitRadius)
                _draggingPoint = 0;
            else if(Vector2.Distance(io.MousePos, p2Screen) < handleHitRadius)
                _draggingPoint = 1;
            else
                _draggingPoint = -1;
        }

        if(ImGui.IsItemActive() && _draggingPoint != -1)
        {
            var norm = ToNorm(io.MousePos);
            norm.X = Math.Clamp(norm.X, 0f, 1f);
            foreach(var kf in selected)
            {
                if(_draggingPoint == 0)
                    kf.P1 = norm;
                else
                    kf.P2 = norm;
            }
            changed = true;
        }

        if(ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            _draggingPoint = -1;

        var handleColor = ImGui.GetColorU32(ImGuiCol.Button);
        var handleLineThickness = 2f * ImGuiHelpers.GlobalScale;
        var curveLineThickness = 3f * ImGuiHelpers.GlobalScale;
        var handleRadius = 6f * ImGuiHelpers.GlobalScale;
        drawList.AddLine(p0Screen, p1Screen, handleColor, handleLineThickness);
        drawList.AddLine(p3Screen, p2Screen, handleColor, handleLineThickness);
        drawList.AddBezierCubic(p0Screen, p1Screen, p2Screen, p3Screen, ImGui.GetColorU32(ImGuiCol.PlotLines), curveLineThickness, 32);
        drawList.AddCircleFilled(p1Screen, handleRadius, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
        drawList.AddCircleFilled(p2Screen, handleRadius, ImGui.GetColorU32(ImGuiCol.ButtonHovered));

        return changed;
    }
    private static int MatchPreset(Vector2 p1, Vector2 p2)
    {
        for(var i = 0; i < _presetP1.Length; i++)
            if(Vector2.Distance(p1, _presetP1[i]) < 0.001f && Vector2.Distance(p2, _presetP2[i]) < 0.001f)
                return i;
        return _presetNames.Length - 1;
    }

    private static TrackKeyframe? DuplicateKeyframe(ITimelineHost host, int trackIndex, Guid keyframeId)
    {
        if(trackIndex < 0 || trackIndex >= host.Tracks.Count)
            return null;

        var track = host.Tracks[trackIndex];
        var source = track.Keyframes.FirstOrDefault(k => k.Id == keyframeId);
        if(source == null)
            return null;

        var newFrame = source.Frame + 1;
        if(track.Keyframes.Any(k => k.Frame == newFrame))
            return null;

        var duplicate = new TrackKeyframe(newFrame, source.Transform)
        {
            P1 = source.P1,
            P2 = source.P2,
            InterpolationMode = source.InterpolationMode,
            Shape = source.Shape,
            CustomColor = source.CustomColor,
            Components = source.Components,
            Camera = source.Camera,
            CameraComponents = source.CameraComponents,
            Light = source.Light,
            LightComponents = source.LightComponents,
            WorldObject = source.WorldObject,
            WorldObjectComponents = source.WorldObjectComponents
        };

        track.Keyframes.Add(duplicate);
        track.Keyframes = [.. track.Keyframes.OrderBy(k => k.Frame)];
        return duplicate;
    }

    private static List<TrackKeyframe> GetSelectedKeyframes(ITimelineHost host, ImSequencerState state)
    {
        var result = new List<TrackKeyframe>();
        foreach(var sk in state.SelectedKeyframes)
        {
            if(sk.TrackIndex < 0 || sk.TrackIndex >= host.Tracks.Count)
                continue;

            var kf = host.Tracks[sk.TrackIndex].Keyframes.FirstOrDefault(k => k.Id == sk.KeyframeId);
            if(kf != null)
                result.Add(kf);
        }
        return result;
    }
}
