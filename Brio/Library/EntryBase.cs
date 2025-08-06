using Brio.Library.Filters;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.UI.Controls.Stateless;
using Brio.UI.Windows;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Bindings.ImGui;

namespace Brio.Library;

/// <summary>
/// An entry is a library object
/// </summary>
public abstract class EntryBase : ITagged
{
    private SourceBase? _source;

    public EntryBase(SourceBase? source)
    {
        _source = source;
    }

    public abstract string Name { get; }
    public abstract IDalamudTextureWrap? Icon { get; }

    public virtual bool IsVisible { get; set; }
    public TagCollection Tags { get; init; } = new();
    public SourceBase? Source => _source;
    public string? SourceInfo { get; set; }

    public string Identifier => $"{this.Source?.GetpublicId()}||{GetpublicId()}";

    public abstract bool PassesFilters(params FilterBase[] filters);

    public virtual bool Search(string[] query)
    {
        return SearchUtility.Matches(this.Name, query);
    }

    public virtual void Dispose()
    {
    }

    public virtual void DrawInfo(LibraryWindow window)
    {
        if(this.Source != null)
        {
            float x = ImGui.GetCursorPosX();
            float y = ImGui.GetCursorPosY();
            float sourceIconBottom = y;
            if(this.Source.Icon != null)
            {
                ImGui.SetCursorPosY(y + 3);
                ImBrio.ImageFit(this.Source.Icon, new(42, 42));
                sourceIconBottom = ImGui.GetCursorPosY();

                ImGui.SameLine();
                x = ImGui.GetCursorPosX();
            }

            ImGui.Text(this.Source.Name);

            if(this.SourceInfo != null)
            {
                ImGui.SetCursorPosY(y + 18);
                ImGui.SetCursorPosX(x);
                ImGui.SetWindowFontScale(0.7f);
                ImGui.BeginDisabled();
                ImGui.TextWrapped(this.SourceInfo);
                ImGui.EndDisabled();
                ImGui.SetWindowFontScale(1.0f);
            }

            if(ImGui.GetCursorPosY() < sourceIconBottom)
                ImGui.SetCursorPosY(sourceIconBottom);
        }
    }

    public virtual void DrawActions(bool isModal)
    {
    }

    public virtual bool InvokeDefaultAction(object? args)
    {
        return false;
    }

    protected abstract string GetpublicId();
}
