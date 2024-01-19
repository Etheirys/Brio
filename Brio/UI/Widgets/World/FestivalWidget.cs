using Brio.Capabilities.World;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.UI.Widgets.World;

internal class FestivalWidget : Widget<FestivalCapability>
{
    public override string HeaderName => "Festivals";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

    private int _selectedFestival;

    private static FestivalSelector _globalFestivalSelector = null!;


    public FestivalWidget(FestivalCapability capability) : base(capability)
    {
        _globalFestivalSelector ??= new("global_festival_selector", capability.AllFestivals.Values);
    }

    public override void DrawBody()
    {
        DrawControls();
        DrawSearch();
    }

    private void DrawControls()
    {
        List<uint> festivals = new(Capability.ActiveFestivals);

        using(ImRaii.Disabled(!Capability.CanModify))
        {
            ImGui.SetNextItemWidth(-1);
            using(var listbox = ImRaii.ListBox("###festival_active_list", new Vector2(0, (ImGui.GetTextLineHeight() * 1.3f) * 4)))
            {
                if(listbox.Success)
                {
                    foreach(var festivalId in festivals)
                    {
                        if(festivalId == 0)
                            continue;

                        var isSelected = festivalId == _selectedFestival;

                        Capability.AllFestivals.TryGetValue(festivalId, out var festival);

                        string name = festival?.ToString() ?? $"Unknown ({festivalId})";

                        if(ImGui.Selectable(name, isSelected))
                        {
                            _selectedFestival = (int)festivalId;
                        }
                    }
                }
            }

            ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXXXX").X);
            ImGui.InputInt("###festival_selected_input", ref _selectedFestival, 0, 0);
            if(ImBrio.IsItemConfirmed())
            {
                AddFestival();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("festival_add_button", FontAwesomeIcon.Plus, "Add Festival", Capability.CanAdd && _selectedFestival != 0))
            {
                AddFestival();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("festival_remove_button", FontAwesomeIcon.Minus, "Remove Festival", _selectedFestival != 0 && festivals.Contains((uint)_selectedFestival)))
            {
                Capability.Remove((uint)_selectedFestival);
            }

            ImGui.SameLine();


            if(ImBrio.FontIconButton("festival_reset_button", FontAwesomeIcon.Redo, "Reset", Capability.HasOverride))
            {
                Capability.Reset();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("festival_search_button", FontAwesomeIcon.Search, "Search", Capability.CanAdd))
            {
                _globalFestivalSelector.Select(null, false);
                ImGui.OpenPopup("festival_search_popup");
            }
        }
    }

    private void DrawSearch()
    {
        using(var popup = ImRaii.Popup("festival_search_popup"))
        {
            if(popup.Success)
            {
                _globalFestivalSelector.Draw();

                if(_globalFestivalSelector.SoftSelectionChanged && _globalFestivalSelector.SoftSelected != null)
                    _selectedFestival = (int)_globalFestivalSelector.SoftSelected.Id;

                if(_globalFestivalSelector.SelectionChanged && _globalFestivalSelector.Selected != null)
                {
                    _selectedFestival = (int)_globalFestivalSelector.Selected.Id;
                    AddFestival();
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private void AddFestival()
    {
        if(_selectedFestival == 0)
            return;

        if(!Capability.CanAdd)
            return;

        Capability.Add((uint)_selectedFestival);
    }
}
