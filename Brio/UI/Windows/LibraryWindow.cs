using Brio.Config;
using Brio.Files;
using Brio.Game.GPose;
using Brio.Game.Types;
using Brio.Library;
using Brio.Library.Filters;
using Brio.Library.Tags;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Brio.UI.Windows;

internal class LibraryWindow : Window
{
    private static Vector2 MinimumSize = new(850, 500);

    private const float InfoPaneWidth = 350;
    private const float SearchWidth = 400;
    private const int MaxTagsInSuggest = 25;
    private const float PathBarButtonWidth = 25;
    private const float FooterScaleSliderWidth = 100;
    private const int MinEntrySize = 100;
    private const int MaxEntrySize = 250;

    private readonly SettingsWindow _settingsWindow;

    public readonly TagFilter TagFilter = new();

    private readonly ConfigurationService _configurationService;
    private readonly LibraryManager _libraryManager;
    private readonly IPluginLog _log;
    private readonly IServiceProvider _serviceProvider;
    private readonly GPoseService _gPoseService;

    private readonly LibraryFavoritesFilter _favoritesFilter;
    private readonly TypeFilter _charactersFilter;
    private readonly TypeFilter _posesFilter;
    private readonly SearchQueryFilter _searchFilter = new();
    private readonly List<GroupEntryBase> _path = new();
    private FilterBase? _modalFilter;
    private FilterBase _selectedFilter;
    FilterBase? _lastFilter;
    private string _searchText = string.Empty;
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
    private bool _isRescanning;
    private bool _isRefreshing;
    private long _lastRefreshTimeMs = 0;
    private bool _isModal = false;
    private Action<object>? _modalCallback;

    public LibraryWindow(
        IPluginLog log,
        GPoseService gPoseService,
        ConfigurationService configurationService,
        LibraryManager libraryManager,
        SettingsWindow settingsWindow,
        IServiceProvider serviceProvider)
        : base($"{Brio.Name} Library###brio_library_window")
    {
        this.Namespace = "brio_library_namespace";

        WindowSizeConstraints constraints = new();
        constraints.MinimumSize = MinimumSize;
        constraints.MaximumSize = ImGui.GetIO().DisplaySize;
        this.SizeConstraints = constraints;

        _log = log;
        _configurationService = configurationService;
        _libraryManager = libraryManager;
        _serviceProvider = serviceProvider;
        _gPoseService = gPoseService;

        _settingsWindow = settingsWindow;

        _path.Add(_libraryManager.Root);

        _libraryManager.RegisterWindow(this);
        _libraryManager.OnScanFinished += OnLibraryScanFinished;
        configurationService.OnConfigurationChanged += OnConfigurationChanged;

        _gPoseService.OnGPoseStateChange += _gPoseService_OnGPoseStateChange;

        _favoritesFilter = new LibraryFavoritesFilter(configurationService);
        _charactersFilter = new TypeFilter("Characters", typeof(AnamnesisCharaFile), typeof(ActorAppearanceUnion), typeof(MareCharacterDataFile));
        _posesFilter = new TypeFilter("Poses", typeof(PoseFile), typeof(CMToolPoseFile));
        _selectedFilter = _favoritesFilter;
    }

    private void _gPoseService_OnGPoseStateChange(bool newState)
    {
        if(newState)
        {

        }
        else
        {
            IsOpen = false;
        }
    }

    private float WindowContentWidth => ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
    private float WindowContentHeight => ImGui.GetWindowContentRegionMax().Y - ImGui.GetWindowContentRegionMin().Y;
    public bool IsSearching => (_searchFilter.Query != null && _searchFilter.Query.Length > 0) || (TagFilter.Tags != null && TagFilter.Tags.Count > 0);
    public bool IsModal => _isModal;

    public void OpenModal(FilterBase filter, Action<object> callback)
    {
        _isModal = true;
        _modalCallback = callback;
        _modalFilter = filter;

        _selectedFilter = _modalFilter;

        Flags = ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse;

        if(_configurationService.Configuration.Library.ReturnLibraryToLastLocation)
        {
            TryRefresh(true);
        }
        else
        {
            Reset();
        }

        IsOpen = true;
    }

