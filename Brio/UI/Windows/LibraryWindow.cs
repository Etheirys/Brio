using Brio.Config;
using Brio.Files;
using Brio.Game.Types;
using Brio.Library;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace Brio.UI.Windows;

internal class LibraryWindow : Window
{
    private const float InfoPaneWidth = 300;

    private readonly ConfigurationService _configurationService;
    private readonly LibraryManager _libraryManager;
    private readonly IPluginLog _log;

    private readonly static List<LibraryFilterBase> filters = new()
    {
        new LibraryFavoritesFilter(),
        new LibraryTypeFilter("Characters", typeof(AnamnesisCharaFile), typeof(ActorAppearanceUnion)),
        new LibraryTypeFilter("Poses", typeof(PoseFile), typeof(CMToolPoseFile)),
    };

    private LibraryFilterBase _currentFilter = filters[0];
    private LibraryStringFilter _searchFilter = new();

    private readonly List<ILibraryEntry> _path = new();
    private IEnumerable<ILibraryEntry>? _currentEntries;
    private ILibraryEntry? _toOpen = null;
    private ILibraryEntry? _selected = null;
    private float spinnerAngle = 0;

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
    public bool IsSearching => !string.IsNullOrEmpty(_searchFilter.SearchString);

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

            if(_currentFilter == null)
                return;

            ImGui.Spacing();
            ImGui.Spacing();
            DrawPath(WindowContentWidth - 200);
            ImGui.SameLine();
            DrawSearch(200);


            var windowSize = ImGui.GetWindowSize();
            using(var child = ImRaii.Child("###left_pane", new Vector2(windowSize.X - InfoPaneWidth, -1), true))
            {
                if(child.Success)
                {
                    DrawFiles();
                }
            }

            ImGui.SameLine();

            using(var child = ImRaii.Child("###right_pane", new Vector2(ImBrio.GetRemainingWidth(), -1), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if(child.Success)
                {
                    if(_selected != null)
                    {
                        DrawInfo(_selected);
                    }
                }
            }
           
        }
    }

    private void DrawFilters()
    {
        if(filters.Count <= 1)
            return;

        float buttonWidth = (WindowContentWidth / filters.Count) - ImGui.GetStyle().FramePadding.X;
        for(int i = 0; i < filters.Count; i++)
        {
            LibraryFilterBase filter = filters[i];
            bool isCurrent = filter == _currentFilter;

            if (i > 0)
                ImGui.SameLine();

            ImBrio.ToggleButton(filter.Name, new(buttonWidth, 0), ref isCurrent, false);

            if(isCurrent && _currentFilter != filter)
            {
                _currentFilter = filter;
                Refresh(true);
            }
        }
    }

    private void DrawPath(float width = -1)
    {
        float lineHeight = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().FramePadding.Y;

        if(_path.Count <= 1)
            ImGui.BeginDisabled();

        if (ImBrio.FontIconButton(FontAwesomeIcon.CaretUp))
        {
            _path.RemoveAt(_path.Count - 1);
            Refresh(false);
        }

        if(_path.Count <= 1)
            ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);

        if(width == -1)
            width = WindowContentWidth;

        width -= ImGui.GetCursorPosX() - ImGui.GetStyle().FramePadding.X;
        width -= 25;

        using(var child = ImRaii.Child("library_path_input", new(width, lineHeight)))
        {
            if(!child.Success)
                return;

            ImGui.PushStyleColor(ImGuiCol.Button, 0);

            for (int i = 0; i < _path.Count; i++)
            {
                if (i > 0)
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
                    ImBrio.FontIcon(FontAwesomeIcon.CaretRight, 0.5f);
                    ImGui.SameLine();
                }

                if (i + 1 > _path.Count - 1)
                    ImGui.BeginDisabled();

                if (ImGui.Button(_path[i].Name))
                {
                    _path.RemoveRange(i + 1, (_path.Count - 1) - i);
                    Refresh(false);
                    break;
                }

                if(i + 1 > _path.Count - 1)
                    ImGui.EndDisabled();

                ImGui.SameLine();
            }

            ImGui.PopStyleColor();

            ImGui.SameLine();
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        ImGui.SameLine();

        if (_libraryManager.IsScanning)
            ImGui.BeginDisabled();

        if (ImBrio.FontIconButton(FontAwesomeIcon.Repeat))
        {
            _libraryManager.Scan();
        }

        if(_libraryManager.IsScanning)
        {
            ImGui.EndDisabled();
        }
    }

    private void DrawSearch(float width = -1)
    {
        ImGui.SetNextItemWidth(width - ImGui.GetStyle().FramePadding.X * 2);
        string searchText = _searchFilter.SearchString ?? string.Empty;
        if (ImGui.InputTextWithHint("###library_search_input", "Search", ref searchText, 256))
        {
            _searchFilter.SearchString = searchText;
            Refresh(true);
        }
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

    private void DrawEntry(ILibraryEntry entry, float width, int id)
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

    private void DrawInfo(ILibraryEntry entry)
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
            using(var child = ImRaii.Child($"library_info_image", size, true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs))
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

    private void OnOpen(ILibraryEntry entry)
    {
        if (entry.FileType != null)
        {
            // open the file?
        }
        else
        {
            _path.Add(entry);
            Refresh(false);
        }
    }

    private void Refresh(bool filter)
    {
        if(_libraryManager.IsScanning)
            return;

        _selected = null;

        if (_currentEntries != null)
        {
            foreach(ILibraryEntry entry in _currentEntries)
            {
                entry.IsVisible = false;
            }
        }

        if(filter)
        {
            if(IsSearching)
            {
                _libraryManager.Root.FilterEntries(_currentFilter, _searchFilter);
            }
            else
            {
                _libraryManager.Root.FilterEntries(_currentFilter);
            }
        }

        ILibraryEntry currentEntry = _path[_path.Count - 1];
        _currentEntries = currentEntry.GetFilteredEntries(IsSearching);

        if(_currentEntries != null)
        {
            foreach(ILibraryEntry entry in _currentEntries)
            {
                entry.IsVisible = true;
            }
        }
    }
}
