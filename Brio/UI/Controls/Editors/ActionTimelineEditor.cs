using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Entities;
using Brio.Files;
using Brio.Game.Actor.Extensions;
using Brio.Game.Cutscene;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Resources;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.IO;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;

namespace Brio.UI.Controls.Editors;

public class ActionTimelineEditor(CutsceneManager cutsceneManager, GPoseService gPoseService, EntityManager entityManager, PhysicsService physicsService, ConfigurationService configService)
{
    private readonly CutsceneManager _cutsceneManager = cutsceneManager;
    private readonly GPoseService _gPoseService = gPoseService;
    private readonly PhysicsService _physicsService = physicsService;
    private readonly ConfigurationService _configService = configService;
    private readonly EntityManager _entityManager = entityManager;

    private static float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXX").X;
    private static float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + (ImGui.GetStyle().FramePadding.X * 2f);

    private static readonly ActionTimelineSelector _globalTimelineSelector = new("global_timeline_selector");

    private static bool _startAnimationOnSelect = true;

    private string _cameraPath = string.Empty;
    private ActionTimelineCapability _capability = null!;
    private bool _delimitSpeed = false;

    public void Draw(bool drawAdvanced, ActionTimelineCapability capability)
    {
        _capability = capability;

        DrawHeder();

        ImGui.Separator();

        DrawBaseOverride();
        DrawBlend();
        DrawOverallSpeed(drawAdvanced);

        if(drawAdvanced == false)
        {
            ImGui.Separator();

            DrawFirstScrub();
        }

        if(drawAdvanced)
        {
            DrawLips();

            if(ImGui.CollapsingHeader("Scrub"))
            {
                DrawScrub();
            }

            if(ImGui.CollapsingHeader("Slots"))
            {
                DrawSlots();
            }

            if(ImGui.CollapsingHeader("Cutscene Control"))
            {
                DrawCutscene();
            }
        }
    }

    private void DrawHeder()
    {
        if(ImBrio.ToggelButton("Freeze Physics", new Vector2(95, 25), _physicsService.IsFreezeEnabled, hoverText: _physicsService.IsFreezeEnabled ? "Un-Freeze Physics" : "Freeze Physics"))
        {
            _physicsService.FreezeToggle();
        }

        ImGui.SameLine();

        ImBrio.RightAlign(100 * ImGuiHelpers.GlobalScale, 1);

        if(ImGui.Button("Actors  ▼", new Vector2(70, 25) * ImGuiHelpers.GlobalScale))
        {
            ImGui.OpenPopup("animation_control");
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "Reset Animation", _capability.HasOverride))
        {
            _capability.Reset();
            _cutsceneManager.StopPlayback();
            _cutsceneManager.CameraPath = null;
            _cameraPath = string.Empty;
        }

