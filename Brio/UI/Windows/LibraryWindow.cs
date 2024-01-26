using Brio.Config;
using Brio.Files;
using Brio.Game.Types;
using Brio.Library;
using Brio.Library.Filters;
using Brio.Library.Tags;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Windows;

internal class LibraryWindow : Window
{
    private const float InfoPaneWidth = 300;
    private const float SearchWidth = 350;
    private const int MaxTagsInSuggest = 25;
    private const float PathBarButtonWidth = 25;

    private readonly ConfigurationService _configurationService;
    private readonly LibraryManager _libraryManager;
    private readonly IPluginLog _log;

    private readonly static List<FilterBase> filters = new()
    {
        new LibraryFavoritesFilter(),
        new TypeFilter("Characters", typeof(AnamnesisCharaFile), typeof(ActorAppearanceUnion), typeof(MareCharacterDataFile)),
        new TypeFilter("Poses", typeof(PoseFile), typeof(CMToolPoseFile)),
    };

    private FilterBase _typeFilter = filters[0];
    private SearchQueryFilter _searchFilter = new();
    private TagFilter _tagFilter = new();
    private string _searchText = string.Empty;

    private readonly List<GroupEntryBase> _path = new();
    private IEnumerable<EntryBase>? _currentEntries;
    private TagCollection _allTags = new();
    private EntryBase? _toOpen = null;
    private EntryBase? _selected = null;
    private float spinnerAngle = 0;
    private bool _searchNeedsFocus = false;
    private int _searchLostFocus = 0;
    private bool _isSearchSuggestWindowOpen = false;
    private bool _isSearchFocused = false;
    private bool _searchTextNeedsClear = false;
    private Vector2? _searchSuggestPos;
    private Vector2? _searchSuggestSize;

    public LibraryWindow(
        IPluginLog log,
        ConfigurationService configurationService,
        LibraryManager libraryManager)
        : base($"{Brio.Name} Library###brio_library_window")
    {
        this.Namespace = "brio_library_namespace";

        WindowSizeConstraints constraints = new();
        constraints.MinimumSize = new(850, 500);
        constraints.MaximumSize = ImGui.GetIO().DisplaySize;
        this.SizeConstraints = constraints;

        _log = log;
        _configurationService = configurationService;
        _libraryManager = libraryManager;

        IsOpen = true;
        _path.Add(_libraryManager.Root);
    }

    private float WindowContentWidth => ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
    private float WindowContentHeight => ImGui.GetWindowContentRegionMax().Y - ImGui.GetWindowContentRegionMin().Y;
    public bool IsSearching => (_searchFilter.Query != null && _searchFilter.Query.Length > 0) || (_tagFilter.Tags != null && _tagFilter.Tags.Count > 0);

    public override void OnOpen()
    {
        base.OnOpen();
        Refresh(true);
    }

