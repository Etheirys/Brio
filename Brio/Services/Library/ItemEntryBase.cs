
using Brio.Config;
using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Windows;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.Library;

/// <summary>
/// An item entry is an entry in teh library that the user can load, or otherwise perform actions on.
/// </summary>
public abstract class ItemEntryBase : EntryBase
{
    protected ItemEntryBase(SourceBase? source)
        : base(source)
    {
    }

    public virtual string? Description { get; }
    public virtual string? Author { get; }
    public virtual string? Version { get; }
    public virtual IDalamudTextureWrap? PreviewImage { get; }
    public abstract Type LoadsType { get; }

    public abstract object? Load();

    protected virtual void AddTag(string tag) { }
    protected virtual void RemoveTag(string tag) { }
    private Tag? _contextSource = null;
    private string _newTagText = String.Empty;

    public override bool PassesFilters(params FilterBase[] filters)
    {
        foreach(FilterBase filter in filters)
        {
            if(!filter.Filter(this))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Search(string[] query)
    {
        bool match = base.Search(query);
        match |= SearchUtility.Matches(this.Description, query);
        match |= SearchUtility.Matches(this.Author, query);
        match |= SearchUtility.Matches(this.Version, query);
        return match;
    }

    public override void DrawInfo(LibraryWindow window)
    {
        base.DrawInfo(window);

        if(this.PreviewImage != null)
        {
            Vector2 size = ImGui.GetContentRegionAvail();

            size.Y = Math.Min(size.X, size.Y * 0.5f);
            using(var child = ImRaii.Child($"library_info_image", size, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs))
            {
                if(!child.Success)
                    return;

                ImBrio.ImageFit(this.PreviewImage);
            }
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.Spacing();

        if(!string.IsNullOrEmpty(this.Description))
            ImGui.TextWrapped(this.Description);

        ImGui.Spacing();
        ImGui.Spacing();

        if(!string.IsNullOrEmpty(this.Author))
            ImGui.Text($"Author: {this.Author}");

        if(!string.IsNullOrEmpty(this.Version))
            ImGui.Text($"Version: {this.Version}");

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.Text("Tags:");
        ImGui.SameLine();

        var (selected, action) = ImBrio.DrawTags(this.Tags);
        if(selected != null)
        {
            switch (action)
            {
                case ImBrio.MouseAction.Left: 
                    window.TagFilter.Add(selected);
                    window.TryRefresh(true);
                    break;

                case ImBrio.MouseAction.Right:
                    if(selected.Name == Author)
                        ImGui.OpenPopup("author_tag_context_menu");
                    else
                        ImGui.OpenPopup("tag_context_menu");
                    _contextSource = selected;
                    break;

                default:
                    break;
            }
        }

        ImGui.SameLine();
        if(ImGui.SmallButton("+"))
        {
            _newTagText = "";
            ImGui.OpenPopup("new_tag_popup");
        }

        bool openRenamePopup = false;
        if(_contextSource != null && ImGui.BeginPopup("tag_context_menu"))
        {
            if(ImGui.MenuItem("Rename"))
            {
                _newTagText = _contextSource.Name;
                openRenamePopup = true;
            }
            if(ImGui.MenuItem("Delete") )
            {
                RemoveTag(_contextSource.Name);
            }
            ImGui.EndPopup();
        }

        if(ImGui.BeginPopup("author_tag_context_menu"))
        {
            ImGui.Text("Author");
            ImGui.EndPopup();
        }

        if(ImGui.BeginPopup("new_tag_popup"))
        {
            ImGui.SetKeyboardFocusHere();

            ImGui.Text("Tag name:");
            if(ImGui.InputText("###new_tag_input", ref _newTagText, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                AddTag(_newTagText);
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        
        if(openRenamePopup)
            ImGui.OpenPopup("rename_tag_popup");
        if(_contextSource != null && ImGui.BeginPopup("rename_tag_popup"))
        {
            ImGui.SetKeyboardFocusHere();

            ImGui.Text("New tag name");
            if(ImGui.InputText("###rename_tag_input", ref _newTagText, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                RemoveTag(_contextSource.Name);
                AddTag(_newTagText);
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    public override void DrawActions(bool isModal)
    {
        base.DrawActions(isModal);

        // Favorite button
        var config = ConfigurationService.Instance.Configuration;
        bool isFavorite = config.Library.Favorites.Contains(this.Identifier);

        ImGui.PushStyleColor(ImGuiCol.Text, isFavorite ? UIConstants.GizmoRed : UIConstants.ToggleButtonInactive);

        //

        if(ImBrio.FontIconButton(FontAwesomeIcon.Heart))
        {
            if(!isFavorite)
            {
                config.Library.Favorites.Add(this.Identifier);
            }
            else
            {
                config.Library.Favorites.Remove(this.Identifier);
            }

            ConfigurationService.Instance.Save();
        }

        ImGui.PopStyleColor();

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip(isFavorite ? "Remove from favorites" : "Add to favorites");

        ImGui.SameLine();
    }
}
