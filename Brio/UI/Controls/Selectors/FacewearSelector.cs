using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

public class FacewearSelector(string id) : Selector<FacewearUnion>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    protected override void PopulateList()
    {
        foreach(var glasses in GameDataProvider.Instance.Glasses.Values)
            AddItem(glasses);

        AddItem(new None());
    }

    protected override void DrawItem(FacewearUnion union, bool isHovered)
    {
        var (facewearId, facewearName, facewearIcon) = union.Match(
          glasses => ((byte)glasses.RowId, glasses.Name, (uint)glasses.Icon),
          none => ((byte)0, "None", (uint)0x0)
      );

        ImBrio.BorderedGameIcon("icon", facewearIcon, "Images.Facewear.png", description: $"{facewearName}\n{facewearId}", flags: ImGuiButtonFlags.None, size: IconSize);
    }

    protected override bool Filter(FacewearUnion item, string search)
    {
        return item.Match(
            (glasses) =>
            {
                if(string.IsNullOrEmpty(glasses.Name.ToString()))
                    return false;

                var searchText = $"{glasses.Name} {glasses.RowId}";

                if(searchText.Contains(search, System.StringComparison.InvariantCultureIgnoreCase))
                    return true;

                return false;
            },
            none => true
       );
    }

    protected override int Compare(FacewearUnion itemA, FacewearUnion itemB)
    {
        // None to top
        if(itemA.Value is None && itemB.Value is not None)
            return -1;

        if(itemA.Value is not None && itemB.Value is None)
            return 1;

        // Get name
        var textA = itemA.Match(
            glasses => glasses.Name.ToString(),
            none => ""
        );

        var textB = itemB.Match(
            glasses => glasses.Name.ToString(),
            none => ""
        );

        // Blank string to bottom
        if(string.IsNullOrEmpty(textA) && !string.IsNullOrEmpty(textB))
            return 1;

        if(!string.IsNullOrEmpty(textA) && string.IsNullOrEmpty(textB))
            return -1;

        // Alphabetical
        return string.Compare(textA, textB, System.StringComparison.InvariantCultureIgnoreCase);
    }
}