    public override void Draw()
    {
        using(ImRaii.PushId("brio_library"))
        {
            DrawFilters();

            if(_typeFilter == null)
                return;

            ImGui.Spacing();
            DrawPath(WindowContentWidth - SearchWidth);
            ImGui.SameLine();
            DrawSearch();


            var windowSize = ImGui.GetWindowSize();
            using(var child = ImRaii.Child("###left_pane", new Vector2(windowSize.X - InfoPaneWidth, -1), true))
            {
                if(child.Success)
                {
                    DrawFiles();
                }
            }
            ImGui.SameLine();

            if(ImGui.BeginChild("###right_pane", new Vector2(ImBrio.GetRemainingWidth(), -1), false,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                float height = ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight() - ImGui.GetStyle().ItemSpacing.Y;
                if (ImGui.BeginChild("###library_info_pane", new Vector2(ImBrio.GetRemainingWidth(), height), true))
                {
                    if(_selected != null && _selected is ItemEntryBase file)
                    {
                        DrawInfo(file);
                    }

                    ImGui.EndChild();
                }

                DrawActions();

                ImGui.EndChild();
            }

            DrawSearchSuggest();
        }
    }

    private void DrawFilters()
    {
        if(filters.Count <= 1)
            return;

        List<string> ops = new();
        int selected = 0;
        for (int i = 0; i < filters.Count; i++)
        {
            if(filters[i] == _typeFilter)
                selected = i;

            ops.Add(filters[i].Name);
        }

        if (ImBrio.ToggleButtonStrip("library_filters_selector", new(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ops.ToArray()))
        {
            _typeFilter = filters[selected];
            Refresh(true);
        }
    }

    private void ClearFilters()
    {
        ClearSearchText();
        _searchFilter.Clear();
        _tagFilter.Clear();

        // cant clear type filter.
        ////_typeFilter.Clear();

        Refresh(true);
    }

    private void DrawPath(float width = -1)
    {
        float lineHeight = ImBrio.GetLineHeight();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);

        if(width == -1)
            width = ImBrio.GetRemainingWidth();

        if (ImGui.BeginChild("library_path_input", new(width, lineHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0);

            // Go Up Button
            {
                if(_path.Count <= 1)
                    ImGui.BeginDisabled();

                if(ImBrio.FontIconButton(FontAwesomeIcon.CaretUp, new(PathBarButtonWidth, lineHeight)))
                {
                    _path.RemoveAt(_path.Count - 1);
                    ClearFilters();
                }

                if(_path.Count <= 1)
                {
                    ImGui.EndDisabled();
                }
            }

            ImGui.SameLine();

            // Path segments
            for(int i = 0; i < _path.Count; i++)
            {
                if(i > 0)
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
                    ImBrio.FontIcon(FontAwesomeIcon.CaretRight, 0.5f);
                    ImGui.SameLine();
                }

                if(ImGui.Button(_path[i].Name))
                {
                    if((i + 1) < _path.Count)
                    {
                        _path.RemoveRange((i + 1), _path.Count - (i + 1));
                        ClearFilters();
                        break;
                    }
                }

                ImGui.SameLine();
            }

            ImGui.SameLine();

            // Blank area
            {
                float blankWidth = ImBrio.GetRemainingWidth() - PathBarButtonWidth - ImGui.GetStyle().ItemSpacing.X;
                if(ImGui.InvisibleButton("###library_path_input_blank", new(blankWidth, lineHeight)))
                {
                    // consider: clicking here swaps to an InputText for pasting paths?
                }
            }

            ImGui.SameLine();

            // Refresh Button
            {
                if(_libraryManager.IsScanning)
                    ImGui.BeginDisabled();

                if(ImBrio.FontIconButton(FontAwesomeIcon.Repeat, new(PathBarButtonWidth, lineHeight)))
                {
                    _libraryManager.Scan();
                }

                if(_libraryManager.IsScanning)
                {
                    ImGui.EndDisabled();
                }
            }

            ImGui.PopStyleColor();


            
            ImGui.SameLine();
            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    private unsafe void DrawSearch()
    {
        float searchBarWidth = ImBrio.GetRemainingWidth();
        float searchBarHeight = ImBrio.GetLineHeight();
        Vector2 searchbarPosition = ImGui.GetCursorScreenPos();
        
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);

        using(var child = ImRaii.Child("library_search_input", new(searchBarWidth, searchBarHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if(child.Success)
            {
                // search / clear icons
                if(IsSearching)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, 0x000000);
                    if(ImBrio.FontIconButton(FontAwesomeIcon.TimesCircle))
                    {
                        ClearSearchText();
                        _searchFilter.Clear();
                        _tagFilter.Clear();
                        _searchNeedsFocus = true;
                        Refresh(true);
                    }

                    ImGui.PopStyleColor();
                }
                else
                {

                    ImGui.BeginDisabled();
                    ImGui.PushStyleColor(ImGuiCol.Button, 0x000000);
                    ImBrio.FontIconButton(FontAwesomeIcon.Search);
                    ImGui.PopStyleColor();
                    ImGui.EndDisabled();
                }

                // Tags
                if(_tagFilter.Tags != null)
                {
                    Tag? toRemove = null;
                    foreach(Tag tag in _tagFilter.Tags)
                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosY(0);

                        if(ImBrio.DrawTag(tag))
                        {
                            toRemove = tag;
                        }
                    }

                    if(toRemove != null)
                    {
                        _tagFilter.Tags.Remove(toRemove);
                        _searchNeedsFocus = true;
                        Refresh(true);
                    }
                }

                // String input
                ImGui.SameLine();
                ImGui.SetCursorPosY(0);
                ImGui.SetNextItemWidth(ImBrio.GetRemainingWidth());

                if(_searchNeedsFocus)
                {
                    ImGui.SetKeyboardFocusHere();
                    _searchNeedsFocus = false;
                }

                ImGui.PushStyleColor(ImGuiCol.FrameBg, 0x000000);
                if(ImGui.InputText("###library_search_input", ref _searchText, 256,
                    ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.NoUndoRedo
                    | ImGuiInputTextFlags.CallbackAlways,
                    OnSearchFunc))
                {
                    if(string.IsNullOrEmpty(_searchText))
                    {
                        _searchFilter.Query = null;
                    }
                    else
                    {
                        _searchFilter.Query = SearchUtility.ToQuery(_searchText);
                    }

                    Refresh(true);
                }

                ImGui.PopStyleColor();

                _isSearchFocused = ImGui.IsItemActive();

                // TODO: Try to capture backspace keys to remove tags. possibly with the new key bind input system?
                // ImGui.IsKeyPressed(ImGuiKey.Backspace) doesn't work, as expected.

                if(!_isSearchFocused)
                {
                    _searchLostFocus++;

                    if(_searchLostFocus > 10)
                    {
                        _searchText = _searchFilter.GetSearchString();
                    }
                }
                else
                {
                    _searchLostFocus = 0;
                }

                _searchSuggestPos = new Vector2(searchbarPosition.X, searchbarPosition.Y + searchBarHeight);
                _searchSuggestSize = new Vector2(searchBarWidth, 0);
            }
        }

        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
    }

    private void ClearSearchText()
    {
        _searchText = string.Empty;
        _searchTextNeedsClear = true;
    }

    private unsafe int OnSearchFunc(ImGuiInputTextCallbackData* data)
    {
        if(_searchTextNeedsClear)
        {
            _searchTextNeedsClear = false;
            _searchText = string.Empty;

            // clear the search input buffer
            data->BufTextLen = 0;
            data->BufSize = 0;
            data->CursorPos = 0;
            data->SelectionStart = 0;
            data->SelectionEnd = 0;
            data->BufDirty = 1;
        }

        return 1;
    }

    private void DrawSearchSuggest()
    {
        if(_searchSuggestPos == null || _searchSuggestSize == null)
            return;

        if(_isSearchFocused)
            _isSearchSuggestWindowOpen = true;

        if(!_isSearchSuggestWindowOpen && !_isSearchFocused)
            return;

        ImGui.SetNextWindowPos((Vector2)_searchSuggestPos);
        ImGui.SetNextWindowSize((Vector2)_searchSuggestSize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7, 7));

        if(ImGui.Begin(
            "##library_search_suggest",
            ref _isSearchSuggestWindowOpen,
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Tooltip
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.ChildWindow))
        {
            bool hasContent = false;
            List<Tag> availableTags = GetAvailableTags(SearchUtility.ToQuery(_searchText));

            int trimmedTags = 0;
            if (availableTags.Count > MaxTagsInSuggest)
            {
                trimmedTags = availableTags.Count - MaxTagsInSuggest;
                availableTags = availableTags.GetRange(0, MaxTagsInSuggest);
            }

            // click tags
            if(availableTags.Count > 0)
            {
                Tag? selected = ImBrio.DrawTags(availableTags);
                if(selected != null)
                {
                    _tagFilter.Add(selected);
                    _searchNeedsFocus = true;

                    ClearSearchText();
                    Refresh(true);
                }

                if (trimmedTags > 0)
                {
                    ImBrio.Text($"plus \"{trimmedTags}\" more tags...", 0.75f, 0x88FFFFFF);
                }

                hasContent = true;
            }

            // search string
            if(!string.IsNullOrEmpty(_searchText))
            {
                ImBrio.Text($"Press ENTER to search for \"{_searchText}\"", 0x88FFFFFF);
                hasContent = true;
            }

            // quick tag
            if(availableTags.Count >= 1)
            {
                ImBrio.Text($"Press TAB to filter by tag \"{availableTags[0].DisplayName}\"", 0x88FFFFFF);
                hasContent = true;

                if(ImGui.IsKeyPressed(ImGuiKey.Tab))
                {
                    _tagFilter.Add(availableTags[0]);
                    ClearSearchText();
                    Refresh(true);
                }
            }

            if (!hasContent)
            {
                ImBrio.Text($"Start typing to search...", 0x88FFFFFF);
            }
        }

        ImGui.End();
        ImGui.PopStyleVar();

        if (_searchLostFocus > 10 && !_searchNeedsFocus)
        {
            _isSearchSuggestWindowOpen = false;
        }
    }

