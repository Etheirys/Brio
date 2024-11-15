using Brio.Resources;
using Brio.UI.Controls.Stateless;
using ImGuiNET;
using System;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;

namespace Brio.UI.Controls.Selectors;

internal class ActionTimelineSelector(string id) : Selector<ActionTimelineSelectorEntry>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;


    private bool _showRaw = true;
    private bool _showEmotes = true;
    private bool _showActions = true;

    private bool _showBlendable = true;

    public bool AllowBlending
    {
        get => _showBlendable;
        set
        {
            _showBlendable = value;
            UpdateList();
        }
    }

    protected override void PopulateList()
    {
        foreach(var timeline in GameDataProvider.Instance.ActionTimelines.Values)
        {
            if(!string.IsNullOrEmpty(timeline.Key))
                AddItem(new ActionTimelineSelectorEntry(timeline.Key, (ushort)timeline.RowId, timeline.RowId, timeline.Key, ActionTimelineSelectorEntry.OriginalType.Raw, ActionTimelineSelectorEntry.AnimationPurpose.Unknown, (ActionTimelineSlots)timeline.Slot, 0));
        }
        foreach(var emote in GameDataProvider.Instance.Emotes.Values)
        {
            // Loop
            var timeline = emote.ActionTimeline[0];
            if(timeline.Value != null && timeline.RowId != 0)
            {
                AddItem(new ActionTimelineSelectorEntry(emote.Name, (ushort)timeline.RowId, emote.RowId, timeline.Value.Key, ActionTimelineSelectorEntry.OriginalType.Emote, ActionTimelineSelectorEntry.AnimationPurpose.Standard, (ActionTimelineSlots)timeline.Value.Slot, emote.Icon));
            }

            // Intro
            timeline = emote.ActionTimeline[1];
            if(timeline.Value != null && timeline.RowId != 0)
            {
                AddItem(new ActionTimelineSelectorEntry(emote.Name, (ushort)timeline.RowId, emote.RowId, timeline.Value.Key, ActionTimelineSelectorEntry.OriginalType.Emote, ActionTimelineSelectorEntry.AnimationPurpose.Intro, (ActionTimelineSlots)timeline.Value.Slot, emote.Icon));
            }

            // Ground
            timeline = emote.ActionTimeline[2];
            if(timeline.Value != null && timeline.RowId != 0)
            {
                AddItem(new ActionTimelineSelectorEntry(emote.Name, (ushort)timeline.RowId, emote.RowId, timeline.Value.Key, ActionTimelineSelectorEntry.OriginalType.Emote, ActionTimelineSelectorEntry.AnimationPurpose.Ground, (ActionTimelineSlots)timeline.Value.Slot, emote.Icon));
            }

            // Chair
            timeline = emote.ActionTimeline[3];
            if(timeline.Value != null && timeline.RowId != 0)
            {
                AddItem(new ActionTimelineSelectorEntry(emote.Name, (ushort)timeline.RowId, emote.RowId, timeline.Value.Key, ActionTimelineSelectorEntry.OriginalType.Emote, ActionTimelineSelectorEntry.AnimationPurpose.Chair, (ActionTimelineSlots)timeline.Value.Slot, emote.Icon));
            }

            // Upper Body
            timeline = emote.ActionTimeline[4];
            if(timeline.Value != null && timeline.RowId != 0)
            {
                AddItem(new ActionTimelineSelectorEntry(emote.Name, (ushort)timeline.RowId, emote.RowId, timeline.Value.Key, ActionTimelineSelectorEntry.OriginalType.Emote, ActionTimelineSelectorEntry.AnimationPurpose.Blend, (ActionTimelineSlots)timeline.Value.Slot, emote.Icon));
            }
        }

        foreach(var action in GameDataProvider.Instance.Actions.Values)
        {
            if(action.AnimationEnd.Value != null && action.AnimationEnd.RowId != 0)
                AddItem(new ActionTimelineSelectorEntry(action.Name, (ushort)action.AnimationEnd.RowId, action.RowId, action.AnimationEnd.Value.Key, ActionTimelineSelectorEntry.OriginalType.Action, ActionTimelineSelectorEntry.AnimationPurpose.Action, (ActionTimelineSlots)action.AnimationEnd.Value.Slot, action.Icon));

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
        if(ImGui.Checkbox("Emotes", ref _showEmotes))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Actions", ref _showActions))
            UpdateList();

        ImGui.SameLine();

        if(ImGui.Checkbox("Timelines", ref _showRaw))
            UpdateList();
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

        var searchText = $"{item.Name} {item.TimelineId} {item.TimelineType} {item.Slot} {item.Purpose} {item.Key}";

        if(searchText.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }
}

internal record class ActionTimelineSelectorEntry(string Name, ushort TimelineId, uint SecondaryId, string Key, ActionTimelineSelectorEntry.OriginalType TimelineType, ActionTimelineSelectorEntry.AnimationPurpose Purpose, ActionTimelineSlots Slot, uint Icon)
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
