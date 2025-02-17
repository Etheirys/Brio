using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Brio.UI.Controls.Selectors;

public abstract class Selector<T> where T : class
{
    public T? Selected => _selected;
    public T? SoftSelected => _softSelected;

    public bool SoftSelectionChanged { get; private set; }
    public bool SelectionChanged { get; private set; }

    protected string _id;

    protected volatile T? _selected;
    protected volatile T? _softSelected;


    private readonly List<T> _items = [];
    private List<T>? _filteredAndSortedItems;

    private string _search = "";

    private bool _scrollToSelected = false;
    private bool _shouldFocusSearch = false;

    protected abstract Vector2 MinimumListSize { get; }
    protected abstract float EntrySize { get; }
    protected abstract SelectorFlags Flags { get; }

    private Task _taskQueue = Task.CompletedTask;

    public Selector(string id)
    {
        _id = id;
        _filteredAndSortedItems = null;

        InitList();
    }

    public void Select(T? selected, bool shouldScroll = true, bool shouldUpdate = true, bool shouldClear = false)
    {
        _selected = selected;
        _softSelected = selected;

        if(selected != null)
            _shouldFocusSearch = true;

        if(shouldScroll)
            _scrollToSelected = true;

        if(shouldUpdate)
            UpdateList(shouldClear);
    }

    public void ClearSearch()
    {
        _search = string.Empty;
    }

    public void Draw()
    {
        var items = _filteredAndSortedItems;

        SoftSelectionChanged = false;
        SelectionChanged = false;

        using(ImRaii.PushId($"selector_{_id}"))
        {

            if(Flags.HasFlag(SelectorFlags.AllowSearch))
            {
                ImGui.SetNextItemWidth(-1);

                if(_shouldFocusSearch)
                    ImGui.SetKeyboardFocusHere();

                if(ImGui.InputTextWithHint($"###search", "Search", ref _search, 256))
                {
                    UpdateList();
                }
            }
            _shouldFocusSearch = false;

            if(Flags.HasFlag(SelectorFlags.ShowOptions))
            {
                using(ImRaii.PushId("options_container"))
                {
                    DrawOptions();
                }
            }

            var listSize = MinimumListSize;

            if(Flags.HasFlag(SelectorFlags.AdaptiveSizing))
            {
                var maxSize = ImGui.GetContentRegionAvail();

                if(listSize.X < maxSize.X)
                {
                    listSize.X = maxSize.X;
                    listSize.Y = MinimumListSize.Y * (1.0f + (listSize.X / MinimumListSize.X));
                }
            }

            using(var listbox = ImRaii.ListBox($"###listbox", listSize))
            {
                if(items == null)
                    return;

                if(listbox.Success)
                {
                    int i = 0;

                    foreach(var item in items)
                    {
                        ++i;

                        using(ImRaii.PushId(i))
                        {
                            var startPos = ImGui.GetCursorPos();
                            bool isSoftSelected = IsItemSoftSelected(item);
                            bool wasSoftSelected = ImGui.Selectable($"###entry", isSoftSelected, ImGuiSelectableFlags.AllowDoubleClick, new Vector2(0, EntrySize));
                            bool wasSelected = wasSoftSelected && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
                            var endPos = ImGui.GetCursorPos();



                            if(ImGui.IsItemVisible())
                            {
                                ImGui.SetCursorPos(startPos);
                                using(ImRaii.PushId("item_container"))
                                {
                                    using(var itemGroup = ImRaii.Group())
                                    {
                                        if(itemGroup.Success)
                                            DrawItem(item, isSoftSelected);
                                    }
                                    if(ImGui.IsItemHovered())
                                        DrawTooltip(item);
                                }
                                ImGui.SetCursorPos(endPos);
                            }

                            if(isSoftSelected && _scrollToSelected)
                            {
                                if(ImGui.IsItemVisible())
                                {
                                    _scrollToSelected = false;
                                }
                                else
                                {
                                    ImGui.SetScrollHereY();
                                }
                            }

                            if(wasSoftSelected)
                            {
                                _softSelected = item;
                                SoftSelectionChanged = true;

                                if(wasSelected)
                                {
                                    _selected = item;
                                    SelectionChanged = true;
                                }
                            }
                        }
                    }
                    //_scrollToSelected = false;

                }
            }
        }
    }

    protected void AddItem(T item)
    {
        _items.Add(item);
    }

    protected void AddItems(IEnumerable<T> items)
    {
        _items.AddRange(items);
    }

    protected abstract void DrawItem(T item, bool isSoftSelected);

    protected virtual void DrawOptions()
    {

    }

    protected virtual void DrawTooltip(T item)
    {

    }

    protected virtual void PopulateList()
    {

    }

    private void InitList()
    {

        _taskQueue = _taskQueue.ContinueWith(_ =>
        {
            PopulateList();
        }, TaskScheduler.Default);

        UpdateList();
    }

    protected void UpdateList(bool shouldClear = false)
    {
        if(shouldClear)
        {
            Interlocked.Exchange(ref _filteredAndSortedItems, null);
        }

        _taskQueue = _taskQueue.ContinueWith(_ =>
        {
            var newList = _items.Where(x =>
            {
                // Selected is always shown
                if(IsItemSelected(x))
                    return true;

                return Filter(x, _search);
            }).ToList();
            newList.Sort(Compare);

            Interlocked.Exchange(ref _filteredAndSortedItems, newList);

        }, TaskScheduler.Default);

    }

    protected virtual bool Filter(T item, string search)
    {
        return true;
    }

    protected virtual int Compare(T itemA, T itemB)
    {
        return 0;
    }

    protected virtual bool IsItemSoftSelected(T item)
    {
        return _softSelected?.Equals(item) ?? false;
    }

    protected virtual bool IsItemSelected(T item)
    {
        return _selected?.Equals(item) ?? false;
    }
}

[Flags]
public enum SelectorFlags
{
    None = 0,
    AllowSearch = 1 << 0,
    ShowOptions = 1 << 1,
    AdaptiveSizing = 1 << 2,
}
