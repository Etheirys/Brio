using Brio.Resources;
using Brio.Resources.Sheets;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using System;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;

namespace Brio.UI.Controls.Selectors;

public class ActionTimelineSelector(string id) : Selector<ActionTimelineSelectorEntry>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    private bool _showRaw = true;
    private bool _showEmotes = true;
    private bool _showActions = false;
    private bool _showBlendable = true;

    private bool _filterByDrawsWeapon = false;
    private bool _drawsWeaponValue = false;

    private bool _filterByEmoteCategory = false;
    private int _emoteCategoryValue = 0;

    private bool _showNonBlendInBlendMode = false;

    private bool _isPinned = false;
    private bool _isWindowOpen = false;

    public bool IsPinned => _isPinned;

    public bool AllowBlending
    {
        get => _showBlendable;
        set
        {
            _showBlendable = value;
            UpdateList();
        }
    }

    public void TogglePin()
    {
        _isPinned = !_isPinned;
        if(_isPinned)
        {
            _isWindowOpen = true;
        }
    }

    public void DrawAsWindow()
    {
        if(!_isPinned)
            return;

        ImGui.SetNextWindowSize(new Vector2(400, 500), ImGuiCond.FirstUseEver);

        if(ImGui.Begin($"Animation Search Selector ###{_id}_window2", ref _isWindowOpen, ImGuiWindowFlags.NoCollapse))
        {
            if(!_isWindowOpen)
            {
                _isPinned = false;
                ImGui.End();
                return;
            }

            DrawPinButton();

            // Use available window space instead of adaptive sizing
            _useAvailableSpace = true;
            base.Draw();
            _useAvailableSpace = false;
        }
        ImGui.End();
    }

    private void DrawPinButton()
    {
        var pinIcon = _isPinned ? FontAwesomeIcon.Thumbtack : FontAwesomeIcon.Thumbtack;
        var pinColor = _isPinned ? UIConstants.GizmoRed : UIConstants.ToggleButtonInactive;

        var tooltip = _isPinned ? "Unpin (close window)" : "Pin to keep open";

        if(ImBrio.FontIconButton($"pin_toggle_{_id}", pinIcon, tooltip, true, true, pinColor))
        {
            TogglePin();
        }

        ImGui.SameLine();
    }

    public new void Draw()
    {
        if(_isPinned)
        {
            ImGui.TextDisabled("(Selector is pinned as separate window)");
            return;
        }

        DrawPinButton();

        base.Draw();
    }

    protected override void PopulateList()
    {
        foreach(var timeline in GameDataProvider.Instance.ActionTimelines.Values)
        {
            if(!string.IsNullOrEmpty(timeline.Key.ToString()))
                AddItem(new ActionTimelineSelectorEntry(
                    timeline.Key.ToString(),
                    (ushort)timeline.RowId,
                    timeline.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Raw,
                    ActionTimelineSelectorEntry.AnimationPurpose.Unknown,
                    (ActionTimelineSlots)timeline.Slot,
                    0,
                    false,
                    0));
        }

        foreach(var emote in GameDataProvider.Instance.Emotes.Values)
        {
            BrioActionTimeline timeline;
            bool drawsWeapon = emote.DrawsWeapon;
            byte emoteCategory = (byte)emote.EmoteCategory.RowId;

            // Loop
            if(emote.ActionTimeline[0].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetValue(emote.ActionTimeline[0].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Standard,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Intro
            if(emote.ActionTimeline[1].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetValue(emote.ActionTimeline[1].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Intro,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Ground
            if(emote.ActionTimeline[2].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetValue(emote.ActionTimeline[2].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Ground,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Chair
            if(emote.ActionTimeline[3].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetValue(emote.ActionTimeline[3].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Chair,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Upper Body
            if(emote.ActionTimeline[4].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetValue(emote.ActionTimeline[4].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Blend,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }
        }

        foreach(var action in GameDataProvider.Instance.Actions.Values)
        {
            if(action.AnimationEnd.RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetValue(action.AnimationEnd.RowId, out BrioActionTimeline timeline))
                AddItem(new ActionTimelineSelectorEntry(
                    action.Name.ToString(),
                    (ushort)action.AnimationEnd.RowId,
                    action.RowId,
                    action.AnimationEnd.Value.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Action,
                    ActionTimelineSelectorEntry.AnimationPurpose.Action,
                    (ActionTimelineSlots)timeline.Slot,
                    action.Icon,
                    false,
                    0));
        }
    }

    protected override void DrawItem(ActionTimelineSelectorEntry item, bool isSoftSelected)
    {
        var description = $"{item.Name}\n{item.SecondaryId} {item.TimelineType} {item.Slot} {item.Purpose}\n{item.TimelineId} {item.Key}";

        ImBrio.BorderedGameIcon("icon", item.Icon, "Images.ActionTimeline.png", description, flags: ImGuiButtonFlags.None, size: IconSize);
    }

    protected override void DrawTooltip(ActionTimelineSelectorEntry item)
    {
        ImGui.SetTooltip($"{item.Name}\n{item.TimelineId} - {item.Key}");
    }

    protected override void DrawOptions()
    {
        bool[] items = [_showEmotes, _showActions, _showRaw];

        var changed = ImBrio.ToggleSelecterStrip("actiontimeline_filters_selector", Vector2.Zero, ref items, ["Emotes", "Actions", "Timelines"]);

        if(changed)
        {
            _showEmotes = items[0];
            _showActions = items[1];
            _showRaw = items[2];

            UpdateList();
        }

        ImBrio.VerticalPadding(4);

        if(_showBlendable)
        {
            if(ImGui.Checkbox("Show Non-Blend Animations", ref _showNonBlendInBlendMode))
                UpdateList();

            ImBrio.VerticalPadding(2);
        }

        if(!_showBlendable)
        {
            ImGui.Text("Draws Weapon");
            ImBrio.VerticalPadding(1);

            int drawsWeaponSelection = !_filterByDrawsWeapon ? 0 : (_drawsWeaponValue ? 2 : 1);

            if(ImBrio.ButtonSelectorStrip("draws_weapon_filter", Vector2.Zero, ref drawsWeaponSelection, ["All", "Sheathed", "Drawn"]))
            {
                switch(drawsWeaponSelection)
                {
                    case 0:
                        _filterByDrawsWeapon = false;
                        _drawsWeaponValue = false;
                        break;
                    case 1:
                        _filterByDrawsWeapon = true;
                        _drawsWeaponValue = false;
                        break;
                    case 2:
                        _filterByDrawsWeapon = true;
                        _drawsWeaponValue = true;
                        break;
                }

                UpdateList();
            }

            ImBrio.VerticalPadding(4);
        }

        ImGui.Text("Emote Category");
        ImBrio.VerticalPadding(1);

        int emoteCategorySelection = _emoteCategoryValue;

        if(ImBrio.ButtonSelectorStrip("emote_category_filter", Vector2.Zero, ref emoteCategorySelection, ["All", "General", "Special", "Expressions"]))
        {
            _emoteCategoryValue = emoteCategorySelection;
            _filterByEmoteCategory = _emoteCategoryValue != 0;
            UpdateList();
        }

        ImBrio.VerticalPadding(3);
    }

    protected override int Compare(ActionTimelineSelectorEntry itemA, ActionTimelineSelectorEntry itemB)
    {
        // Emotes first
        if(itemA.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote && itemB.TimelineType != ActionTimelineSelectorEntry.OriginalType.Emote)
            return -1;

        if(itemA.TimelineType != ActionTimelineSelectorEntry.OriginalType.Emote && itemB.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            return 1;

        // Then Actions
        if(itemA.TimelineType == ActionTimelineSelectorEntry.OriginalType.Action && itemB.TimelineType != ActionTimelineSelectorEntry.OriginalType.Action)
            return -1;

        if(itemA.TimelineType != ActionTimelineSelectorEntry.OriginalType.Action && itemB.TimelineType == ActionTimelineSelectorEntry.OriginalType.Action)
            return 1;

        // Blank to last
        if(string.IsNullOrEmpty(itemA.Name) && !string.IsNullOrEmpty(itemB.Name))
            return 1;

        if(!string.IsNullOrEmpty(itemA.Name) && string.IsNullOrEmpty(itemB.Name))
            return -1;

        // Alphabetical
        var comp = string.Compare(itemA.Name, itemB.Name, StringComparison.InvariantCultureIgnoreCase);
        if(comp != 0)
            return comp;

        // Base Actions first
        if(itemA.Slot == ActionTimelineSlots.Base && itemB.Slot != ActionTimelineSlots.Base)
            return -1;

        if(itemA.Slot != ActionTimelineSlots.Base && itemB.Slot == ActionTimelineSlots.Base)
            return 1;

        return 0;
    }

    protected override bool Filter(ActionTimelineSelectorEntry item, string search)
    {
        if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote && !_showEmotes)
            return false;

        if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Action && !_showActions)
            return false;

        if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Raw && !_showRaw)
            return false;

        if(item.Slot != ActionTimelineSlots.Base && !_showBlendable)
            return false;

        // When in blend mode, filter out non-blend animations unless option is enabled
        if(_showBlendable && !_showNonBlendInBlendMode)
        {
            if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            {
                if(item.Purpose != ActionTimelineSelectorEntry.AnimationPurpose.Blend)
                    return false;
            }
        }

        if(_filterByDrawsWeapon)
        {
            if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            {
                if(item.DrawsWeapon != _drawsWeaponValue)
                    return false;
            }
        }

        if(_filterByEmoteCategory)
        {
            if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            {
                if(item.EmoteCategory != _emoteCategoryValue)
                    return false;
            }
        }

        var searchText = $"{item.Name} {item.TimelineId} {item.TimelineType} {item.Slot} {item.Purpose} {item.Key} {item.SecondaryId}";

        if(searchText.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }
}

public record class ActionTimelineSelectorEntry(
    string Name,
    ushort TimelineId,
    uint SecondaryId,
    string Key,
    ActionTimelineSelectorEntry.OriginalType TimelineType,
    ActionTimelineSelectorEntry.AnimationPurpose Purpose,
    ActionTimelineSlots Slot,
    uint Icon,
    bool DrawsWeapon,
    byte EmoteCategory)
{

    public enum AnimationPurpose
    {
        Unknown,
        Action,
        Standard,
        Intro,
        Ground,
        Chair,
        Blend,
    }

    public enum OriginalType
    {
        Raw,
        Emote,
        Action,
    }
}
