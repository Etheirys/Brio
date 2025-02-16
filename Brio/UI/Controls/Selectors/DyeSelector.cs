using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OneOf.Types;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

public class DyeSelector(string id) : Selector<DyeUnion>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 1.2f;

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.AdaptiveSizing;

    protected override void PopulateList()
    {
        foreach(var stain in GameDataProvider.Instance.Stains.Values)
            AddItem(stain);

        AddItem(new None());
    }

    protected override void DrawItem(DyeUnion item, bool isHovered)
    {
        var (id, name, color) = item.Match(
            stain => ((byte)stain.RowId, stain.Name, (uint)ImBrio.ARGBToABGR(stain.Color)),
            none => ((byte)0, "None", (uint)0)
        );
        string label = $"{name} ({id})";
        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 1.1f);

        if(isHovered)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 5f);
            ImGui.PushStyleColor(ImGuiCol.Border, 0xFFFFFFFF);
        }

        ImBrio.DrawLabeledColor(id.ToString(), color, label, label, size);

        if(isHovered)
        {
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
    }

    protected override bool Filter(DyeUnion item, string search)
    {

        bool isInvalid = item.Match(
            stain => stain.Shade == 0 || stain.RowId == 0,
            none => false
        );

        if(isInvalid)
            return false;

        string match = item.Match(
            stain => $"{stain.Name} {stain.RowId}",
            none => "None 0"
        );

        if(!match.Contains(search, System.StringComparison.InvariantCultureIgnoreCase))
            return false;

        return true;
    }

    protected override int Compare(DyeUnion itemA, DyeUnion itemB)
    {
        // None first
        if(itemA.Value is None && itemB.Value is not None)
            return -1;

        if(itemA.Value is not None && itemB.Value is None)
            return 1;

        // Sort by ID
        if(itemA.Value is Stain stainA && itemB.Value is Stain stainB)
        {
            if(stainA.RowId < stainB.RowId)
                return -1;

            if(stainA.RowId > stainB.RowId)
                return 1;
        }

        return 0;
    }
}
