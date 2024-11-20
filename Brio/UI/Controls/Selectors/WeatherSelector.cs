using Brio.Game.Types;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

internal class WeatherSelector(string id) : Selector<WeatherUnion>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;


    private readonly List<Weather> _validWeathers = [];
    private bool _showInvalidWeathers = false;

    protected override void PopulateList()
    {
        foreach(var weather in GameDataProvider.Instance.Weathers.Values)
            AddItem(weather);
    }

    public void SetNaturalWeathers(IEnumerable<Weather> weathers)
    {
        _validWeathers.Clear();
        _validWeathers.AddRange(weathers);
        UpdateList();
    }

    protected override void DrawItem(WeatherUnion union, bool isHovered)
    {
        ImBrio.BorderedGameIcon("icon", union, flags: ImGuiButtonFlags.None, size: IconSize);
    }

    protected override void DrawOptions()
    {
        if(ImGui.Checkbox("Show Invalid Weathers", ref _showInvalidWeathers))
            UpdateList();
    }

    protected override bool Filter(WeatherUnion item, string search)
    {

        return item.Match(
            (weatherRow) =>
            {
                if(string.IsNullOrEmpty(weatherRow.Name.ToString()))
                    return false;

                if(!_showInvalidWeathers && !_validWeathers.Contains(weatherRow))
                    return false;

                var searchText = $"{weatherRow.Name} {weatherRow.RowId}";

                if(searchText.Contains(search, System.StringComparison.InvariantCultureIgnoreCase))
                    return true;

                return false;
            },
            none => true
       );
    }
}