        using var popup = ImRaii.Popup("animation_control");
        if(popup.Success)
        {
            ImBrio.VerticalPadding(2);

            if(ImGui.Button("Freeze All Actors", new Vector2(150, 0)))
            {
                foreach(var actor in _entityManager.TryGetAllActors())
                {
                    if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
                    {
                        if(atCap is null)
                            return;

                        if(atCap.SpeedMultiplier > 0f)
                        {
                            atCap.SetOverallSpeedOverride(0f);
                        }
                    }
                }
            }

            ImBrio.VerticalPadding(2);

            if(ImGui.Button("Un-Freeze All Actors", new Vector2(150, 0)))
            {
                foreach(var actor in _entityManager.TryGetAllActors())
                {
                    if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
                    {
                        if(atCap is null)
                            return;

                        if(atCap.HasSpeedMultiplierOverride)
                        {
                            atCap.ResetOverallSpeedOverride();
                        }
                    }
                }
            }

            ImBrio.VerticalPadding(2);

            if(ImGui.Button("Play all Animations", new Vector2(150, 0)))
            {
                foreach(var actor in _entityManager.TryGetAllActors())
                {
                    if(actor.TryGetCapability<ActionTimelineCapability>(out ActionTimelineCapability? atCap))
                    {
                        if(atCap is null)
                            return;

                        ApplyBaseOverride(atCap, true);
                    }
                }
            }

            ImBrio.VerticalPadding(2);

            if(ImGui.Button("Stop all Animations", new Vector2(150, 0)))
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

            ImBrio.VerticalPadding(2);

        }
    }

    private void DrawBaseOverride()
    {
        const string baseLabel = "Base";
        ImGui.SetNextItemWidth(MaxItemWidth - ImGui.CalcTextSize("XXXX").X);
        ImGui.InputInt($"###base_animation", ref _capability.SlotedBaseAnimation, 0, 0);
        if(ImBrio.IsItemConfirmed())
        {
            ApplyBaseOverride(_capability, true);
        }

        ImGui.SameLine();
        ImGui.Checkbox("###base_interrupt", ref _capability.DoBaseInterrupt);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Interrupt");

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(baseLabel);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_play", FontAwesomeIcon.PlayCircle, 3, "Play", _capability.SlotedBaseAnimation != 0))
            ApplyBaseOverride(_capability);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_reset", FontAwesomeIcon.StopCircle, 2, "Stop", _capability.HasBaseOverride))
        {
            _capability.ResetBaseOverride();
            _capability.ResetOverallSpeedOverride();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_search", FontAwesomeIcon.Search, 1, "Search"))
        {
            _globalTimelineSelector.Select(null, false);
            _globalTimelineSelector.AllowBlending = false;
            ImGui.OpenPopup("base_search_popup");

        }

        using(var popup = ImRaii.Popup("base_search_popup"))
        {
            if(popup.Success)
            {
                ImGui.Checkbox("Start Animation On Select", ref _startAnimationOnSelect);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Start Animation On Select");

                _globalTimelineSelector.Draw();

                if(_globalTimelineSelector.SoftSelectionChanged && _globalTimelineSelector.SoftSelected != null)
                {
                    _capability.SlotedBaseAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
                }

                if(_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
                {
                    _capability.SlotedBaseAnimation = _globalTimelineSelector.Selected.TimelineId;

                    if(_startAnimationOnSelect)
                        ApplyBaseOverride(_capability, true);

                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private void DrawBlend()
    {
        const string blendLabel = "Blend";

        ImGui.SetNextItemWidth(MaxItemWidth);
        ImGui.InputInt($"###blend_animation", ref _capability.SlotedBlendAnimation, 0, 0);
        if(ImBrio.IsItemConfirmed())
        {
            ApplyBlend(_capability);
        }

        ImGui.SameLine();

        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(blendLabel);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("blend_play", FontAwesomeIcon.PlayCircle, 2, "Play", _capability.SlotedBlendAnimation != 0))
            ApplyBlend(_capability);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("blend_search", FontAwesomeIcon.Search, 1, "Search"))
        {
            _globalTimelineSelector.Select(null, false);
            _globalTimelineSelector.AllowBlending = true;

            ImGui.OpenPopup("blend_search_popup");

        }

        using(var popup = ImRaii.Popup("blend_search_popup"))
        {
            if(popup.Success)
            {
                _globalTimelineSelector.Draw();

                if(_globalTimelineSelector.SoftSelectionChanged && _globalTimelineSelector.SoftSelected != null)
                {
                    _capability.SlotedBlendAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
                }

                if(_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
                {
                    _capability.SlotedBlendAnimation = _globalTimelineSelector.Selected.TimelineId;
                    ApplyBlend(_capability);
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private void DrawLips()
    {
        var lipsOverride = _capability.LipsOverride;

        string preview = "None";
        if(lipsOverride != 0)
            preview = GameDataProvider.Instance.ActionTimelines[lipsOverride].Key.ToString();

        ImGui.SetNextItemWidth(MaxItemWidth);
        using(var combo = ImRaii.Combo("###lips", preview))
        {
            if(combo.Success)
            {
                if(ImGui.Selectable($"None", lipsOverride == 0))
                {
                    _capability.LipsOverride = 0;
                }

                for(uint i = 0x272; i <= 0x272 + 8; ++i)
                {
                    var entry = GameDataProvider.Instance.ActionTimelines[i];
                    bool selected = lipsOverride == i;
                    if(ImGui.Selectable($"{entry.Key} ({i})", selected))
                    {
                        _capability.LipsOverride = (ushort)i;
                    }
                }
            }
        }
        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text("Lips");
    }

    private unsafe void DrawScrub()
    {
        float width = -ImGui.CalcTextSize("XXXX").X;

        var drawObj = _capability.Character.Native()->GameObject.DrawObject;
        if(drawObj == null)
            return;

        if(drawObj->Object.GetObjectType() != ObjectType.CharacterBase)
            return;

        var charaBase = (CharacterBase*)drawObj;

        if(charaBase->Skeleton == null)
            return;

        var skeleton = charaBase->Skeleton;

        for(int p = 0; p < skeleton->PartialSkeletonCount; ++p)
        {
            var partial = &skeleton->PartialSkeletons[p];
            var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
            if(animatedSkele == null)
                continue;

            for(int c = 0; c < animatedSkele->AnimationControls.Length; ++c)
            {
                var control = animatedSkele->AnimationControls[c].Value;
                if(control == null)
                    continue;

                var binding = control->hkaAnimationControl.Binding;
                if(binding.ptr == null)
                    continue;

                var anim = binding.ptr->Animation.ptr;
                if(anim == null)
                    continue;

                var duration = anim->Duration;
                var time = control->hkaAnimationControl.LocalTime;
                ImGui.SetNextItemWidth(width);
                if(ImGui.SliderFloat($"###scrub_{p}_{c}", ref time, 0f, duration, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                {
                    control->hkaAnimationControl.LocalTime = time;
                }
                if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    _capability.SetOverallSpeedOverride(0f);
                }
                ImGui.SameLine();
                ImGui.Text($"{p}.{c}");
            }
        }
    }

    private unsafe void DrawFirstScrub()
    {

        var drawObj = _capability.Character.Native()->GameObject.DrawObject;
        if(drawObj == null)
            return;

        if(drawObj->Object.GetObjectType() != ObjectType.CharacterBase)
            return;

        var charaBase = (CharacterBase*)drawObj;

        if(charaBase->Skeleton == null)
            return;

        var skeleton = charaBase->Skeleton;

        if(!(skeleton->PartialSkeletonCount > 0))
            return;

        var partial = &skeleton->PartialSkeletons[0];
        var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
        if(animatedSkele == null)
            return;

        if(!(animatedSkele->AnimationControls.Length > 0))
            return;

        var control = animatedSkele->AnimationControls[0].Value;
        if(control == null)
            return;

        var binding = control->hkaAnimationControl.Binding;
        if(binding.ptr == null)
            return;

        var anim = binding.ptr->Animation.ptr;
        if(anim == null)
            return;

        var duration = anim->Duration;
        var time = control->hkaAnimationControl.LocalTime;

        ImGui.SetNextItemWidth(-ImGui.CalcTextSize("ScrubX").X);
        if(ImGui.SliderFloat($"###scrub_001", ref time, 0f, duration, "%.2f", ImGuiSliderFlags.AlwaysClamp))
        {
            control->hkaAnimationControl.LocalTime = time;
        }
        if(ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            _capability.SetOverallSpeedOverride(0f);
        }
        ImGui.SameLine();
        ImGui.Text("Scrub");
    }

    private void DrawSlots()
    {

        var slots = Enum.GetValues<ActionTimelineSlots>();

        foreach(var slot in slots)
        {
            using(ImRaii.PushId((int)slot))
            {
                DrawSlot(slot);
                ImGui.Separator();
            }
        }
    }

    private void DrawSlot(ActionTimelineSlots slot)
    {
        var actionInfo = _capability.GetSlotAction(slot).Match(
                   action => $"{action.RowId} ({action.Key})",
                   none => "None"
               );

        var slotDescription = $"{slot} ({(int)slot}): {actionInfo}";

        using(ImRaii.PushId($"slot_{slot}"))
        {
            ImGui.Text(slotDescription);

            float existingSpeed = _capability.GetSlotSpeed(slot);
            float newSpeed = existingSpeed;
            const string speedLabel = "Slot Speed";
            ImGui.SetNextItemWidth(ImGui.CalcTextSize($"XXXXXXXXXXXXXXXXXi").X);
            if(ImGui.SliderFloat($"{speedLabel}", ref newSpeed, 0f, 5f))
                _capability.SetSlotSpeedOverride(slot, newSpeed);


            ImGui.SameLine();

            if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "Reset Speed", _capability.HasSlotSpeedOverride(slot)))
                _capability.ResetSlotSpeedOverride(slot);

            ImGui.SameLine();

            var speed = _capability.GetSlotSpeed(slot);
            if(ImBrio.FontIconButtonRight("speed_pause", FontAwesomeIcon.PauseCircle, 2, "Pause", speed > 0f))
                _capability.SetSlotSpeedOverride(slot, 0.0f);
        }
    }

    private void DrawOverallSpeed(bool drawAdvanced)
    {
        float existingSpeed = _capability.SpeedMultiplier;
        float newSpeed = existingSpeed;

        const string speedLabel = "Speed";
        ImGui.SetNextItemWidth(drawAdvanced ? MaxItemWidth - ImGui.CalcTextSize("XXXX").X : MaxItemWidth);
        if(ImGui.SliderFloat($"###speed_slider", ref newSpeed, _delimitSpeed ? -5f : 0f, _delimitSpeed ? 10f : 5f))
            _capability.SetOverallSpeedOverride(newSpeed);

        if(drawAdvanced)
        {
            ImGui.SameLine();
            if(ImGui.Checkbox("###delimit_speed", ref _delimitSpeed))
                if(_delimitSpeed == false)
                {
                    _capability.ResetOverallSpeedOverride();
                }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Delimit Speed");
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(speedLabel);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("speed_reset", FontAwesomeIcon.Undo, 1, "Reset Speed", _capability.HasSpeedMultiplierOverride))
            _capability.ResetOverallSpeedOverride();

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("speed_pause", FontAwesomeIcon.PauseCircle, 2, "Pause", _capability.SpeedMultiplier != 0f))
        {
            _capability.SetOverallSpeedOverride(0f);
        }
    }

    private void DrawCutscene()
    {
        ImGui.Text("Camera Path ");

        ImGui.SameLine();

        ImGui.InputText(string.Empty, ref _cameraPath, 260, ImGuiInputTextFlags.ReadOnly);

        ImGui.SameLine();

        if(ImGui.Button("Browse"))
        {
            UIManager.Instance.FileDialogManager.OpenFileDialog("Browse for XAT Camera File", "XAT Camera File {.xcp}",
                (success, path) =>
                {
                    if(success)
                    {
                        _cameraPath = path[0];

                        string? folderPath = Path.GetDirectoryName(_cameraPath);
                        if(folderPath is not null)
                        {
                            _configService.Configuration.LastXATPath = folderPath;
                            _configService.Save();

                            _cutsceneManager.CameraPath = new XATCameraFile(new BinaryReader(File.OpenRead(_cameraPath)));
                        }
                    }
                    else
                    {
                        _cameraPath = string.Empty;
                        _cutsceneManager.CameraPath = null;
                    }
                }, 1, _configService.Configuration.LastXATPath, false);
        }

        ImGui.Separator();

        using(ImRaii.Disabled(string.IsNullOrEmpty(_cameraPath)))
        {
            ImGui.Checkbox("Enable FOV", ref _cutsceneManager.CameraSettings.EnableFOV);

            ImGui.Separator();

            ImGui.Text("Disabling FOV will make for a less accurate Camera, but might");
            ImGui.Text("provide for an easer way to support more character sizes without");
            ImGui.Text("changing the Camera's Scale & Offset!");

            ImGui.Separator();

            ImGui.InputFloat3("Camera Scale", ref _cutsceneManager.CameraSettings.Scale);
            ImGui.InputFloat3("Camera Offset", ref _cutsceneManager.CameraSettings.Offset);

            ImGui.Separator();

            ImGui.Checkbox("Loop", ref _cutsceneManager.CameraSettings.Loop);

            ImGui.Checkbox("Hide Brio On Play  (Press 'Shift + B' to Stop Cutscene)", ref _cutsceneManager.CloseWindowsOnPlay);

            ImGui.Checkbox("###delay_Start", ref _cutsceneManager.DelayStart);
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Start Delay");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(MaxItemWidth);

            using(ImRaii.Disabled(_cutsceneManager.DelayStart == false))
            {
                ImGui.InputInt($"###delay_Start_Chek", ref _cutsceneManager.DelayTime, 0, 0);
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(LabelStart);
            ImGui.Text("Start Delay");

            ImGui.Separator();

            ImGui.Checkbox("Start All Actors Animations On Play", ref _cutsceneManager.StartAllActorAnimationsOnPlay);

            using(ImRaii.Disabled(_cutsceneManager.StartAllActorAnimationsOnPlay == false))
            {
                ImGui.Checkbox("###animation_delay_Start", ref _cutsceneManager.DelayAnimationStart);
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Animation Start Delay");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(MaxItemWidth);

                using(ImRaii.Disabled(_cutsceneManager.DelayAnimationStart == false))
                {
                    ImGui.InputInt($"###animation_delay_Start_Chek", ref _cutsceneManager.DelayAnimationTime, 0, 0);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(LabelStart);
                ImGui.Text("Animation Delay");
            }

            ImGui.Separator();

            ImGui.Text("The time-scale for the delay functions are in Milliseconds!");
            ImGui.Text("1000 Milliseconds = 1 Second");

            ImGui.Separator();

            var isrunning = _cutsceneManager.IsRunning;
            using(ImRaii.Disabled(isrunning))
            {
                if(ImGui.Button("Play"))
                {
                    _cutsceneManager.StartPlayback();
                }
            }

            ImGui.SameLine();

            using(ImRaii.Disabled(!isrunning))
            {
                if(ImGui.Button("Stop"))
                {
                    _cutsceneManager.StopPlayback();
                }
            }
        }
    }

    //

    public static void ApplyBaseOverride(ActionTimelineCapability cap, bool resetSpeed = false)
    {
        if(cap.SlotedBaseAnimation == 0 || cap.IsPaused)
            return;

        if(resetSpeed || cap.SpeedMultiplier == 0)
            cap.ResetOverallSpeedOverride();

        cap.ApplyBaseOverride((ushort)cap.SlotedBaseAnimation, cap.DoBaseInterrupt);
    }
    public static void ApplyBlend(ActionTimelineCapability cap)
    {
        if(cap.SlotedBlendAnimation == 0 || cap.IsPaused)
            return;

        cap.BlendTimeline((ushort)cap.SlotedBlendAnimation);
    }
}
