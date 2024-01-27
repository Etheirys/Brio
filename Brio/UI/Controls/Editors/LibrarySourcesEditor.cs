using Brio.Config;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;

namespace Brio.UI.Controls.Editors;
internal static class LibrarySourcesEditor
{
    public static void Draw(string? label, ConfigurationService service, LibraryConfiguration config, ref LibraryConfiguration.SourceConfigBase? selected, ref bool isEditing)
    {
        config.CheckDefaults();

        float buttonWidth = 32;

        float paneHeight = ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight() - ImGui.GetStyle().ItemSpacing.Y;
        if(ImGui.BeginChild("library_sources_pane", new(-1, paneHeight), true))
        {
            if (label != null)
                ImGui.Text(label);

            int index = 0;
            foreach(LibraryConfiguration.SourceConfigBase sourceConfig in config.GetAllConfigs())
            {
                bool isItemSelected = selected == sourceConfig;
                DrawSourceItem(index, sourceConfig, ref isItemSelected);

                if (isItemSelected)
                {
                    selected = sourceConfig;
                }

                index++;
            }

            ImGui.EndChild();
        }

        if(selected == null)
            ImGui.BeginDisabled();

        if(ImBrio.FontIconButton(FontAwesomeIcon.Minus, new(buttonWidth, 0)))
        {
            // TODO: confirm?
            if(selected != null)
            {
                config.RemoveSource(selected);
                service.ApplyChange();
            }
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Remove the selected source");

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImBrio.GetRemainingWidth() - (buttonWidth * 3) - (ImGui.GetStyle().ItemSpacing.X * 2)));
        if(ImBrio.FontIconButton(FontAwesomeIcon.ToggleOff, new(buttonWidth, 0)))
        {
            if(selected != null)
            {
                selected.Enabled = !selected.Enabled;
                service.ApplyChange();
            }
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Toggle the selected source on or off");

        ImGui.SameLine();
        if(ImBrio.FontIconButton(FontAwesomeIcon.Edit, new(buttonWidth, 0)))
        {
            ImGui.OpenPopup("###settings_edit_source");
            isEditing = true;
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Edit the selected source");

        if(selected == null)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImBrio.GetRemainingWidth() - buttonWidth));
        if(ImBrio.FontIconButton(FontAwesomeIcon.Plus, new(buttonWidth, 0)))
        {
            isEditing = true;
            selected = new LibraryConfiguration.FileSourceConfig();
            selected.Name = "New Source";
            config.AddSource(selected);
            ImGui.OpenPopup("###settings_edit_source");
        }

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Add a new source");

        if(isEditing && selected != null)
        {
            ImGui.SetNextWindowSize(new(400, 200));

            if(ImGui.BeginPopupModal("###settings_edit_source", ref isEditing,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
            {
                string name = selected.Name ?? string.Empty;
                if(ImGui.InputText("Name###settings_edit_source_name", ref name, 80))
                {
                    selected.Name = name;
                }

                if(selected is LibraryConfiguration.FileSourceConfig fileSource)
                {
                    DrawSource(fileSource, true);
                }

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImBrio.GetRemainingWidth() - 100);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImBrio.GetRemainingHeight() - ImBrio.GetLineHeight());
                if(ImGui.Button("Save", new(100, 0)))
                {
                    ImGui.CloseCurrentPopup();
                    service.ApplyChange();
                }

                ImGui.EndPopup();
            }
        }
    }

    private static void DrawSourceItem(int id, LibraryConfiguration.SourceConfigBase sourceConfig, ref bool isSelected)
    {
        float itemHeight = (ImBrio.GetLineHeight() * 2) + ImGui.GetStyle().ItemSpacing.Y;

        float itemTop = ImGui.GetCursorPosY();
        if(ImGui.Selectable($"###settings_source_{id}_selectable", ref isSelected, ImGuiSelectableFlags.None, new(ImBrio.GetRemainingWidth(), itemHeight)))
        {

        }

        bool isHover = ImGui.IsItemActive();
        ImGui.SetCursorPosY(itemTop + ImGui.GetStyle().FramePadding.Y);


        ImBrio.FontIcon(sourceConfig.Enabled ? FontAwesomeIcon.Check : FontAwesomeIcon.Times);
        ImGui.SameLine();
        ImGui.Text(sourceConfig.Name ?? "Unnamed Source");

        if(sourceConfig is LibraryConfiguration.FileSourceConfig fileSource)
        {
            DrawSource(fileSource, false);
        }

        ImGui.SetCursorPosY(itemTop + itemHeight + ImGui.GetStyle().ItemSpacing.Y);
    }

    private static void DrawSource(LibraryConfiguration.FileSourceConfig config, bool edit)
    {
        if(edit)
        {
            // TODO: make a nicer version of this...
            // maybe a 'select folder' button?


            ////string[] names = Enum.GetNames<Environment.SpecialFolder>();
            ///
            if(config.Root == null)
                config.Root = Environment.SpecialFolder.MyComputer;

            List<string> names = new();
            names.Add(Environment.SpecialFolder.MyComputer.ToString());
            names.Add(Environment.SpecialFolder.MyDocuments.ToString());
            names.Add(Environment.SpecialFolder.Desktop.ToString());
            names.Add(Environment.SpecialFolder.LocalApplicationData.ToString());
            int current = names.IndexOf(config.Root.ToString()!);

            if(ImGui.Combo("Root", ref current, names.ToArray(), names.Count))
            {
                config.Root = Enum.Parse<Environment.SpecialFolder>(names[current]);
            }

            string path = config.Path ?? string.Empty;
            if(ImGui.InputText("Path", ref path, 120))
            {
                config.Path = path;
            }
        }
        else
        {
            if(config.Root != null && config.Root != Environment.SpecialFolder.MyComputer)
            {
                string currentPath = Environment.GetFolderPath((Environment.SpecialFolder)config.Root) + config.Path;
                bool valid = Directory.Exists(currentPath);
                if (!valid)
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
            else if(config.Path != null)
            {
                ImGui.TextDisabled(config.Path);
            }
        }
    }
}
