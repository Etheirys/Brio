using Brio.Config;
using Brio.Library;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Brio.UI.Windows;

internal class LibraryWindow : Window
{
    private readonly ConfigurationService _configurationService;
    private readonly LibraryManager _libraryManager;

    private CategoryBase? _currentCategory;
    private bool _isEditingPathText = false;
    private string _currentSearchText = string.Empty;

    public LibraryWindow(ConfigurationService configurationService, LibraryManager libraryManager)
        : base($"{Brio.Name} Library###brio_library_window")
    {
        this.Namespace = "brio_library_namespace";
        this.Size = new(800, 450);

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

            DrawPath(WindowContentWidth - 200);
            ImGui.SameLine();
            DrawSearch(200);
            DrawFiles();
        }
    }


    private void DrawCategoryPicker()
    {
        if(_libraryManager.Categories.Count <= 1)
            return;

        if(_currentCategory == null)
            _currentCategory = _libraryManager.Categories[0];

        float buttonWidth = (WindowContentWidth / _libraryManager.Categories.Count) - ImGui.GetStyle().FramePadding.X;
        for(int i = 0; i < _libraryManager.Categories.Count; i++)
        {
            CategoryBase category = _libraryManager.Categories[i];
            bool isCurrent = category == _currentCategory;

            if (i > 0)
                ImGui.SameLine();

            ImBrio.ToggleButton(category.Title, new(buttonWidth, 0), ref isCurrent, false);

            if(isCurrent)
            {
                _currentCategory = category;
            }
        }
    }

    private void DrawPath(float width = -1)
    {
        string currentPath = "Library/Characters/";
        float lineHeight = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().FramePadding.Y;

        if (ImBrio.FontIconButton(FontAwesomeIcon.LevelUpAlt))
        {

        }

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

            if(_isEditingPathText)
            {
                ImGui.SetNextItemWidth(width);
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
                float buttonWidth = width - ImGui.GetCursorPosX();
                if (ImGui.InvisibleButton("###library_path_input", new(buttonWidth, lineHeight)))
                {
                    _isEditingPathText = true;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);
            }
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    private void DrawSearch(float width = -1)
    {
        ImGui.SetNextItemWidth(width - ImGui.GetStyle().FramePadding.X * 2);
        ImGui.InputTextWithHint("###library_search_input", "Search", ref _currentSearchText, 256);
    }

    private void DrawFiles()
    {
        using(var child = ImRaii.Child("library_files_area", new(-1, -1)))
        {
            if(!child.Success)
                return;
        }
    }
}