    private List<Tag> GetAvailableTags(string[] query)
    {
        List<Tag> results = new();
        foreach(var tag in _allTags)
        {
            if(_tagFilter.Tags?.Contains(tag) == true)
                continue;

            if(query == null || tag.Search(query))
            {
                results.Add(tag);
            }
        }

        return results;
    }

    private void DrawFiles()
    {
        float fileWidth = 120;

        int columnCount = (int)Math.Floor(WindowContentWidth / 120.0f);
        int column = 0;
        int index = 0;

        if(_libraryManager.IsScanning)
        {
            ImGui.SetCursorPosX((WindowContentWidth / 2) - 24);
            ImGui.SetCursorPosY((WindowContentHeight / 2) - 24);
            ImBrio.Spinner(ref spinnerAngle);
        }
        else
        {
            if(_currentEntries == null)
            {
                Refresh(true);
            }
            else
            {
                foreach(var entry in _currentEntries)
                {
                    DrawEntry(entry, fileWidth, index);
                    index++;

                    column++;
                    if(column >= columnCount)
                    {
                        column = 0;
                    }
                    else
                    {
                        ImGui.SameLine();
                    }
                }
            }
        }
        

        if(_toOpen != null)
        {
            OnOpen(_toOpen);
            _toOpen = null;
        }
    }

