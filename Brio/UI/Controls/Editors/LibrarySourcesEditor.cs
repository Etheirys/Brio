using Brio.Config;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System;
using System.IO;
using System.Numerics;
using static Brio.Config.LibraryConfiguration;

namespace Brio.UI.Controls.Editors;
public static class LibrarySourcesEditor
{
    static Vector2 MinimumSize = new(400, 95);

    public static bool HasSourcesChanged = true;

    static SourceConfigBase? selectedItem;

    static bool isEditing;
    static bool isNewItem;

    static bool isFolderDialogOpen;
    static bool isItemEditorOpen;

    public static void Draw(string? label, ConfigurationService service, LibraryConfiguration config, float? heightPadding = null)
    {
        config.ReEstablishDefaultPaths();

        float buttonWidth = 32;
        float paneHeight = ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight() - ImGui.GetStyle().ItemSpacing.Y;

        if(heightPadding.HasValue)
        {
            paneHeight -= heightPadding.Value;
        }

        using(var child = ImRaii.Child("library_sources_pane", new(-1, paneHeight), true))
        {
            if(child.Success)
            {
                if(label is not null)
                {
                    ImGui.Text(label);
                }

                int index = 0;
                foreach(SourceConfigBase sourceConfig in config.GetAll())
                {
                    bool isItemSelected = selectedItem == sourceConfig;

                    DrawSourceItem(index, sourceConfig, ref isItemSelected);

                    if(isItemSelected && sourceConfig is not null)
                    {
                        selectedItem = sourceConfig;
                    }

                    index++;
                }
            }
        }

        using(ImRaii.Disabled(selectedItem is null || selectedItem.CanEdit == false))
        {
            // Remove Item
            {
                if(ImBrio.FontIconButton(FontAwesomeIcon.Minus, new(buttonWidth, 0)))
                {
                    // TODO: confirm?
                    if(selectedItem is not null)
                    {
                        config.RemoveSource(selectedItem);

                        HasSourcesChanged = true;

                        service.ApplyChange();

                        selectedItem = null;
                    }
                }

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove the selected source");
            }

            ImGui.SameLine();

            // Toggle Item
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImBrio.GetRemainingWidth() - (buttonWidth * 3) - (ImGui.GetStyle().ItemSpacing.X * 2)));
                if(ImBrio.FontIconButton(FontAwesomeIcon.ToggleOff, new(buttonWidth, 0)))
                {
                    if(selectedItem != null)
                    {
                        selectedItem.Enabled = !selectedItem.Enabled;

                        HasSourcesChanged = true;

                        service.ApplyChange();
                    }
                }

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Toggle the selected source on or off");
            }

            ImGui.SameLine();

            // Edit Item
            {
                if(ImBrio.FontIconButton(FontAwesomeIcon.Edit, new(buttonWidth, 0)))
                {
                    isEditing = true;
                    isItemEditorOpen = true;
                    isNewItem = false;

                    ImGui.OpenPopup("###settings_edit_source");
                }

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Edit the selected source");

            }
        }


        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImBrio.GetRemainingWidth() - buttonWidth));

        // New Item
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.Plus, new(buttonWidth, 0)))
            {
                isEditing = true;
                isItemEditorOpen = true;
                isNewItem = true;

                selectedItem = new FileSourceConfig
                {
                    Name = "New Source"
                };

                ImGui.OpenPopup("###settings_edit_source");
            }

            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Add a new source");
        }

        if(isEditing && selectedItem is not null)
        {
            ImGui.SetNextWindowSize(new(450, 200));

            if(isItemEditorOpen && isFolderDialogOpen)
            {
                isFolderDialogOpen = false;
                isItemEditorOpen = true;

                ImGui.CloseCurrentPopup();
                ImGui.OpenPopup("###settings_edit_source");
            }

            ImGui.SetNextWindowSizeConstraints(MinimumSize, MinimumSize);
            ImGui.SetNextWindowPos(new Vector2((ImGui.GetIO().DisplaySize.X / 2) - (MinimumSize.X / 2), (ImGui.GetIO().DisplaySize.Y / 2) - (MinimumSize.Y / 2)));

            using(var popup = ImRaii.PopupModal("###settings_edit_source", ref isItemEditorOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar))
            {
                if(popup.Success == false)
                    return;

                string name = selectedItem.Name ?? string.Empty;
                if(ImGui.InputText("Name###settings_edit_source_name", ref name, 80))
                {
                    selectedItem.Name = name;
                }

                if(selectedItem is FileSourceConfig fileSource)
                {
                    fileSource.Root = Environment.SpecialFolder.MyComputer;

                    string path = fileSource.Path ?? string.Empty;

                    if(ImGui.InputText("Path", ref path, 120))
                    {
                        fileSource.Path = path;
                    }

                    if(ImGui.Button("Browse for Folder", new(120, 0)))
                    {
                        isItemEditorOpen = false;
                        isFolderDialogOpen = true;

                        UIManager.Instance.FileDialogManager.OpenFolderDialog(
                            "Browse for Folder",
                            (success, path) =>
                            {
                                if(success && !string.IsNullOrEmpty(path))
                                {
                                    fileSource.Path = path;
                                    isItemEditorOpen = true;
                                }
                                else // No Path
                                {
                                    ClosePopUp();
                                }
                            },
                            path,
                            true);
                    }

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight());

                    if(isNewItem)
                    {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImBrio.GetRemainingWidth() - 210);

                        if(string.IsNullOrEmpty(fileSource.Path))
                            ImGui.BeginDisabled();

                        if(ImGui.Button("Save", new(100, 0)))
                        {
                            config.AddSource(selectedItem);

                            HasSourcesChanged = true;

                            service.ApplyChange();

                            ClosePopUp();
                        }

                        if(string.IsNullOrEmpty(fileSource.Path))
                            ImGui.EndDisabled();

                        ImGui.SameLine();

                        if(ImGui.Button("Cancel", new(100, 0)))
                        {
                            ClosePopUp();
                        }
                    }
                    else
                    {
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImBrio.GetRemainingWidth() - 110);

                        if(ImGui.Button("OK", new(100, 0)))
                        {
                            HasSourcesChanged = true;

                            service.ApplyChange();

                            ClosePopUp();
                        }
                    }
                }
                else
                {
                    ClosePopUp();
                }
            }

            static void ClosePopUp()
            {
                isEditing = false;
                isItemEditorOpen = false;
                isNewItem = false;

                selectedItem = null;

                ImGui.CloseCurrentPopup();
            }
        }
    }

    private static void DrawSourceItem(int id, SourceConfigBase sourceConfig, ref bool isSelected)
    {
        float itemHeight = (ImBrio.GetLineHeight() * 2) + ImGui.GetStyle().ItemSpacing.Y;

        float itemTop = ImGui.GetCursorPosY();
        ImGui.Selectable($"###settings_source_{id}_selectable", ref isSelected, ImGuiSelectableFlags.None, new(ImBrio.GetRemainingWidth(), itemHeight));

        bool isHover = ImGui.IsItemHovered();

        ImGui.SetCursorPosY(itemTop + ImGui.GetStyle().FramePadding.Y);

        ImBrio.FontIcon(sourceConfig.Enabled ? FontAwesomeIcon.Check : FontAwesomeIcon.Times);
        ImGui.SameLine();
        ImGui.Text(sourceConfig.Name ?? "Unnamed Source");

        if(sourceConfig is FileSourceConfig fileSource)
        {
            DrawSource(fileSource);
        }

        ImGui.SetCursorPosY(itemTop + itemHeight + ImGui.GetStyle().ItemSpacing.Y);
    }

    private static void DrawSource(FileSourceConfig config)
    {
        if(config.Root is not null and not Environment.SpecialFolder.MyComputer)
        {
            string currentPath = Environment.GetFolderPath((Environment.SpecialFolder)config.Root) + config.Path;
            bool valid = Directory.Exists(currentPath);
            if(!valid)
            {
                ImBrio.FontIcon(FontAwesomeIcon.ExclamationTriangle);
                ImGui.SameLine();

                if(ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("This directory does not exist");
                }
            }

            ImGui.TextDisabled($"{config.Root}{config.Path}");
        }
        else if(config.Path is not null)
        {
            ImGui.TextDisabled(config.Path);
        }
    }
}
