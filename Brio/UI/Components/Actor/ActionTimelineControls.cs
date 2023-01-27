using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Math = System.Math;
using System.Numerics;

namespace Brio.UI.Components.Actor;

public static class ActionTimelineControls
{
    private static int _baseTimelineInput = 0;
    private static int _blendTimelineInput = 0;
    private static bool _baseInterrupt = true;
    private static int _selectedSlot = -1;
    private static string _searchTerm = string.Empty;
    private static string _searchSelectCache = string.Empty;

    private static SearchType _searchType = SearchType.Base;

    private enum SearchType
    {
        Base,
        Blend
    }

    private enum EmoteTimelineType
    {
        Loop,
        Intro,
        Ground,
        Chair,
        UpperBody,
        Add1,
        Add2
    }

    public unsafe static void Draw(GameObject gameObject)
    {
        if(gameObject is Character managedChara)
        {
            DrawOverrideInputs(managedChara);
            DrawSlotSelect(managedChara);
            DrawSlotModifier(managedChara, _selectedSlot);
            DrawSlotScrub(managedChara, _selectedSlot);
            ActionTimelineService.Instance.UpdateAllSlots(managedChara);
            DrawSearch(managedChara);
        }
        else
        {
            ImGui.Text("Incompatible actor type.");
        }
    }

    private unsafe static void DrawOverrideInputs(Character character)
    {
        ImGui.Checkbox("###base_anim_interrupt", ref _baseInterrupt);
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Interrupt");

        ImGui.SameLine();

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 4);
        ImGui.BeginGroup();
        ImGui.InputInt("Base ", ref _baseTimelineInput, 0, 0);
        ImGui.SameLine();

