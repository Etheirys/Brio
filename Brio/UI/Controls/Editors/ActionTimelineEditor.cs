using Brio.Capabilities.Actor;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using System;
using Brio.Resources;
using Brio.Game.Actor.Extensions;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Brio.UI.Controls.Editors;

internal class ActionTimelineEditor()
{
    private ActionTimelineCapability _capability = null!;

    private float MaxItemWidth => ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("XXXXXXXXXXXXXXXXXX").X;
    private float LabelStart => MaxItemWidth + ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X * 2f;

    private bool _baseInterrupt = true;

    private int _baseAnimation = 0;

    private int _blendAnimation = 0;

    private static readonly ActionTimelineSelector _globalTimelineSelector = new("global_timeline_selector");

    public void Draw(bool drawAdvanced, ActionTimelineCapability capability)
    {
        _capability = capability;

        DrawBaseOverride();
        DrawBlend();
        DrawOverallSpeed();

        if (drawAdvanced)
        {
            DrawLips();

            if (ImGui.CollapsingHeader("Scrub"))
            {
                DrawScrub();
            }

            if (ImGui.CollapsingHeader("Slots"))
            {
                DrawSlots();
            }
        }
    }

    private void DrawBaseOverride()
    {
        const string baseLabel = "Base";
        ImGui.SetNextItemWidth(MaxItemWidth - ImGui.CalcTextSize("XXXX").X);
        ImGui.InputInt($"###base_animation", ref _baseAnimation, 0, 0);
        if (ImBrio.IsItemConfirmed())
        {
            ApplyBaseOverride();
        }

        ImGui.SameLine();
        ImGui.Checkbox("###base_interrupt", ref _baseInterrupt);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Interrupt");

        ImGui.SameLine();
        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(baseLabel);

        ImGui.SameLine();

        if (ImBrio.FontIconButtonRight("base_play", FontAwesomeIcon.PlayCircle, 3, "Play", _baseAnimation != 0))
            ApplyBaseOverride();

        ImGui.SameLine();

        if (ImBrio.FontIconButtonRight("base_reset", FontAwesomeIcon.Undo, 2, "Reset", _capability.HasBaseOverride))
            _capability.ResetBaseOverride();

        ImGui.SameLine();

        if (ImBrio.FontIconButtonRight("base_search", FontAwesomeIcon.Search, 1, "Search"))
        {
            _globalTimelineSelector.Select(null, false);
            _globalTimelineSelector.AllowBlending = false;
            ImGui.OpenPopup("base_search_popup");

        }

        using (var popup = ImRaii.Popup("base_search_popup"))
        {
            if (popup.Success)
            {
                _globalTimelineSelector.Draw();

                if (_globalTimelineSelector.HoverChanged && _globalTimelineSelector.Hovered != null)
                {
                    _baseAnimation = _globalTimelineSelector.Hovered.TimelineId;
                }

                if (_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
                {
                    _baseAnimation = _globalTimelineSelector.Selected.TimelineId;
                    ApplyBaseOverride();
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
        if (ImBrio.IsItemConfirmed())
        {
            ApplyBlend();
        }

        ImGui.SameLine();

        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(blendLabel);

        ImGui.SameLine();

        if (ImBrio.FontIconButtonRight("blend_play", FontAwesomeIcon.PlayCircle, 2, "Play", _blendAnimation != 0))
            ApplyBlend();

        ImGui.SameLine();
        if (ImBrio.FontIconButtonRight("blend_search", FontAwesomeIcon.Search, 1, "Search"))
        {
            _globalTimelineSelector.Select(null, false);
            _globalTimelineSelector.AllowBlending = true;
            ImGui.OpenPopup("blend_search_popup");

        }

        using (var popup = ImRaii.Popup("blend_search_popup"))
        {
            if (popup.Success)
            {
                _globalTimelineSelector.Draw();

                if (_globalTimelineSelector.HoverChanged && _globalTimelineSelector.Hovered != null)
                {
                    _blendAnimation = _globalTimelineSelector.Hovered.TimelineId;
                }

                if (_globalTimelineSelector.SelectionChanged && _globalTimelineSelector.Selected != null)
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
        if (lipsOverride != 0)
            preview = GameDataProvider.Instance.ActionTimelines[lipsOverride].Key;

        ImGui.SetNextItemWidth(MaxItemWidth);
        using (var combo = ImRaii.Combo("###lips", preview))
        {
            if (combo.Success)
            {
                if (ImGui.Selectable($"None", lipsOverride == 0))
                {
                    _capability.LipsOverride = 0;
                }

                for (uint i = 0x272; i <= 0x272 + 8; ++i)
                {
                    var entry = GameDataProvider.Instance.ActionTimelines[i];
                    bool selected = lipsOverride == i;
                    if (ImGui.Selectable($"{entry.Key} ({i})", selected))
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
        if (drawObj == null)
            return;

        if (drawObj->Object.GetObjectType() != ObjectType.CharacterBase)
            return;

        var charaBase = (CharacterBase*)drawObj;

        if (charaBase->Skeleton == null)
            return;

        var skeleton = charaBase->Skeleton;

        for (int p = 0; p < skeleton->PartialSkeletonCount; ++p)
        {
            var partial = &skeleton->PartialSkeletons[p];
            var animatedSkele = partial->GetHavokAnimatedSkeleton(0);
            if (animatedSkele == null)
                continue;

            for (int c = 0; c < animatedSkele->AnimationControls.Length; ++c)
            {
                var control = animatedSkele->AnimationControls[c].Value;
                if (control == null)
                    continue;

                var binding = control->hkaAnimationControl.Binding;
                if (binding.ptr == null)
                    continue;

                var anim = binding.ptr->Animation.ptr;
                if (anim == null)
                    continue;

                if (control->PlaybackSpeed == 0)
                {
                    drewAny |= true;
                    var duration = anim->Duration;
                    var time = control->hkaAnimationControl.LocalTime;
                    ImGui.SetNextItemWidth(width);
                    if (ImGui.SliderFloat($"###scrub_{p}_{c}", ref time, 0f, duration, "%.2f", ImGuiSliderFlags.AlwaysClamp))
                    {
                        control->hkaAnimationControl.LocalTime = time;
                    }
                    ImGui.SameLine();
                    ImGui.Text($"{p}.{c}");
                }
            }
        }

        if (!drewAny)
            ImGui.Text("Pause motion to enable.");
    }

    private void DrawSlots()
    {

        var slots = Enum.GetValues<ActionTimelineSlots>();
        foreach (var slot in slots)
        {
            using (ImRaii.PushId((int)slot))
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

        using (ImRaii.PushId($"slot_{slot}"))
        {
            ImGui.Text(slotDescription);

            float existingSpeed = _capability.GetSlotSpeed(slot);
            float newSpeed = existingSpeed;
            const string speedLabel = "Slot Speed";
            ImGui.SetNextItemWidth(ImGui.CalcTextSize($"XXXXXXXXXXXXXXXXXi").X);
            if (ImGui.SliderFloat($"{speedLabel}", ref newSpeed, 0f, 5f))
                _capability.SetSlotSpeedOverride(slot, newSpeed);


            ImGui.SameLine();

            if (ImBrio.FontIconButtonRight("reset", FontAwesomeIcon.Undo, 1, "Reset Speed", _capability.HasSlotSpeedOverride(slot)))
                _capability.ResetSlotSpeedOverride(slot);

            ImGui.SameLine();

            var speed = _capability.GetSlotSpeed(slot);
            if (ImBrio.FontIconButtonRight("speed_pause", FontAwesomeIcon.PauseCircle, 2, "Pause", speed > 0f))
                _capability.SetSlotSpeedOverride(slot, 0.0f);
        }
    }

    private void DrawOverallSpeed()
    {
        float existingSpeed = _capability.SpeedMultiplier;
        float newSpeed = existingSpeed;

        const string speedLabel = "Speed";
        ImGui.SetNextItemWidth(MaxItemWidth);
        if (ImGui.SliderFloat($"###speed_slider", ref newSpeed, 0f, 5f))
            _capability.SetOverallSpeedOverride(newSpeed);

        ImGui.SameLine();

        ImGui.SetCursorPosX(LabelStart);
        ImGui.Text(speedLabel);

        ImGui.SameLine();

        if (ImBrio.FontIconButtonRight("speed_reset", FontAwesomeIcon.Undo, 1, "Reset Speed", _capability.HasSpeedMultiplierOverride))
            _capability.ResetOverallSpeedOverride();

        ImGui.SameLine();

        if (ImBrio.FontIconButtonRight("speed_pause", FontAwesomeIcon.PauseCircle, 2, "Pause", _capability.SpeedMultiplier > 0f))
            _capability.SetOverallSpeedOverride(0.0f);
    }

    private void ApplyBaseOverride()
    {
        if (_baseAnimation == 0)
            return;

        _capability.ApplyBaseOverride((ushort)_baseAnimation, _baseInterrupt);
    }

    private void ApplyBlend()
    {
        if (_blendAnimation == 0)
            return;

        _capability.BlendTimeline((ushort)_blendAnimation);
    }
}