    private void DrawEntry(EntryBase entry, float width, int id)
    {
        float height = width + 60;
        Vector2 size = new(width, height);
        Vector2 pos = ImGui.GetCursorPos();

        bool selected = _selected == entry;
        if (ImGui.Selectable($"###library_entry_{id}_selectable", ref selected, ImGuiSelectableFlags.AllowDoubleClick, size))
        {
            _selected = entry;

            if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _toOpen = entry;
            }
        }

        if(ImGui.IsItemVisible())
        {
            ImGui.SetCursorPos(pos);

            using(var child = ImRaii.Child($"library_entry_{id}", size, true, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs))
            {
                if(!child.Success)
                    return;

                if(entry.Icon != null)
                {
                    ImBrio.ImageFit(entry.Icon, ImGui.GetContentRegionAvail());
                }

                ImBrio.TextCentered(entry.Name, ImGui.GetContentRegionAvail().X);
            }
        }
    }

    private void DrawInfo(ItemEntryBase entry)
    {
        ImGui.Text(entry.Name);

        if(entry.Source != null)
        {
            float x = ImGui.GetCursorPosX();
            float y = ImGui.GetCursorPosY();
            float sourceIconBottom = y;
            if(entry.Source.Icon != null)
            {
                ImGui.SetCursorPosY(y + 3);
                ImBrio.ImageFit(entry.Source.Icon, new(42, 42));
                sourceIconBottom = ImGui.GetCursorPosY();

                ImGui.SameLine();
                x = ImGui.GetCursorPosX();
            }

            ImGui.Text(entry.Source.Name);

            if(entry.SourceInfo != null)
            {
                ImGui.SetCursorPosY(y + 18);
                ImGui.SetCursorPosX(x);
                ImGui.SetWindowFontScale(0.7f);
                ImGui.BeginDisabled();
                ImGui.TextWrapped(entry.SourceInfo);
                ImGui.EndDisabled();
                ImGui.SetWindowFontScale(1.0f);
            }

            if (ImGui.GetCursorPosY() < sourceIconBottom)
                ImGui.SetCursorPosY(sourceIconBottom);
        }


        if(entry.PreviewImage != null)
        {
            Vector2 size = ImGui.GetContentRegionAvail();
            size.Y = size.X;
            using(var child = ImRaii.Child($"library_info_image", size, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs))
            {
                if(!child.Success)
                    return;

                ImBrio.ImageFit(entry.PreviewImage);
            }
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        if(!string.IsNullOrEmpty(entry.Description))
            ImGui.TextWrapped(entry.Description);

        ImGui.Spacing();
        ImGui.Spacing();

        if (!string.IsNullOrEmpty(entry.Author))
            ImGui.Text($"Author: {entry.Author}");

        if(!string.IsNullOrEmpty(entry.Version))
            ImGui.Text($"Version: {entry.Version}");

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Text("Tags:");
        ImGui.SameLine();
        ImBrio.DrawTags(entry.Tags);
    }

    private void DrawActions()
    {
        float lineHeight = ImBrio.GetLineHeight();
        float browseButtonWidth = lineHeight;
        float openButtonWidth = 100;

        if (ImGui.Button("...", new(browseButtonWidth, lineHeight)))
        {
            // TODO!
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Open file picker");

        ImGui.SameLine();

        float space = ImBrio.GetRemainingWidth() - openButtonWidth;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() +  space);
        ImGui.Button("Open", new(openButtonWidth, lineHeight));
    }

    private void OnOpen(EntryBase entry)
    {
        if (entry is GroupEntryBase dir)
        {
            _path.Add(dir);
            Refresh(false);
        }
        else
        {
            // open the file?
        }
    }

    private void Refresh(bool filter)
    {
        if(_libraryManager.IsScanning)
            return;

        _selected = null;

        if (_currentEntries != null)
        {
            foreach(EntryBase entry in _currentEntries)
            {
                entry.IsVisible = false;
            }
        }

        if(filter)
        {
            List<FilterBase> filters = new();
            filters.Add(_typeFilter);

            if(!string.IsNullOrEmpty(_searchText))
                filters.Add(_searchFilter);

            if(_tagFilter.Tags != null && _tagFilter.Tags.Count > 0)
                filters.Add(_tagFilter);

            _libraryManager.Root.FilterEntries(filters.ToArray());
        }

        GroupEntryBase currentEntry = _path[_path.Count - 1];
        _currentEntries = currentEntry.GetFilteredEntries(IsSearching);

        _allTags.Clear();
        currentEntry.GetAllTags(ref _allTags);

        if(_currentEntries != null)
        {
            foreach(EntryBase entry in _currentEntries)
            {
                entry.IsVisible = true;
            }
        }
    }
}