        bool canBasePlay = _baseTimelineInput != 0;
        if(!canBasePlay) ImGui.BeginDisabled();
        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button(FontAwesomeIcon.Play.ToIconString() + "###base_anim_play"))
        {
            ApplyBase(character, (ushort) _baseTimelineInput);
        }
        ImGui.PopFont();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Play");
        if(!canBasePlay) ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        bool isOverride = ActionTimelineService.Instance.HasOverride(character);
        if(!isOverride) ImGui.BeginDisabled();
        if(ImGui.Button(FontAwesomeIcon.Redo.ToIconString() + "###base_anim_reset"))
        {
            ActionTimelineService.Instance.RemoveOverride(character);
        }
        if(!isOverride) ImGui.EndDisabled();
        ImGui.PopFont();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Reset");


        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button(FontAwesomeIcon.Search.ToIconString() + "###base_anim_search"))
        {
            _searchType = SearchType.Base;
            ImGui.OpenPopup("###actiontimeline_search_popup");
        }
        ImGui.PopFont();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Search");

        ImGui.EndGroup();


        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 7.5f);
        ImGui.BeginGroup();

        ImGui.InputInt("Blend", ref _blendTimelineInput, 0, 0);

        ImGui.SameLine();
        bool canBlendPlay = _blendTimelineInput != 0;
        if(!canBlendPlay) ImGui.BeginDisabled();
        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button(FontAwesomeIcon.Play.ToIconString() + "###blend_anim_play"))
        {
            PlayBlend(character, (ushort)_blendTimelineInput);
        }
        ImGui.PopFont();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Play");
        if(!canBlendPlay) ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button(FontAwesomeIcon.Search.ToIconString() + "###blend_anim_search"))
        {
            _searchType = SearchType.Blend;
            ImGui.OpenPopup("###actiontimeline_search_popup");
        }
        ImGui.PopFont();
        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Search");

        ImGui.EndGroup();

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 6f);
        ImGui.BeginGroup();

        var speedOverride = ActionTimelineService.Instance.GetSpeedOverride(character);
        ImGui.SliderFloat("Speed Multiplier", ref speedOverride.SpeedMultiplier, 0, 5f);
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button(FontAwesomeIcon.Redo.ToIconString() + "###slot_multiplier_reset"))
        {
            speedOverride.SpeedMultiplier = 1.0f;
        }
        ImGui.PopFont();

        ImGui.EndGroup();

    }

    private unsafe static void DrawSlotSelect(Character managedChara)
    {
        var chara = managedChara.AsNative();
        var oldSelectedSlot = _selectedSlot;
        _selectedSlot = -1;
        var actionTimelineSheet = Dalamud.DataManager.Excel.GetSheet<ActionTimeline>();

        ImGui.SetNextItemWidth(-1);
        if(ImGui.BeginListBox("###anim_slots", new Vector2(0, ImGui.GetTextLineHeight() * 5)))
        {
            for(int i = 0; i < ActionTimelineDriver.TimelineSlotCount; i++)
            {
                var timelineId = chara->ActionTimelineManager.Driver.TimelineIds[i];

                if(timelineId == 0)
                    continue;

                bool wasSelected = oldSelectedSlot == i;

                if(wasSelected && _selectedSlot == -1)
                    _selectedSlot = i;

                var timelineEntry = actionTimelineSheet?.GetRow(timelineId);
                string timelineKey = timelineEntry?.Key ?? "unknown";

                var slot = (ActionTimelineSlots)i;
                string description = $"{slot} ({i}): {timelineId} ({timelineKey})";

                if(ImGui.Selectable(description, wasSelected))
                {
                    _selectedSlot = i;
                }

                if(ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(description);
                }
            }

            ImGui.EndListBox();
        }
    }

    private unsafe static void DrawSlotModifier(Character character, int selectedSlot)
    {
        if(selectedSlot == -1)
            return;

        var speedOverride = ActionTimelineService.Instance.GetSpeedOverride(character);

        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 7.5f);
        ImGui.BeginGroup();
        ImGui.SliderFloat("Slot Speed", ref speedOverride.SlotModifiers[selectedSlot], 0f, 5f);
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button(FontAwesomeIcon.Redo.ToIconString() + "###slot_mspeed_reset"))
        {
            speedOverride.SlotModifiers[selectedSlot] = 1.0f;
        }
        ImGui.PopFont();
        ImGui.EndGroup();
    }

    private unsafe static void DrawSlotScrub(Character character, int selectedSlot)
    {
        if(selectedSlot == -1)
            return;

        var speedOverride = ActionTimelineService.Instance.GetSpeedOverride(character);

        var chara = character.AsNative();

        if(chara->GameObject.DrawObject == null)
            return;

        var drawObject = chara->GameObject.DrawObject;

        if(drawObject->Object.GetObjectType() != ObjectType.CharacterBase)
            return;

        var charaBase = (CharacterBase*)&drawObject->Object;

        if(charaBase->Skeleton == null)
            return;

        if(charaBase->Skeleton->PartialSkeletonCount > 0)
        {
            var partialSkele = charaBase->Skeleton->PartialSkeletons[0];
            var bodyBase = partialSkele.GetHavokAnimatedSkeleton(0);

            if(bodyBase != null)
            {
                if(bodyBase->AnimationControls.Length > selectedSlot)
                {
                    var animControls = bodyBase->AnimationControls[selectedSlot].Value;
                    var duration = animControls->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration;

                    bool isPaused = Math.Abs(speedOverride.GetEffectiveSpeed((ActionTimelineSlots)selectedSlot) - animControls->PlaybackSpeed) > 0.1f;

                    ImGui.SetNextItemWidth(ImGui.GetFontSize() * 5.5f);
                    ImGui.BeginGroup();
                    ImGui.SliderFloat("Skeleton Speed", ref animControls->PlaybackSpeed, 0, 5f);
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if(ImGui.Button(FontAwesomeIcon.Redo.ToIconString() + "###skeleton_speed_reset"))
                    {
                        animControls->PlaybackSpeed = 1.0f;
                    }
                    ImGui.PopFont();
                    ImGui.EndGroup();

                    ImGui.SetNextItemWidth(ImGui.GetFontSize() * 7.5f);
                    ImGui.BeginGroup();
                    var isFrozen = animControls->PlaybackSpeed == 0f;
                    if(!isFrozen) ImGui.BeginDisabled();
                    ImGui.SliderFloat("Skeleton Scrub", ref animControls->hkaAnimationControl.LocalTime, 0, duration - 0.05f);
                    if(!isFrozen) ImGui.EndDisabled();
                    ImGui.EndGroup();

                    if(isPaused && animControls->hkaAnimationControl.LocalTime > duration - 0.05f)
                        animControls->hkaAnimationControl.LocalTime = 0.0f;
                }
            }
        }
    }

    private static void DrawSearch(Character character)
    {
        if(ImGui.BeginPopup("###actiontimeline_search_popup"))
        {
            var actionTimelineSheet = Dalamud.DataManager.GetExcelSheet<ActionTimeline>();
            if(actionTimelineSheet != null)
            {
                if(ImGui.BeginTabBar("###actiontimeline_search_tab"))
                {

                    if(ImGui.BeginTabItem("Raw"))
                    {
                        ImGui.InputText("####actiontimeline_search_term", ref _searchTerm, 1024);

                        if(ImGui.BeginListBox("###action_timeline_search_actiontimelinelist"))
                        {
                            foreach(var timeline in actionTimelineSheet)
                            {
                                if(string.IsNullOrEmpty(timeline.Key))
                                    continue;

                                if(_searchType == SearchType.Base && timeline.Slot != (byte)ActionTimelineSlots.Base)
                                    continue;

                                var searchEntry = $"{timeline.RowId} ({timeline.Key}) ({(ActionTimelineSlots)timeline.Slot})";

                                if(!searchEntry.Contains(_searchTerm, System.StringComparison.InvariantCultureIgnoreCase))
                                    continue;

                                DrawSearchEntry(character, searchEntry, (int)timeline.RowId);
                            }

                            ImGui.EndListBox();
                        }

                        ImGui.EndTabItem();
                    }

                    if(ImGui.BeginTabItem("Emotes"))
                    {
                        ImGui.InputText("####actiontimeline_search_term", ref _searchTerm, 1024);

                        if(ImGui.BeginListBox("###action_timeline_search_emotelist"))
                        {
                            var emoteSheet = Dalamud.DataManager.GetExcelSheet<Emote>();
                            if(emoteSheet != null)
                            {
                                foreach(var emote in emoteSheet)
                                {
                                    if(string.IsNullOrEmpty(emote.Name))
                                        continue;

                                    for(int actionEntry = 0; actionEntry < emote.ActionTimeline.Length; ++actionEntry)
                                    {
                                        var actionTimelineLazy = emote.ActionTimeline[actionEntry];
                                        if(actionTimelineLazy?.Value != null)
                                        {
                                            ActionTimeline timeline = actionTimelineLazy.Value;
                                            if(timeline.RowId == 0 || string.IsNullOrEmpty(timeline.Key))
                                                continue;

                                            if(_searchType == SearchType.Base && timeline.Slot != (byte)ActionTimelineSlots.Base)
                                                continue;

                                            var emoteEntryType = (EmoteTimelineType)actionEntry;

                                            var searchEntry = $"{emote.Name} ({emoteEntryType}) - {timeline.RowId} ({timeline.Key}) ({(ActionTimelineSlots)timeline.Slot})";

                                            if(!searchEntry.Contains(_searchTerm, System.StringComparison.InvariantCultureIgnoreCase))
                                                continue;

                                            DrawSearchEntry(character, searchEntry, (int)timeline.RowId);
                                        }
                                    }
                                }
                            }

                            ImGui.EndListBox();
                        }
                        ImGui.EndTabItem();
                    }

                    if(ImGui.BeginTabItem("Actions"))
                    {

                        ImGui.InputText("####actiontimeline_search_term", ref _searchTerm, 1024);

                        if(ImGui.BeginListBox("###action_timeline_search_actionlist"))
                        {
                            var actionSheet = Dalamud.DataManager.GetExcelSheet<Action>();
                            if(actionSheet != null)
                            {
                                foreach(var action in actionSheet)
                                {
                                    if(string.IsNullOrEmpty(action.Name))
                                        continue;

                                    var timeline = action.AnimationEnd.Value;
                                    if(timeline == null || timeline.RowId == 0 || string.IsNullOrEmpty(timeline.Key))
                                        continue;

                                    if(_searchType == SearchType.Base && timeline.Slot != (byte)ActionTimelineSlots.Base)
                                        continue;

                                    var searchEntry = $"{action.Name} - {timeline.RowId} ({timeline.Key}) ({(ActionTimelineSlots)timeline.Slot})";

                                    if(!searchEntry.Contains(_searchTerm, System.StringComparison.InvariantCultureIgnoreCase))
                                        continue;

                                    DrawSearchEntry(character, searchEntry, (int)timeline.RowId);
                                }
                            }

                            ImGui.EndListBox();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }


            }
            ImGui.EndPopup();
        }
    }

    private static void DrawSearchEntry(Character character, string text, int id)
    {
        bool selected = _searchSelectCache == text;

        if(ImGui.Selectable(text, selected))
        {
            if(_searchType == SearchType.Base)
                _baseTimelineInput = id;

            if(_searchType == SearchType.Blend)
                _blendTimelineInput = id;

            _searchSelectCache = text;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(text);

        if(ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
        {
            ImGui.CloseCurrentPopup();
            if(_searchType == SearchType.Base)
                ApplyBase(character, (ushort) _baseTimelineInput);

            if(_searchType == SearchType.Blend)
                PlayBlend(character, (ushort)_blendTimelineInput);

        }
    }

    private static void ApplyBase(Character character, ushort id) => ActionTimelineService.Instance.ApplyBaseOverride(character, (ushort)_baseTimelineInput, _baseInterrupt);
    private static void PlayBlend(Character character, ushort id) => ActionTimelineService.Instance.Blend(character, (ushort)_blendTimelineInput);

}