    public void Open()
    {
        _isModal = false;
        _modalCallback = null;
        _modalFilter = null;

        Flags = ImGuiWindowFlags.None;

        if(_configurationService.Configuration.Library.ReturnLibraryToLastLocation)
        {
            if(_lastFilter is not null)
            {
                _selectedFilter = _lastFilter;
            }

            TryRefresh(true);
        }
        else
        {
            _selectedFilter = _favoritesFilter;

            Reset();
        }

        IsOpen = true;
    }

    public void Close()
    {
        if(_isModal == false)
        {
            _lastFilter = _selectedFilter;
        }

        IsOpen = false;
        _isModal = false;
    }

    public new void Toggle()
    {
        if(IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Reset()
    {
        if(_path.Count > 1)
            _path.RemoveRange(1, _path.Count - 1);

        TryRefresh(true);
    }

    private void OnLibraryScanFinished()
    {
        TryRefresh(true);
    }

    private void OnConfigurationChanged()
    {
        if(_gPoseService.IsGPosing)
        {
            TryRefresh(true);
        }
    }

    public override void Draw()
    {
        DrawInternal();
    }

    public override bool DrawConditions()
    {
        if(_isModal)
            return false;

        return base.DrawConditions();
    }

    public void DrawModal()
    {
        if(!this.IsOpen || !this._isModal || _modalFilter == null)
            return;

        ImGui.OpenPopup($"Import {_modalFilter.Name}##brio_library_popup");

        ImGui.SetNextWindowSizeConstraints(MinimumSize, ImGui.GetIO().DisplaySize);

        ImGui.BeginPopupModal($"Import {_modalFilter.Name}##brio_library_popup");

        DrawInternal();
        ImGui.EndPopup();
    }

    private void DrawInternal()
    {
        using(ImRaii.PushId("brio_library"))
        {

            DrawFilters();

            if(_selectedFilter == null)
                return;


            float pathWidth = WindowContentWidth - SearchWidth;
            if(_isModal)
                pathWidth -= 155;

            ImGui.Spacing();
            DrawPath(pathWidth);

            if(_isModal)
            {
                ImGui.SameLine();

                if(ImBrio.Button("Browse for File", FontAwesomeIcon.FolderOpen, new Vector2(155, 0), 0.9f))
                    DoBrowse();

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Browse for a file");
            }

            ImGui.SameLine();
            DrawSearch();

            var windowSize = ImGui.GetWindowSize();
            if(ImGui.BeginChild("###left_pane", new Vector2(windowSize.X - InfoPaneWidth, -1), false,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                float entriesPaneHeight = ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight() - ImGui.GetStyle().ItemSpacing.Y;
                float entriesPaneWidth = ImBrio.GetRemainingWidth();
                if(ImGui.BeginChild("###library_entries_pane", new Vector2(entriesPaneWidth, entriesPaneHeight), true))
                {
                    DrawFiles();
                    ImGui.EndChild();
                }


                Vector2 mousePos = ImGui.GetMousePos() - ImGui.GetWindowPos();
                bool isMouseOverArea = (mousePos.X > 0 && mousePos.Y > 0 && mousePos.X < entriesPaneWidth && mousePos.Y < entriesPaneHeight);
                if(isMouseOverArea)
                {
                    // Check if the user has clicked on the background to clear selection.
                    if(ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsAnyItemHovered())
                    {
                        _selected = null;
                    }

                    // Ctrl+wheel to change icon size
                    float mouseWheel = ImGui.GetIO().MouseWheel * 10;
                    // TODO: replace this ctrl listener with the new key bind system when it is merged
                    // as ImGUI ctrl support is _spotty_
                    if(ImGui.IsKeyPressed(ImGuiKey.LeftCtrl) && mouseWheel != 0)
                    {
                        float val = _configurationService.Configuration.Library.IconSize;
                        val = Math.Clamp(val + mouseWheel, MinEntrySize, MaxEntrySize);
                        _configurationService.Configuration.Library.IconSize = val;
                        _configurationService.Save();
                    }
                }

                DrawFooter();

                ImGui.EndChild();
            }
            ImGui.SameLine();

            if(ImGui.BeginChild("###right_pane", new Vector2(ImBrio.GetRemainingWidth(), -1), false,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                float paneHeight = ImBrio.GetRemainingHeight() - (ImBrio.GetLineHeight() + ImGui.GetStyle().ItemSpacing.Y);

                if(ImGui.BeginChild("###library_info_pane", new Vector2(ImBrio.GetRemainingWidth(), paneHeight), true,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    if(_selected != null)
                    {
                        DrawInfo(_selected);
                    }
                    else
                    {
                        DrawInfo(_path[_path.Count - 1]);
                    }

                    ImGui.EndChild();
                }

                // Actions
                if(_isModal)
                {
                    bool isIEB = false;

                    if(_selected is ItemEntryBase ieb)
                    {
                        isIEB = true;

                        if(ieb is not null)
                        {
                            var config = ConfigurationService.Instance.Configuration;
                            bool isFavorite = config.Library.Favorites.Contains(ieb.Identifier);

                            ImGui.PushStyleColor(ImGuiCol.Text, isFavorite ? UIConstants.GizmoRed : UIConstants.ToggleButtonInactive);

                            if(ImBrio.FontIconButton(FontAwesomeIcon.Heart))
                            {
                                if(!isFavorite)
                                {
                                    config.Library.Favorites.Add(ieb.Identifier);
                                }
                                else
                                {
                                    config.Library.Favorites.Remove(ieb.Identifier);
                                }

                                ConfigurationService.Instance.Save();
                            }

                            ImGui.PopStyleColor();

                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip(isFavorite ? "Remove from favorites" : "Add to favorites");

                            ImGui.SameLine();
                        }
                    }

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImBrio.GetRemainingWidth() - (200 + ImGui.GetStyle().ItemSpacing.X)));

                    if(isIEB == false)
                    {
                        ImGui.BeginDisabled();
                    }

                    if(ImBrio.Button("Select", FontAwesomeIcon.Check, new Vector2(100, 0)))
                    {
                        if(_selected != null)
                        {
                            OnOpen(_selected);
                        }
                    }

                    if(isIEB == false)
                    {
                        ImGui.EndDisabled();
                    }

                    ImGui.SameLine();

                    if(ImBrio.Button("Cancel", FontAwesomeIcon.Times, new Vector2(100, 0)))
                    {
                        Close();
                    }
                }
                else
                {
                    if(_selected != null)
                    {
                        _selected.DrawActions(_isModal);
                    }
                    else
                    {
                        _path[_path.Count - 1].DrawActions(_isModal);
                    }
                }
            }

            DrawSearchSuggest();
        }
    }

    private void DoBrowse()
    {
        if(_modalFilter != null && _modalCallback != null)
            _libraryManager.ShowFilePicker(_modalFilter, _modalCallback);

        Close();
    }

    private void DrawFilters()
    {
        if(_isModal && _modalFilter != null)
        {
            List<string> ops = new();
            int selected = 0;

            ops.Add(_favoritesFilter.Name);
            if(_favoritesFilter == _selectedFilter)
                selected = 0;

            ops.Add(_modalFilter.Name);
            if(_modalFilter == _selectedFilter)
                selected = 1;

            if(ImBrio.ToggleButtonStrip("library_filters_selector", new(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ops.ToArray()))
            {
                if(selected == 0)
                {
                    _selectedFilter = _favoritesFilter;
                }
                else if(selected == 1)
                {
                    _selectedFilter = _modalFilter;
                }

                if(_path.Count > 1)
                    _path.RemoveRange(1, _path.Count - 1);

                TryRefresh(true);
            }
        }
        else
        {
            List<string> ops = new();
            int selected = 0;

            ops.Add(_favoritesFilter.Name);
            if(_favoritesFilter == _selectedFilter)
                selected = 0;

            ops.Add(_charactersFilter.Name);
            if(_charactersFilter == _selectedFilter)
                selected = 1;

            ops.Add(_posesFilter.Name);
            if(_posesFilter == _selectedFilter)
                selected = 2;


            if(ImBrio.ToggleButtonStrip("library_filters_selector", new(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, ops.ToArray()))
            {
                if(selected == 0)
                {
                    _selectedFilter = _favoritesFilter;
                }
                else if(selected == 1)
                {
                    _selectedFilter = _charactersFilter;
                }
                else if(selected == 2)
                {
                    _selectedFilter = _posesFilter;
                }

                if(_path.Count > 1)
                    _path.RemoveRange(1, _path.Count - 1);

                TryRefresh(true);
            }
        }
    }

    private void ClearFilters()
    {
        ClearSearchText();
        _searchFilter.Clear();
        TagFilter.Clear();

        // cant clear type filter.
        ////_typeFilter.Clear();

        TryRefresh(true);
    }

    private void DrawPath(float width = -1)
    {
        float lineHeight = ImBrio.GetLineHeight();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);

        if(width == -1)
            width = ImBrio.GetRemainingWidth();

        if(ImGui.BeginChild("library_path_input", new(width, lineHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
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
                if(_isRescanning)
                    ImGui.BeginDisabled();

                if(ImBrio.FontIconButton(FontAwesomeIcon.Repeat, new(PathBarButtonWidth, lineHeight)))
                {
                    ReScan();
                }

                if(ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Scan all library sources and refresh the view");
                }

                if(_isRescanning)
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
                        TagFilter.Clear();
                        _searchNeedsFocus = true;
                        TryRefresh(true);
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
                if(TagFilter.Tags != null)
                {
                    Tag? toRemove = null;
                    foreach(Tag tag in TagFilter.Tags)
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
                        TagFilter.Tags.Remove(toRemove);
                        _searchNeedsFocus = true;
                        TryRefresh(true);
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
                    ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.NoUndoRedo
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

                    TryRefresh(true);
                }

                ImGui.PopStyleColor();

                _isSearchFocused = ImGui.IsItemActive();

                // TODO: Try to capture backspace keys to remove tags. possibly with the new key bind input system?
                // ImGui.IsKeyPressed(ImGuiKey.Backspace) doesn't work, as expected.

                if(!_isSearchFocused)
                {
                    _searchLostFocus++;
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
            if(availableTags.Count > MaxTagsInSuggest)
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
                    TagFilter.Add(selected);
                    _searchNeedsFocus = true;

                    ClearSearchText();
                    TryRefresh(true);
                }

                if(trimmedTags > 0)
                {
                    ImBrio.Text($"plus \"{trimmedTags}\" more tags...", 0.75f, 0x88FFFFFF);
                }

                hasContent = true;
            }

            // quick tag
            if(availableTags.Count >= 1)
            {
                ImBrio.Text($"Press TAB to filter by tag \"{availableTags[0].DisplayName}\"", 0x88FFFFFF);
                hasContent = true;

                if(ImGui.IsKeyPressed(ImGuiKey.Tab))
                {
                    TagFilter.Add(availableTags[0]);
                    ClearSearchText();
                    TryRefresh(true);
                }
            }

            if(!hasContent)
            {
                ImBrio.Text($"Start typing to search...", 0x88FFFFFF);
            }
        }

        ImGui.End();
        ImGui.PopStyleVar();

        if(_searchLostFocus > 10 && !_searchNeedsFocus)
        {
            _isSearchSuggestWindowOpen = false;
        }
    }

    private List<Tag> GetAvailableTags(string[] query)
    {
        List<Tag> results = new();
        foreach(var tag in _allTags)
        {
            if(TagFilter.Tags?.Contains(tag) == true)
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
        float fileWidth = _configurationService.Configuration.Library.IconSize;

        int columnCount = (int)Math.Floor(WindowContentWidth / fileWidth);
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
                TryRefresh(true);
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
        if(ImGui.Selectable($"###library_entry_{id}_selectable", ref selected, ImGuiSelectableFlags.AllowDoubleClick, size))
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

            using(var child = ImRaii.Child($"library_entry_{id}", size, true,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
            {
                if(!child.Success)
                    return;

                Vector2 iconSize = new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().X);

                if(entry.Icon != null)
                {
                    ImBrio.ImageFit(entry.Icon, iconSize);
                }

                ImBrio.TextCentered(entry.Name, iconSize.Y);
            }
        }
    }

    private void DrawFooter()
    {
        if(ImBrio.Button("Add new source", FontAwesomeIcon.None, new Vector2(100, 0)))
        {
            if(_isModal)
            {
                Close();
            }

            _settingsWindow.OpenAsLibraryTab();
        }

        ImGui.SameLine();

        int size = (int)_configurationService.Configuration.Library.IconSize;
        ImGui.SetNextItemWidth(FooterScaleSliderWidth);
        if(ImGui.SliderInt("###library_scale_slider", ref size, MinEntrySize, MaxEntrySize, ""))
        {
            _configurationService.Configuration.Library.IconSize = size;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip($"Icon Size: {size}px");

        ImGui.SameLine();

        if(_isRescanning || _libraryManager.IsScanning)
        {
            ImGui.TextDisabled("Scanning...");
        }
        else
        {
            ImGui.TextDisabled($"found {_currentEntries?.Count().ToString("N0")} items in {_lastRefreshTimeMs}ms");
        }
    }


    private void DrawInfo(EntryBase entry)
    {
        ImGui.Text(entry.Name);

        // internal info
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0x000000);
        if(ImGui.BeginChild("###library_info_pane", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetRemainingHeight()), false))
        {
            entry.DrawInfo(this);
            ImGui.EndChild();
        }

        ImGui.PopStyleColor();
    }

    private void OnOpen(EntryBase entry)
    {
        if(entry is GroupEntryBase dir)
        {
            _path.Add(dir);
            TryRefresh(false);
        }
        else
        {
            if(_isModal && entry is ItemEntryBase itemEntry)
            {
                if(_modalCallback is not null)
                {
                    try
                    {
                        object? result = itemEntry.Load();

                        if(result is not null)
                        {
                            _modalCallback.Invoke(result);
                        }
                    }
                    catch(Exception ex)
                    {
                        Brio.Log.Error(ex, "Exception while invoking library modal Callback!");
                    }
                }

                Close();
            }
            else
            {
                // YUKI TODO ?
            }
        }
    }

    public async void ReScan()
    {
        _isRescanning = true;

        Stopwatch sw = new();
        sw.Start();

        await _libraryManager.ScanAsync();
        TryRefresh(true);

        sw.Stop();
        _lastRefreshTimeMs = sw.ElapsedMilliseconds;
        _isRescanning = false;
    }

    public void TryRefresh(bool filter)
    {
        try
        {
            Refresh(true);            
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Exception while Refreshing!");
        }
    }

    public void Refresh(bool filter)
    {
        // TODO: it is possible that the refresh function could take so long that new refresh requests
        // end up discarded, resulting in mixed results, with half the results being from the previous refresh
        // and half being with updated values.
        // to correct this, we _should_ include an abort flag so we can stop a refresh and trigger a new one
        // on each request here.
        if(_libraryManager.IsScanning || _isRefreshing)
            return;

        if(_libraryManager.IsScanning || _isRefreshing)
            return;

        _isRefreshing = true;

        Stopwatch sw = new();
        sw.Start();

        _selected = null;

        if(_currentEntries != null)
        {
            foreach(EntryBase entry in _currentEntries)
            {
                entry.IsVisible = false;
            }
        }

        GroupEntryBase currentEntry = _path[^1];

        if(filter)
        {
            List<FilterBase> filters = [_selectedFilter];

            if(_modalFilter != null)
                filters.Add(_modalFilter);

            if(TagFilter.Tags != null && TagFilter.Tags.Count > 0)
                filters.Add(TagFilter);

            _libraryManager.Root.FilterEntries(filters.ToArray());

            _allTags.Clear();
            currentEntry.GetAllTags(ref _allTags);

            // Add the search filter last, and re-filter the entries now that we have all the
            // tags
            if(!string.IsNullOrEmpty(_searchText))
            {
                filters.Add(_searchFilter);
                _libraryManager.Root.FilterEntries(filters.ToArray());
            }
        }

        bool flatten = IsSearching;

        if(_selectedFilter is LibraryFavoritesFilter)
            flatten = true;

        _currentEntries = currentEntry.GetFilteredEntries(flatten);
        if(_currentEntries != null)
        {
            foreach(EntryBase entry in _currentEntries)
            {
                entry.IsVisible = true;
            }
        }

        sw.Stop();
        _lastRefreshTimeMs = sw.ElapsedMilliseconds;
        _isRefreshing = false;
    }
}
