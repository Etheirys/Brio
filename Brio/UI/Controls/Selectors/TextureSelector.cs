using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

public class TextureId(uint id)
{
    public uint Id { get; init; } = id;

    public static implicit operator TextureId(uint id) => new(id);
    public static implicit operator uint(TextureId textureId) => textureId.Id;
}

public enum TextureType
{
    Particle,
    Sky,
    Cloud,
    CloudSide
}

public class TextureSelector : Selector<TextureId>
{
    private readonly TextureType _textureType;
    private readonly uint _maxId;

    protected override Vector2 MinimumListSize { get; } = new( 200, 400);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.AdaptiveSizing;

    public TextureSelector(string id, TextureType textureType, uint maxId = 80) : base(id)
    {
        _textureType = textureType;
        _maxId = maxId;
    }

    protected override void PopulateList()
    {
        for(uint i = 0; i <= _maxId; i++)
        {
            AddItem(new TextureId(i));
        }
    }

    protected override void DrawItem(TextureId textureId, bool isHovered)
    {
        var path = GetTexturePath(textureId.Id);

        ImBrio.BorderedGameTex($"##texture_{textureId.Id}", path, fallback: "Images.UnknownIcon.png", flags: ImGuiButtonFlags.None, size: IconSize);
        
        ImGui.SameLine();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (IconSize.Y / 2) - (ImGui.GetTextLineHeight() / 2));
        ImGui.Text($"ID: {textureId.Id}");
    }

    public string GetTexturePath(uint id)
    {
        return _textureType switch
        {
            TextureType.Particle => $"bgcommon/nature/dust/texture/dust_{Math.Max(0, (int)id - 2):D3}.tex",
            TextureType.Sky => $"bgcommon/nature/sky/texture/sky_{id:D3}.tex",
            TextureType.Cloud => $"bgcommon/nature/cloud/texture/cloud_{id:D3}.tex",
            TextureType.CloudSide => $"bgcommon/nature/cloud/texture/cloudside_{id:D3}.tex",
            _ => throw new ArgumentException($"Unknown texture type: {_textureType}")
        };
    }

    protected override bool Filter(TextureId item, string search)
    {
        if(string.IsNullOrEmpty(search))
            return true;

        var searchText = $"{item.Id}";
        return searchText.Contains(search, StringComparison.InvariantCultureIgnoreCase);
    }

    protected override int Compare(TextureId itemA, TextureId itemB)
    {
        return itemA.Id.CompareTo(itemB.Id);
    }
}
