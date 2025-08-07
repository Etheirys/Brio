using Brio.Config;
using Brio.Entities;
using Brio.Files;
using Brio.Game.GPose;
using Brio.Game.Posing;
using Brio.Game.Types;
using Brio.Input;
using Brio.Library;
using Brio.Library.Filters;
using Brio.Library.Tags;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Windows;

public class LibraryWindow : Window
{
    private static float WindowContentWidth => ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
    private static float WindowContentHeight => ImGui.GetWindowContentRegionMax().Y - ImGui.GetWindowContentRegionMin().Y;

    private static Vector2 MinimumSize = new(785, 435);

    private const float InfoPaneWidth = 285;
    private const float SearchWidth = 400;
    private const int MaxTagsInSuggest = 25;
    private const float PathBarButtonWidth = 25;
    private const float FooterScaleSliderWidth = 100;
    private const int MinEntrySize = 80;
    private const int MaxEntrySize = 250;

    private readonly SettingsWindow _settingsWindow;

    public readonly TagFilter TagFilter = new();

    private readonly ConfigurationService _configurationService;
    private readonly LibraryManager _libraryManager;
    private readonly IPluginLog _log;
    private readonly IServiceProvider _serviceProvider;
    private readonly GPoseService _gPoseService;
    private readonly PosingService _posingService;
    private readonly IFramework _frameworkService;
    private readonly EntityManager _entityManager;

    private readonly SearchQueryFilter _searchFilter = new();
    private readonly LibraryFavoritesFilter _favoritesFilter;
    private readonly TypeFilter _charactersFilter;
    private readonly TypeFilter _posesFilter;

    private List<GroupEntryBase> _lastPathModalPose = [];
    private List<GroupEntryBase> _lastPathModalChar = [];
    private List<GroupEntryBase> _lastPath = [];

    private List<GroupEntryBase> _path = [];

    private FilterBase? _lastFilter;
    private FilterBase? _modalFilter;
    private FilterBase _selectedFilter;

    private string _searchText = string.Empty;

    private IEnumerable<EntryBase>? _currentEntries;

    private TagCollection _allTags = [];
    private EntryBase? _toOpen = null;
    private EntryBase? _selected = null;

    private float spinnerAngle = 0;
    private bool _searchNeedsFocus = false;
    private int _searchLostFocus = 0;
    private bool _isSearchSuggestWindowOpen = false;
    private bool _isSearchSuggestFocused = false;
    private bool _isSearchFocused = false;
    private bool _searchTextNeedsClear = false;

    private Vector2? _searchSuggestPos;
    private Vector2? _searchSuggestSize;

    private bool _isRescanning;
    private bool _isRefreshing;
    private long _lastRefreshTimeMs = 0;

    private bool _isModal = false;
    private bool _wasOpenBeforeModal = false;
    private Action<object>? _modalCallback;

    public LibraryWindow(
        IPluginLog log,
        GPoseService gPoseService,
        EntityManager entityManager,
        ConfigurationService configurationService,
        LibraryManager libraryManager,
        PosingService posingService,
        IFramework frameworkService,
        SettingsWindow settingsWindow,
        IServiceProvider serviceProvider)
        : base($"{Brio.Name} Library###brio_library_window")
    {
        this.Namespace = "brio_library_namespace";

        WindowSizeConstraints constraints = new()
        {
            MinimumSize = MinimumSize,
            MaximumSize = ImGui.GetIO().DisplaySize
        };
        this.SizeConstraints = constraints;

        _log = log;
        _configurationService = configurationService;
        _libraryManager = libraryManager;
        _serviceProvider = serviceProvider;
        _gPoseService = gPoseService;
        _frameworkService = frameworkService;
        _posingService = posingService;
        _settingsWindow = settingsWindow;
        _entityManager = entityManager;

        _path.Add(_libraryManager.Root);
        _lastPath.Add(_libraryManager.Root);
        _lastPathModalPose.Add(_libraryManager.Root);
        _lastPathModalChar.Add(_libraryManager.Root);

        _libraryManager.RegisterWindow(this);
        _libraryManager.OnScanFinished += OnLibraryScanFinished;
        _configurationService.OnConfigurationChanged += OnConfigurationChanged;
        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;

        _favoritesFilter = new LibraryFavoritesFilter(configurationService);
        _charactersFilter = new TypeFilter("Characters", typeof(AnamnesisCharaFile), typeof(ActorAppearanceUnion), typeof(MareCharacterDataFile));
        _posesFilter = new TypeFilter("Poses", typeof(PoseFile), typeof(CMToolPoseFile));
        _selectedFilter = _favoritesFilter;
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(newState == false)
            IsOpen = false;
    }

