using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets2;
using OneOf.Types;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

internal class FacewearSelector(string id) : Selector<FacewearUnion>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;


    private readonly List<Glasses> _validWeathers = [];

    protected override void PopulateList()
    {
        foreach(var glasses in GameDataProvider.Instance.Glasses.Values)
            AddItem(glasses);

        AddItem(new None());
    }

    protected override void DrawItem(FacewearUnion union, bool isHovered)
    {
        var (facewearId, facewearName, facewearIcon) = union.Match(
          glasses => ((byte)glasses.RowId, glasses.Unknown3, (uint)glasses.Unknown11),
          none => ((byte)0, "None", (uint)0x0)
      );

        ImBrio.BorderedGameIcon("icon", facewearIcon, "Images.Head.png", description: $"{facewearName}\n{facewearId}", flags: ImGuiButtonFlags.None, size: IconSize);
    }

    protected override bool Filter(FacewearUnion item, string search)
    {

        return item.Match(
            (glasses) =>
            {
                if(string.IsNullOrEmpty(glasses.Unknown3))
                    return false;

                var searchText = $"{glasses.Unknown3} {glasses.RowId}";

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
            glasses => glasses.Unknown3,
            none => ""
        );

        var textB = itemB.Match(
            glasses => glasses.Unknown3,
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
