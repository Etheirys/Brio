using Brio.Config;
using Brio.Files;
using Brio.Library;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Windows;

internal class LibraryWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly LibraryManager _libraryManager;
    private readonly IPluginLog _log;

    private readonly static List<LibraryFilterBase> filters = new()
    {
        new LibraryFavoritesFilter(),
        new LibraryTypeFilter("Characters", typeof(AnamnesisCharaFile)),
        new LibraryTypeFilter("Poses", typeof(PoseFile), typeof(CMToolPoseFile)),
    };

    private LibraryFilterBase _currentFilter = filters[0];
    private LibraryStringFilter _searchFilter = new();

    private readonly List<ILibraryEntry> _path = new();
    private IEnumerable<ILibraryEntry>? _currentEntries;
    private ILibraryEntry? _toOpen = null;

    public LibraryWindow(
        IPluginLog log,
        ConfigurationService configurationService,
        LibraryManager libraryManager)
        : base($"{Brio.Name} Library###brio_library_window")
    {
        this.Namespace = "brio_library_namespace";
        this.Size = new(800, 450);

        _log = log;
        _configurationService = configurationService;
        _libraryManager = libraryManager;

        IsOpen = true;
        _path.Add(_libraryManager.Root);
    }

    private float WindowContentWidth => ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
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

            DrawPath(WindowContentWidth - 200);
            ImGui.SameLine();
            DrawSearch(200);
            DrawFiles();
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

        if (ImBrio.FontIconButton(FontAwesomeIcon.LevelUpAlt))
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
                    ImBrio.FontIcon(FontAwesomeIcon.ChevronRight, 0.5f);
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
        int columnCount = 6;
        int column = 0;
        int index = 0;
        
        float fileWidth = (WindowContentWidth - 50) / columnCount;

        using(var child = ImRaii.Child("library_files_area", new(-1, -1)))
        {
            if(!child.Success)
                return;

            if(_currentEntries != null)
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
        float height = width + 50;
        Vector2 size = new(width, height);
        Vector2 pos = ImGui.GetCursorPos();
        ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ChildBg));

        if (ImGui.Button($"###library_entry_{id}_button", size))
        {
            _toOpen = entry;
        }

        ImGui.PopStyleColor();
        ImGui.SetCursorPos(pos);

        using(var child = ImRaii.Child($"library_entry_{id}", size, false, ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs))
        {
            if(!child.Success)
                return;

            if(entry.Icon != null)
            {
                float fitWidth = width; //// entry.Icon.Width / width * height;
                ImGui.Image(entry.Icon.ImGuiHandle, new(fitWidth, width));
            }

            ImGui.SetCursorPosY(width);
            ImBrio.TextCentered(entry.Name, width);
        }
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
