using Brio.Capabilities.Actor;
using Brio.Capabilities.Core;
using Brio.Config;
using Brio.Game.Actor.Extensions;
using Brio.Game.Cutscene;
using Brio.Game.Cutscene.Files;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Resources;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;

namespace Brio.UI.Controls.Editors;

internal class ActionTimelineEditor
{
    private readonly CutsceneManager _cutsceneManager;
    private readonly GPoseService _gPoseService;
    private readonly PhysicsService _physicsService;
    private readonly ConfigurationService _configService;

    private static float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXX").X;
    private static float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X * 2f;

    private ActionTimelineCapability _capability = null!;

    private bool _baseInterrupt = true;
    private int _baseAnimation = 0;
    private int _blendAnimation = 0;
    private bool _isPaused = false;

    private static readonly ActionTimelineSelector _globalTimelineSelector = new("global_timeline_selector");

    public ActionTimelineEditor(CutsceneManager cutsceneManager, GPoseService gPoseService, PhysicsService physicsService, ConfigurationService configService)
    {
        _cutsceneManager = cutsceneManager;
        _gPoseService = gPoseService;
        _physicsService = physicsService;
        _configService = configService;
    }

    public void Draw(bool drawAdvanced, ActionTimelineCapability capability)
    {
        _capability = capability;

        DrawHeder();

        ImGui.Separator();

        DrawBaseOverride();
        DrawBlend();
        DrawOverallSpeed();

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
        if(ImBrio.ToggelButton("Freeze Physics", new Vector2(95, 0), _physicsService.IsFreezeEnabled, hoverText: _physicsService.IsFreezeEnabled ? "Un-Freeze Physics" : "Freeze Physics"))
        {
            _physicsService.FreezeToggle();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "Reset Animation", 
            _baseAnimation != 0 && 
            (_capability.HasBaseOverride || _capability.HasSpeedMultiplierOverride || _baseInterrupt is false || _capability.LipsOverride > 0)))
        {
            _baseInterrupt = true;

            _baseAnimation = 0;
            _blendAnimation = 0;
            _capability.LipsOverride = 0;
            
            Pause(true);

            _capability.ResetBaseOverride();
            _capability.ResetOverallSpeedOverride();

            _cutsceneManager.StopPlayback();

            cameraPath = string.Empty;
            _cutsceneManager.CameraPath = null;
        }
    }

    private void DrawBaseOverride()
    {
        const string baseLabel = "Base";
        ImGui.SetNextItemWidth(MaxItemWidth - ImGui.CalcTextSize("XXXX").X);
        ImGui.InputInt($"###base_animation", ref _baseAnimation, 0, 0);
        if(ImBrio.IsItemConfirmed())
        {
            ApplyBaseOverride(true);
        }

        ImGui.SameLine();
        ImGui.Checkbox("###base_interrupt", ref _baseInterrupt);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Interrupt");

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(baseLabel);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_play", FontAwesomeIcon.PlayCircle, 3, "Play", _baseAnimation != 0))
            ApplyBaseOverride();

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("base_reset", FontAwesomeIcon.StopCircle, 2, "Stop", _capability.HasBaseOverride))
        {
            Pause(true);
            
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
                _globalTimelineSelector.Draw();

                if(_globalTimelineSelector.SoftSelectionChanged && _globalTimelineSelector.SoftSelected != null)
                {
                    _baseAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
                }

                if(_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
                {
                    _baseAnimation = _globalTimelineSelector.Selected.TimelineId;
                  
                    ApplyBaseOverride(true);

                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private void DrawBlend()
    {
        const string blendLabel = "Blend";

        ImGui.SetNextItemWidth(MaxItemWidth);
        ImGui.InputInt($"###blend_animation", ref _blendAnimation, 0, 0);
        if(ImBrio.IsItemConfirmed())
        {
            ApplyBlend();
        }

        ImGui.SameLine();

        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(blendLabel);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("blend_play", FontAwesomeIcon.PlayCircle, 2, "Play", _blendAnimation != 0))
            ApplyBlend();

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
                    _blendAnimation = _globalTimelineSelector.SoftSelected.TimelineId;
                }

                if(_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
                {
                    _blendAnimation = _globalTimelineSelector.Selected.TimelineId;
                    ApplyBlend();
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
            preview = GameDataProvider.Instance.ActionTimelines[lipsOverride].Key;

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
        bool drewAny = false;

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

                if(control->PlaybackSpeed == 0)
                {
                    drewAny |= true;
                    var duration = anim->Duration;
                    var time = control->hkaAnimationControl.LocalTime;
                    ImGui.SetNextItemWidth(width);
                    if(ImGui.SliderFloat($"###scrub_{p}_{c}", ref time, 0f, duration, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                    {
                        control->hkaAnimationControl.LocalTime = time;
                    }
                    ImGui.SameLine();
                    ImGui.Text($"{p}.{c}");
                }
            }
        }

        if(!drewAny)
            ImGui.Text("Pause motion to enable.");
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

    private void DrawOverallSpeed()
    {
        float existingSpeed = _capability.SpeedMultiplier;
        float newSpeed = existingSpeed;

        const string speedLabel = "Speed";
        ImGui.SetNextItemWidth(MaxItemWidth);
        if(ImGui.SliderFloat($"###speed_slider", ref newSpeed, 0f, 5f))
            _capability.SetOverallSpeedOverride(newSpeed);

        ImGui.SameLine();

        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(speedLabel);

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("speed_reset", FontAwesomeIcon.Undo, 1, "Reset Speed", _capability.HasSpeedMultiplierOverride))
            _capability.ResetOverallSpeedOverride();

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight("speed_pause", FontAwesomeIcon.PauseCircle, 2, _isPaused ? "Un-Pause" : "Paused", _capability.SpeedMultiplier > 0f))
        {
            _capability.SetOverallSpeedOverride(0f);
            Pause();
        }
    }

    //

    public string cameraPath = string.Empty;
    private void DrawCutscene()
    {
        ImGui.Text("Camera Path ");

        ImGui.SameLine();

        ImGui.InputText(string.Empty, ref cameraPath, 260, ImGuiInputTextFlags.ReadOnly);

        ImGui.SameLine();

        if(ImGui.Button("Browse"))
        {
            UIManager.Instance.FileDialogManager.OpenFileDialog("Browse for XAT Camera File", "XAT Camera File {.xcp}",
                (success, path) =>
                {
                    if(success)
                    {
                        cameraPath = path[0];

                        string? folderPath = Path.GetDirectoryName(cameraPath);
                        if(folderPath is not null)
                        {
                            _configService.Configuration.LastXATPath = folderPath;
                            _configService.Save();

                            _cutsceneManager.CameraPath = new XATCameraPathFile(new BinaryReader(File.OpenRead(cameraPath)));
                        }
                    }
                    else
                    {
                        cameraPath = string.Empty;
                        _cutsceneManager.CameraPath = null;
                    }
                }, 1, _configService.Configuration.LastXATPath, false);
        }

        ImGui.Separator();

        if(_cutsceneManager.CameraPath is null) ImGui.BeginDisabled();

        ImGui.InputFloat3("Scale", ref _cutsceneManager.CameraSettings.Scale);
        ImGui.InputFloat3("Offset", ref _cutsceneManager.CameraSettings.Offset);

        ImGui.Checkbox("Loop", ref _cutsceneManager.CameraSettings.Loop);

        ImGui.Separator();

        ImGui.Checkbox("Hide Brio On Play  (Press 'Ctrl + B' to Stop Cutscene)", ref _cutsceneManager.CloseWindowsOnPlay);
        ImGui.Checkbox("Start Animation On Play", ref _cutsceneManager.StartAnimationOnPlay);

        ImGui.Separator();

        var enb = _cutsceneManager.IsRunning;

        if(enb) ImGui.BeginDisabled();
        if(ImGui.Button("Play"))
        {
            if(_cutsceneManager.StartAnimationOnPlay && _baseAnimation != 0)
            {
                ApplyBaseOverride();
            }
            _cutsceneManager.StartPlayback();
        }
        if(enb) ImGui.EndDisabled();

        ImGui.SameLine();

        enb = _cutsceneManager.IsRunning;

        if(!enb) ImGui.BeginDisabled();
        if(ImGui.Button("Stop"))
        {
            _cutsceneManager.StopPlayback();
        }
        if(!enb) ImGui.EndDisabled();

        if(_cutsceneManager.CameraPath is null) ImGui.EndDisabled();

        if(!_gPoseService.IsGPosing)
            ImGui.EndDisabled();
    }

    //

    private unsafe void Pause(bool unPauseOverride = false)
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

        var count = skeleton->PartialSkeletonCount;
        if(count > 0)
        {
            for(var i = 0; count > i; i++)
            {
                var partial = &skeleton->PartialSkeletons[i];
                var animatedSkele = partial->GetHavokAnimatedSkeleton(0);

                if(animatedSkele == null)
                    return;

                var len = animatedSkele->AnimationControls.Length;
                if(len > 0)
                {
                    for(var j = 0; len >= j; j++)
                    {
                        var control = animatedSkele->AnimationControls[j].Value;

                        if(control == null)
                            return;

                        if(control->PlaybackSpeed == 0 || unPauseOverride)
                        {
                            Brio.Log.Warning("UnPaused " + j);
                            control->PlaybackSpeed = 1;
                            _isPaused = false;
                        }
                        else
                        {
                            Brio.Log.Warning("Paused " + j);
                            control->PlaybackSpeed = 0;
                            _isPaused = true;
                        }
                    }
                }
            }
        }
    }

    private void ApplyBaseOverride(bool resetSpeed = false)
    {
        if(_baseAnimation == 0 || _isPaused)
            return;

        if(resetSpeed || _capability.SpeedMultiplier == 0)
            _capability.ResetOverallSpeedOverride();

        _capability.ApplyBaseOverride((ushort)_baseAnimation, _baseInterrupt);
    }

    private void ApplyBlend()
    {
        if(_blendAnimation == 0 || _isPaused)
            return;

        _capability.BlendTimeline((ushort)_blendAnimation);
    }
}
