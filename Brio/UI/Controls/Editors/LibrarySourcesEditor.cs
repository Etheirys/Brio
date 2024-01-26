using Brio.Config;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;

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
                service.Save();
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImBrio.GetRemainingWidth() - (buttonWidth * 2) - ImGui.GetStyle().ItemSpacing.X));
        if(ImBrio.FontIconButton(FontAwesomeIcon.Edit, new(buttonWidth, 0)))
        {
            ImGui.OpenPopup("###settings_edit_source");
            isEditing = true;
        }

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
                    service.Save();
                }

                ImGui.EndPopup();
            }
        }
    }

    private static void DrawSourceItem(int id, LibraryConfiguration.SourceConfigBase sourceConfig, ref bool isSelected)
    {
        float itemHeight = ImBrio.GetLineHeight() * 2;

        
        float itemTop = ImGui.GetCursorPosY();
        if(ImGui.Selectable($"###settings_source_{id}_{sourceConfig.Name}_selectable", ref isSelected, ImGuiSelectableFlags.None, new(ImBrio.GetRemainingWidth(), itemHeight)))
        {

        }

        bool isHover = ImGui.IsItemActive();
        ImGui.SetCursorPosY(itemTop + ImGui.GetStyle().FramePadding.Y);

        ImGui.Text(sourceConfig.Name ?? "Unnamed Source");

        if(sourceConfig is LibraryConfiguration.FileSourceConfig fileSource)
        {
            DrawSource(fileSource, false);
        }

        ImGui.SetCursorPosY(itemTop + itemHeight + ImGui.GetStyle().ItemSpacing.Y);
    }

    private static void DrawSource(LibraryConfiguration.FileSourceConfig fileSource, bool edit)
    {
        if(edit)
        {
            // TODO: make a nicer version of this...
            // maybe a 'select folder' button?


            ////string[] names = Enum.GetNames<Environment.SpecialFolder>();
            ///
            if(fileSource.Root == null)
                fileSource.Root = Environment.SpecialFolder.MyComputer;

            List<string> names = new();
            names.Add(Environment.SpecialFolder.MyComputer.ToString());
            names.Add(Environment.SpecialFolder.MyDocuments.ToString());
            names.Add(Environment.SpecialFolder.Desktop.ToString());
            names.Add(Environment.SpecialFolder.LocalApplicationData.ToString());
            int current = names.IndexOf(fileSource.Root.ToString()!);

            if(ImGui.Combo("Root", ref current, names.ToArray(), names.Count))
            {
                fileSource.Root = Enum.Parse<Environment.SpecialFolder>(names[current]);
            }

            string path = fileSource.Path ?? string.Empty;
            if(ImGui.InputText("Path", ref path, 120))
            {
                fileSource.Path = path;
            }
        }
        else
        {
            if(fileSource.Root != null && fileSource.Root != Environment.SpecialFolder.MyComputer)
            {
                ImGui.TextDisabled($"{fileSource.Root}{fileSource.Path}");
            }
            else if(fileSource.Path != null)
            {
                ImGui.TextDisabled(fileSource.Path);
            }
        }
    }
}
