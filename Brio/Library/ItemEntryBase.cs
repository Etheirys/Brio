using Brio.Library.Actions;
using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.UI.Controls.Stateless;
using Brio.UI.Windows;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.Library;

/// <summary>
/// An item entry is an entry in teh library that the user can load, or otherwise perform actions on.
/// </summary>
internal abstract class ItemEntryBase : EntryBase
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
        Tag? selected = ImBrio.DrawTags(this.Tags);
        if(selected != null)
        {
            window.TagFilter.Add(selected);
            window.Refresh(true);
        }
    }
}
