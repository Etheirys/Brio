
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

    public virtual bool EditAble { get; }
    public virtual void AddTag(string tag) { }
    public virtual void RemoveTag(string tag) { }
    public virtual void EditDetailsPopup(bool openPopup) { }
    private Tag? _contextSource = null;
    private string _tagName = String.Empty;
    private TagAction _tagAction = TagAction.New;

    private enum TagAction
    {
        New,
        Rename
    }

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

            size.Y = Math.Min(size.X, size.Y * 0.75f);
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
        DrawTags(window);
    }

    private void DrawTags(LibraryWindow window)
    {
        bool openTagNamePopup = false;
        var (selected, action) = ImBrio.DrawTags(this.Tags);
        if(selected != null)
        {
            switch(action)
            {
                case ImBrio.MouseAction.Left:
                    window.TagFilter.Add(selected);
                    window.TryRefresh(true);
                    break;

                case ImBrio.MouseAction.Right:
                    if(!EditAble)
                        break;
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

        if(!EditAble)
            return;

        ImGui.SameLine();
        if(ImGui.SmallButton("+"))
        {
            _tagName = "";
            _tagAction = TagAction.New;
            openTagNamePopup = true;
        }

        if(_contextSource != null && ImGui.BeginPopup("tag_context_menu"))
        {
            if(ImGui.MenuItem("Rename"))
            {
                _tagName = _contextSource.Name;
                _tagAction = TagAction.Rename;
                openTagNamePopup = true;
            }
            if(ImGui.MenuItem("Delete"))
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

        if(openTagNamePopup)
            ImGui.OpenPopup("tag_name_popup");
        if(ImGui.BeginPopup("tag_name_popup"))
        {
            ImGui.SetKeyboardFocusHere();

            ImGui.Text(_tagAction == TagAction.Rename ? "New tag name:" : "Tag name:");
            if(ImGui.InputText("###tag_name_input", ref _tagName, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                if(_tagAction == TagAction.Rename && _contextSource != null)
                    RemoveTag(_contextSource.Name);
                AddTag(_tagName);
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

        if(EditAble is false)
            return;

        bool openPopup = false;
        using(var disabled = ImRaii.Disabled(true))
            if(ImBrio.FontIconButton(FontAwesomeIcon.FilePen))
                openPopup = true;

        if(ImGui.IsItemHovered())
            ImGui.SetTooltip("Edit details (coming soon)");

        EditDetailsPopup(openPopup);

        ImGui.SameLine();
    }
}
