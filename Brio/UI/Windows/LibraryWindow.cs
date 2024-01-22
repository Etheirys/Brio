using Brio.Config;
using Brio.Library;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.UI.Windows;

internal class LibraryWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly LibraryManager _libraryManager;

    private CategoryBase? _currentCategory;
    private bool _isEditingPathText = false;

    public LibraryWindow(ConfigurationService configurationService, LibraryManager libraryManager)
        : base($"{Brio.Name} Library###brio_library_window")
    {
        this.Namespace = "brio_library_namespace";
        this.Size = new(600, 450);

        _configurationService = configurationService;
        _libraryManager = libraryManager;

        IsOpen = true;
    }

    private float WindowContentWidth => ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;

    public override void Draw()
    {
        using(ImRaii.PushId("brio_library"))
        {
            DrawCategoryPicker();

            if(_currentCategory == null)
                return;

            DrawPath();
        }
    }


    private void DrawCategoryPicker()
    {
        if(_currentCategory == null)
            _currentCategory = _libraryManager.Categories.FirstOrDefault();

        float buttonWidth = (WindowContentWidth / _libraryManager.Categories.Count()) - ImGui.GetStyle().CellPadding.X;
        foreach(CategoryBase category in _libraryManager.Categories)
        {
            bool isCurrent = category == _currentCategory;
            ImGui.SameLine();
            ImBrio.ToggleButton(category.Title, new(buttonWidth, 0), ref isCurrent, false);

            if(isCurrent)
            {
                _currentCategory = category;
            }
        }
    }

    private void DrawPath()
    {
        string currentPath = "Library/Characters/";
        float lineHeight = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().FramePadding.Y;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg));

        using(var child = ImRaii.Child("library_path_input", new(-1, lineHeight)))
        {
            if(_isEditingPathText)
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.SetKeyboardFocusHere();
                ImGui.InputText("###library_path_input", ref currentPath, 256);

                if(!ImGui.IsItemFocused())
                {
                    _isEditingPathText = false;
                }
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, 0);



                ImGui.Button("Library");
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
                ImBrio.FontIcon(FontAwesomeIcon.ChevronRight, 0.5f);
                ImGui.SameLine();
                ImGui.Button("Characters");

                ImGui.PopStyleColor();

                ImGui.SameLine();
                float width = WindowContentWidth - ImGui.GetCursorPosX();
                if (ImGui.InvisibleButton("###library_path_input", new(width, lineHeight)))
                {
                    _isEditingPathText = true;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
            }
        }

        ImGui.PopStyleColor();
    }
}