    public bool IsSearching => (_searchFilter.Query != null && _searchFilter.Query.Length > 0) || (TagFilter.Tags != null && TagFilter.Tags.Count > 0);
    public bool IsModal => _isModal;

    public void OpenModal(FilterBase filter, Action<object> callback)
    {
        if(IsOpen)
        {
            _wasOpenBeforeModal = true;

            Close();
        }

        _isModal = true;
        _modalCallback = callback;
        _modalFilter = filter;

        _selectedFilter = _modalFilter;

        Flags = ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse;

        if(_configurationService.Configuration.Library.ReturnLibraryToLastLocation)
        {
            if(_modalFilter?.Name == "Poses" && _lastPathModalPose is not null)
            {
                _path = _lastPathModalPose;
            }
            else if(_modalFilter?.Name == "Characters" && _lastPathModalChar is not null)
            {
                _path = _lastPathModalChar;
            }

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
        bool wasModal = _isModal;

        _isModal = false;
        _modalCallback = null;
        _modalFilter = null;

        Flags = ImGuiWindowFlags.None;

        if(_configurationService.Configuration.Library.ReturnLibraryToLastLocation)
        {
            if(wasModal && _lastPath is not null)
            {
                _path = _lastPath;
            }

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
            _lastPath = _path;
            _lastFilter = _selectedFilter;

        }
        else if(_modalFilter?.Name == "Poses")
        {
            _lastPathModalPose = _path;
        }
        else if(_modalFilter?.Name == "Characters")
        {
            _lastPathModalChar = _path;
        }

        IsOpen = false;

        if(_isModal && _wasOpenBeforeModal)
        {
            _frameworkService.RunOnTick(() =>
            {
                _wasOpenBeforeModal = false;
                Open();
            }, delay: TimeSpan.FromSeconds(0.1));
        }
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
        _path.Clear();
        _lastPath.Clear();
        _lastPathModalPose.Clear();
        _lastPathModalChar.Clear();

        _path.Add(_libraryManager.Root);
        _lastPath.Add(_libraryManager.Root);
        _lastPathModalPose.Add(_libraryManager.Root);
        _lastPathModalChar.Add(_libraryManager.Root);

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

    public override bool DrawConditions()
    {
        if(_isModal)
            return false;

        return base.DrawConditions();
    }

    public override void Draw()
    {
        DrawLibrary();
    }

    public void DrawModal()
    {
        if(!this.IsOpen || !this._isModal || _modalFilter == null)
            return;

        ImGui.OpenPopup($"Import {_modalFilter.Name}##brio_library_popup");

        ImGui.SetNextWindowSizeConstraints(MinimumSize, ImGui.GetIO().DisplaySize);

        using(var popup = ImRaii.PopupModal($"Import {_modalFilter.Name}##brio_library_popup"))
        {
            if(popup.Success)
            {
                DrawLibrary();
            }
        }
    }

    private void DrawLibrary()
    {
        using(ImRaii.PushId("##brio_library"))
        {

            DrawFilters();

            if(_selectedFilter == null)
                return;

            float pathWidth = WindowContentWidth - SearchWidth;
            if(_isModal)
                pathWidth -= 155;

            ImGui.Spacing();

            DrawPath(pathWidth);

            ImGui.SameLine();

            if(_isModal)
            {
                ImGui.SameLine();

                if(ImBrio.Button("Browse for File", FontAwesomeIcon.FolderOpen, new Vector2(155, 0)))
                    DoBrowse();

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Browse for a file");
            }

            ImGui.SameLine();
            DrawSearch();

            var windowSize = ImGui.GetWindowSize();
            using(var child = ImRaii.Child("###left_pane", new Vector2(windowSize.X - InfoPaneWidth, -1), false,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(child.Success == false)
                    return;

                float entriesPaneHeight = ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight() - ImGui.GetStyle().ItemSpacing.Y;
                float entriesPaneWidth = ImBrio.GetRemainingWidth();
                using(var entriesChild = ImRaii.Child("###library_entries_pane", new Vector2(entriesPaneWidth, entriesPaneHeight), true))
                {
                    if(entriesChild.Success)
                    {
                        DrawFiles();
                    }
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
                    if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier) && mouseWheel != 0)
                    {
                        float val = _configurationService.Configuration.Library.IconSize;
                        val = Math.Clamp(val + mouseWheel, MinEntrySize, MaxEntrySize);
                        _configurationService.Configuration.Library.IconSize = val;
                        _configurationService.Save();
                    }
                }

                DrawFooter();
            }

            ImGui.SameLine();


            using(var child = ImRaii.Child("###right_pane", new Vector2(ImBrio.GetRemainingWidth(), -1), false,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(child.Success == false)
                    return;

                float paneHeight = ImBrio.GetRemainingHeight() - (ImBrio.GetLineHeight() + ImGui.GetStyle().ItemSpacing.Y);

                using(var child2 = ImRaii.Child("###library_info_pane", new Vector2(ImBrio.GetRemainingWidth(), paneHeight), true,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    if(child2.Success == false)
                        return;

                    if(_selected != null)
                    {
                        DrawInfo(_selected);
                    }
                    else
                    {
                        DrawInfo(_path[^1]);
                    }
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

                            using(ImRaii.PushColor(ImGuiCol.Text, isFavorite ? TheameManager.CurrentTheame.Accent.AccentColor : UIConstants.ToggleButtonInactive))
                            {
                                if(ImBrio.FontIconButton(FontAwesomeIcon.Heart))
                                {
                                    if(isFavorite)
                                        config.Library.Favorites.Remove(ieb.Identifier);
                                    else
                                        config.Library.Favorites.Add(ieb.Identifier);

                                    ConfigurationService.Instance.Save();
                                }
                            }

                            if(ImGui.IsItemHovered())
                                ImGui.SetTooltip(isFavorite ? "Remove from favorites" : "Add to favorites");

                            ImGui.SameLine();
                        }
                    }

                    bool isPoseModal = _modalFilter?.Name == "Poses";

                    int offset = 200;
                    if(isPoseModal)
                        offset += 30;

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImBrio.GetRemainingWidth() - (offset + ImGui.GetStyle().ItemSpacing.X)));

                    if(isPoseModal)
                    {
                        if(ImBrio.Button("##importPoseOptionButton", FontAwesomeIcon.Cog, new Vector2(25, 0), hoverText: "Import Options"))
                        {
                            ImGui.OpenPopup("import_options_popup_lib");
                        }

                        using(var popup = ImRaii.Popup("import_options_popup_lib"))
                        {
                            if(popup.Success)
                            {
                                PosingEditorCommon.DrawImportOptionEditor(_posingService.DefaultImporterOptions);
                            }
                        }

                        ImGui.SameLine();
                    }

                    var doDisable = isIEB == false;
                    if(doDisable)
                        ImGui.BeginDisabled();

                    if(ImBrio.Button("Import", FontAwesomeIcon.Check, new Vector2(100, 0)))
                    {
                        if(_selected != null)
                        {
                            OnOpen(_selected);
                        }
                    }

                    if(doDisable)
                        ImGui.EndDisabled();

                    ImGui.SameLine();

                    if(ImBrio.Button("Cancel", FontAwesomeIcon.Times, new Vector2(100, 0)))
                    {
                        Close();
                    }
                }
                else
                {
                    if(_selected is not null)
                    {
                        _selected.DrawActions(_isModal);
                    }
                    else
                    {
                        _path[^1].DrawActions(_isModal);
                    }
                }
            }

            DrawSearchSuggest();
        }
    }

    private void DoBrowse()
    {
        if(_modalFilter != null && _modalCallback != null)
            LibraryManager.GetWithFilePicker(_modalFilter, _modalCallback);

        Close();
    }

    private void DrawFilters()
    {
        List<string> ops = [];
        int selected = 0;

        if(_isModal && _modalFilter != null)
        {
            ops.Add(_favoritesFilter.Name);
            if(_favoritesFilter == _selectedFilter)
                selected = 0;

            ops.Add(_modalFilter.Name);
            if(_modalFilter == _selectedFilter)
                selected = 1;

            if(ImBrio.ToggleButtonStrip("library_filters_selector", new(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, [.. ops]))
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
            ops.Add(_favoritesFilter.Name);
            if(_favoritesFilter == _selectedFilter)
                selected = 0;

            ops.Add(_charactersFilter.Name);
            if(_charactersFilter == _selectedFilter)
                selected = 1;

            ops.Add(_posesFilter.Name);
            if(_posesFilter == _selectedFilter)
                selected = 2;


            if(ImBrio.ToggleButtonStrip("library_filters_selector", new(ImBrio.GetRemainingWidth(), ImBrio.GetLineHeight()), ref selected, [.. ops]))
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

        TryRefresh(true);
    }

    private void DrawPath(float width = -1)
    {
        float lineHeight = ImBrio.GetLineHeight();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);

        if(width == -1)
            width = ImBrio.GetRemainingWidth();

        try
        {
            using(var child = ImRaii.Child("library_path_input", new(width, lineHeight), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(child.Success == false)
                    return;

                using(ImRaii.PushColor(ImGuiCol.Button, 0))
                {
                    // Go Up Button
                    using(ImRaii.Disabled(_path.Count <= 1))
                    {
                        if(ImBrio.FontIconButton(FontAwesomeIcon.CaretUp, new(PathBarButtonWidth, lineHeight)))
                        {
                            _path.RemoveAt(_path.Count - 1);
                            ClearFilters();
                        }
                    }

                    ImGui.SameLine();

                    // Path segments
                    for(int i = 0; i < _path.Count; i++)
                    {
                        if(i > 0)
                        {
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY());
                            ImBrio.FontIcon(FontAwesomeIcon.CaretRight);
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
                    float blankWidth = ImBrio.GetRemainingWidth() - PathBarButtonWidth - ImGui.GetStyle().ItemSpacing.X;
                    if(blankWidth != 0 && lineHeight != 0)
                    {
                        if(ImGui.InvisibleButton("###library_path_input_blank", new(blankWidth, lineHeight)))
                        {
                            // consider: clicking here swaps to an InputText for pasting paths?
                        }

                        ImGui.SameLine();
                    }
                    else
                    {
                        Brio.Log.Warning($"<{blankWidth},{lineHeight}>");
                    }

                    // Refresh Button
                    using(ImRaii.Disabled(_isRescanning))
                    {
                        if(ImBrio.FontIconButton(FontAwesomeIcon.Repeat, new(PathBarButtonWidth, lineHeight)))
                        {
                            ReScan();
                        }

                        if(ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Scan all library sources and refresh the view");
                        }
                    }
                }
            }
        }
        catch
        {
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    private unsafe void DrawSearch()
    {
        float searchBarWidth = ImBrio.GetRemainingWidth();
        float searchBarHeight = ImBrio.GetLineHeight();
        Vector2 searchBarPosition = ImGui.GetCursorScreenPos();

        using(ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg)))
        {
            using(ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding))
            {
                try
                {
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

                            using(ImRaii.PushColor(ImGuiCol.FrameBg, 0x000000))
                            {
                                if(ImGui.InputText("###library_search_input", ref _searchText,  256, 
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
                            }

                            _isSearchFocused = ImGui.IsItemActive();

                            // TODO: Try to capture backspace keys to remove tags.

                            if(!_isSearchFocused)
                            {
                                _searchLostFocus++;
                            }
                            else
                            {
                                _searchLostFocus = 0;
                            }

                            _searchSuggestPos = new Vector2(searchBarPosition.X, searchBarPosition.Y + searchBarHeight);
                            _searchSuggestSize = new Vector2(searchBarWidth, searchBarHeight);
                        }
                    }
                }
                catch
                {

                }
            }
        }
    }

    private void ClearSearchText()
    {
        _searchText = string.Empty;
        _searchTextNeedsClear = true;
    }

    private int OnSearchFunc(ref ImGuiInputTextCallbackData data)
    {
        if(_searchTextNeedsClear)
        {
            _searchTextNeedsClear = false;
            _searchText = string.Empty;

            // clear the search input buffer
            data.BufTextLen = 0;
            data.BufSize = 0;
            data.CursorPos = 0;
            data.SelectionStart = 0;
            data.SelectionEnd = 0;
            data.BufDirty = 1;
        }

        return 1;
    }

    private void DrawSearchSuggest()
    {
        if((_searchSuggestPos is null || _searchSuggestSize is null))
            return;

        if(_isSearchFocused || _isSearchSuggestFocused)
            _isSearchSuggestWindowOpen = true;

        if(_isSearchSuggestWindowOpen is false)
            return;

        using(ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(7, 7)))
        {
            List<Tag> availableTags = GetAvailableTags(SearchUtility.ToQuery(_searchText));

            int trimmedTags = 0;
            if(availableTags.Count > MaxTagsInSuggest)
            {
                trimmedTags = availableTags.Count - MaxTagsInSuggest;
                availableTags = availableTags.GetRange(0, MaxTagsInSuggest);
            }

            int lineCount = 2;
            int itemsinLine = 0;
            foreach(var tag in availableTags)
            {
                itemsinLine++;

                float itemWidth = ImGui.CalcTextSize(tag.DisplayName).X + 10;
                float nextX = itemsinLine * itemWidth;

                if(nextX > _searchSuggestPos.Value.X)
                {
                    lineCount++;
                    itemsinLine = 0;
                }
            }

            ImGui.SetNextWindowPos(_searchSuggestPos.Value);

            using var window = ImRaii.Child("##library_search_suggest", new Vector2(_searchSuggestSize.Value.X, (_searchSuggestSize.Value.Y * lineCount) + 10), true,
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Tooltip |
            ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.ChildWindow);
            {
                if(window.Success)
                {
                    bool hasContent = false;
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
                            ImBrio.Text($"plus {trimmedTags} more tags...", 0x88FFFFFF);
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

                _isSearchSuggestFocused = ImGui.IsWindowHovered();
                _isSearchSuggestFocused = ImGui.IsWindowFocused();
            }
        }

        if(_searchLostFocus > 10 && !_searchNeedsFocus)
        {
            _isSearchSuggestWindowOpen = false;
        }
    }

    private List<Tag> GetAvailableTags(string[] query)
    {
        List<Tag> results = [];
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

        if(_toOpen is not null)
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
                Brio.Log.Verbose("IsMouseDoubleClicked");
            }
        }

        if(ImGui.IsItemVisible())
        {
            ImGui.SetCursorPos(pos);

            using(var child = ImRaii.Child($"library_entry_{id}", size, true,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar))
            {
                if(child.Success == false)
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

        using(ImRaii.PushColor(ImGuiCol.ChildBg, 0))
        {
            using(var child = ImRaii.Child("###library_info_pane", new Vector2(ImBrio.GetRemainingWidth(), ImBrio.GetRemainingHeight()), false))
            {
                if(child.Success)
                {
                    entry.DrawInfo(this);
                }
            }
        }
    }

    private void OnOpen(EntryBase entry)
    {
        if(entry is not null and GroupEntryBase dir)
        {
            _path.Add(dir);
            TryRefresh(false);
        }
        else if(entry is not null and ItemEntryBase itemEntry)
        {
            try
            {
                object? result = itemEntry.Load();

                if(result is not null)
                {
                    if(_isModal)
                    {
                        _modalCallback?.Invoke(result);

                        Close();
                    }
                    else
                    {
                        itemEntry.InvokeDefaultAction(_entityManager.SelectedEntity);
                    }
                }
            }
            catch(Exception ex)
            {
                Brio.Log.Error(ex, "Exception while invoking library OnOpen Callback!");
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

            _libraryManager.Root.FilterEntries([.. filters]);

            _allTags.Clear();
            currentEntry.GetAllTags(ref _allTags);

            // Add the search filter last, and re-filter the entries now that we have all the
            // tags
            if(!string.IsNullOrEmpty(_searchText))
            {
                filters.Add(_searchFilter);
                _libraryManager.Root.FilterEntries([.. filters]);
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
