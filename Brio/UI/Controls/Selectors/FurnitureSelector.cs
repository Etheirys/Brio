using Brio.Resources;
using Brio.Resources.Extra;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

public class FurnitureSelector(string id) : Selector<FurnitureDatabase.FurnitureInfo>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags => SelectorFlags.AllowSearch | SelectorFlags.AdaptiveSizing;

    protected override void PopulateList()
    {
        AddItems(GameDataProvider.Instance.FurnitureDatabase.GetAll());
    }

    protected override bool Filter(FurnitureDatabase.FurnitureInfo item, string search)
    {
        string searchTerm = $"{item.Name} {item.Category}";

        if(searchTerm.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }

    protected override int Compare(FurnitureDatabase.FurnitureInfo itemA, FurnitureDatabase.FurnitureInfo itemB)
    {
        int categoryCompare = string.Compare(itemA.Category, itemB.Category, StringComparison.InvariantCultureIgnoreCase);
        if(categoryCompare != 0)
            return categoryCompare;

        return string.Compare(itemA.Name, itemB.Name, StringComparison.InvariantCultureIgnoreCase);
    }

    protected override void DrawItem(FurnitureDatabase.FurnitureInfo item, bool isHovered)
    {
        ImBrio.BorderedGameIcon("icon", item.IconId, "Images.UnknownIcon.png", flags: ImGuiButtonFlags.None, size: IconSize);

        ImGui.SameLine();

        ImGui.Text($"{item.Name}\n{item.Category}\n{(item.Indoors ? "Indoor" : "Outdoor")}");
    }
}
