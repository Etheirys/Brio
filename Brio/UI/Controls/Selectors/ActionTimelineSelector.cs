using Brio.IPC;
using Brio.Resources;
using Brio.Resources.Sheets;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;
using ActionSheet = Lumina.Excel.Sheets.Action;

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

    private IGameObject? _modActionActor;
    private IReadOnlyList<PenumbraModAction> _modActions = [];
    private int _modActionVersion = -1;

    public bool IsPinned => _isPinned;

    public IGameObject? ModActionActor
    {
        get => _modActionActor;
        set
        {
            var changed = _modActionActor?.ObjectIndex != value?.ObjectIndex;
            _modActionActor = value;
            if(changed)
                _modActionVersion = -1;
        }
    }

    //TODO(KEN) at some point make all of them use `field`

    public bool AllowBlending
    {
        get => _showBlendable;
        set
        {
            _showBlendable = value;
            UpdateList();
        }
    }

    public bool ExpressionsOnly
    {
        get => field;
        set
        {
            field = value;
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
        RefreshModActions();

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

            ImBrio.BlurWindow(ImGuiWindowFlags.None);

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
        var pinColor = _isPinned ? UIConstants.GizmoRed : ThemeManager.CurrentTheme.Text.Text;

        var tooltip = _isPinned ? "Unpin (close window)" : "Pin to keep open";

        if(ImBrio.FontIconButton($"pin_toggle_{_id}", pinIcon, tooltip, true, true, pinColor))
        {
            TogglePin();
        }

        ImGui.SameLine();
    }

    public new void Draw()
    {
        RefreshModActions();

        if(_isPinned)
        {
            ImGui.TextDisabled("(Selector is pinned as separate window)");
            return;
        }

        DrawPinButton();

        base.Draw();
    }

    private void RefreshModActions()
    {
        if(_modActionActor is null || !Brio.TryGetService<PenumbraModActionService>(out var service))
            return;

        var actions = service.GetActiveActions(_modActionActor);
        if(_modActionVersion == service.Version)
            return;

        _modActions = actions;
        _modActionVersion = service.Version;
        ReloadList();
    }

    protected override void PopulateList()
    {
        foreach(var timeline in GameDataProvider.Instance.ActionTimelines)
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

        foreach(var emote in GameDataProvider.Instance.GetExcelSheet<Emote>())
        {
            BrioActionTimeline timeline;
            bool drawsWeapon = emote.DrawsWeapon;
            byte emoteCategory = (byte)emote.EmoteCategory.RowId;

            // Loop
            if(emote.ActionTimeline[0].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[0].RowId, out timeline))
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
            if(emote.ActionTimeline[1].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[1].RowId, out timeline))
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
            if(emote.ActionTimeline[2].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[2].RowId, out timeline))
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
            if(emote.ActionTimeline[3].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[3].RowId, out timeline))
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
            if(emote.ActionTimeline[4].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[4].RowId, out timeline))
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

        foreach(var action in GameDataProvider.Instance.GetExcelSheet<ActionSheet>())
        {
            if(action.AnimationEnd.RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(action.AnimationEnd.RowId, out BrioActionTimeline timeline))
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

        foreach(var pose in CommonPoseCatalog.All)
        {
            if(pose.TimelineId > ushort.MaxValue
                || !GameDataProvider.Instance.ActionTimelines.TryGetRow(pose.TimelineId, out BrioActionTimeline timeline))
                continue;

            AddItem(new ActionTimelineSelectorEntry(
                pose.DisplayName,
                (ushort)pose.TimelineId,
                pose.TimelineId,
                timeline.Key.ToString(),
                ActionTimelineSelectorEntry.OriginalType.Pose,
                ActionTimelineSelectorEntry.AnimationPurpose.Standard,
                (ActionTimelineSlots)timeline.Slot,
                0,
                false,
                0));
        }

        foreach(var modAction in _modActions)
        {
            if(modAction.TimelineId is uint timelineId)
            {
                AddModTimeline(modAction, timelineId, ActionTimelineSelectorEntry.AnimationPurpose.Standard);
            }
            else
            {
                AddModTimeline(modAction, 0, ActionTimelineSelectorEntry.AnimationPurpose.Standard);
                AddModTimeline(modAction, 1, ActionTimelineSelectorEntry.AnimationPurpose.Intro);
                AddModTimeline(modAction, 2, ActionTimelineSelectorEntry.AnimationPurpose.Ground);
                AddModTimeline(modAction, 3, ActionTimelineSelectorEntry.AnimationPurpose.Chair);
                AddModTimeline(modAction, 4, ActionTimelineSelectorEntry.AnimationPurpose.Blend);
            }
        }
    }

    private void AddModTimeline(PenumbraModAction modAction, int timelineIndex, ActionTimelineSelectorEntry.AnimationPurpose purpose)
    {
        if(modAction.Emote is not Emote emote)
            return;

        var timelineId = emote.ActionTimeline[timelineIndex].RowId;
        AddModTimeline(modAction, timelineId, purpose);
    }

    private void AddModTimeline(PenumbraModAction modAction, uint timelineId, ActionTimelineSelectorEntry.AnimationPurpose purpose)
    {
        var emote = modAction.Emote;
        if(timelineId == 0 || timelineId > ushort.MaxValue
            || !GameDataProvider.Instance.ActionTimelines.TryGetRow(timelineId, out BrioActionTimeline timeline))
            return;

        AddItem(new ActionTimelineSelectorEntry(
            $"{modAction.ModName} - {modAction.EmoteName}",
            (ushort)timelineId,
            emote?.RowId ?? timelineId,
            timeline.Key.ToString(),
            ActionTimelineSelectorEntry.OriginalType.Mod,
            purpose,
            (ActionTimelineSlots)timeline.Slot,
            emote?.Icon ?? 0,
            emote?.DrawsWeapon ?? false,
            emote is Emote value ? (byte)value.EmoteCategory.RowId : (byte)0));
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
        if(ExpressionsOnly)
            return;

        bool[] items = [_showEmotes, _showActions, _showRaw];

        var changed = ImBrio.ToggleSelecterStrip("actiontimeline_filters_selector", Vector2.Zero, ref items,
            ["Emotes", "Actions", "Timelines"]);

        if(changed)
        {
            _showEmotes = items[0];
            _showActions = items[1];
            _showRaw = items[2];

            UpdateList();
        }

        if(_emoteCategoryValue == 4 && _modActions.Count == 0
            && Brio.TryGetService<PenumbraModActionService>(out var modActionService))
            ImGui.TextDisabled(modActionService.StatusMessage);

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

            if(ImBrio.ButtonSelectorStrip("draws_weapon_filter", Vector2.Zero, ref drawsWeaponSelection,
                ["All", "Sheathed", "Drawn"]))
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

        if(ImBrio.ButtonSelectorStrip("emote_category_filter", Vector2.Zero, ref emoteCategorySelection,
            ["All", "General", "Special", "Expression", "Mod", "Poses"]))
        {
            _emoteCategoryValue = emoteCategorySelection;
            _filterByEmoteCategory = _emoteCategoryValue is >= 1 and <= 3;
            UpdateList();
        }

        ImBrio.VerticalPadding(3);
    }

    protected override int Compare(ActionTimelineSelectorEntry itemA, ActionTimelineSelectorEntry itemB)
    {
        var typeCompare = TypePriority(itemA.TimelineType).CompareTo(TypePriority(itemB.TimelineType));
        if(typeCompare != 0)
            return typeCompare;

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

    private static int TypePriority(ActionTimelineSelectorEntry.OriginalType type)
        => type switch
        {
            ActionTimelineSelectorEntry.OriginalType.Emote => 0,
            ActionTimelineSelectorEntry.OriginalType.Pose => 1,
            ActionTimelineSelectorEntry.OriginalType.Mod => 2,
            ActionTimelineSelectorEntry.OriginalType.Action => 3,
            _ => 4,
        };

    protected override bool Filter(ActionTimelineSelectorEntry item, string search)
    {
        var searchText = $"{item.Name} {item.TimelineId} {item.TimelineType} {item.Slot} {item.Purpose} {item.Key} {item.SecondaryId}";

        if(!searchText.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return false;

        var isEmote = item.TimelineType is ActionTimelineSelectorEntry.OriginalType.Emote
            or ActionTimelineSelectorEntry.OriginalType.Mod
            or ActionTimelineSelectorEntry.OriginalType.Pose;

        if(ExpressionsOnly)
            return isEmote && item.EmoteCategory == 3 && item.Purpose == ActionTimelineSelectorEntry.AnimationPurpose.Blend;

        if(isEmote && !_showEmotes)
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
            if(isEmote)
            {
                if(item.Purpose != ActionTimelineSelectorEntry.AnimationPurpose.Blend)
                    return false;
            }
        }

        if(_filterByDrawsWeapon)
        {
            if(isEmote)
            {
                if(item.DrawsWeapon != _drawsWeaponValue)
                    return false;
            }
        }

        if(_filterByEmoteCategory)
        {
            if(item.TimelineType is ActionTimelineSelectorEntry.OriginalType.Mod or ActionTimelineSelectorEntry.OriginalType.Pose)
                return false;

            if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            {
                if(item.EmoteCategory != _emoteCategoryValue)
                    return false;
            }
        }

        if(_emoteCategoryValue == 4 && item.TimelineType != ActionTimelineSelectorEntry.OriginalType.Mod)
            return false;

        if(_emoteCategoryValue == 5 && item.TimelineType != ActionTimelineSelectorEntry.OriginalType.Pose)
            return false;

        return true;
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
        Mod,
        Pose,
    }
}
