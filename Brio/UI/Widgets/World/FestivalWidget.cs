using Brio.Capabilities.World;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class FestivalWidget : Widget<FestivalCapability>
{
    public override string HeaderName => "Festivals";
    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    private int _selectedFestival;
    private int _selectedPhase;

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
        using(ImRaii.Disabled(!Capability.CanModify))
        {
            ImGui.SetNextItemWidth(-1);
            using(var listbox = ImRaii.ListBox("###festival_active_list", new Vector2(0, (ImGui.GetTextLineHeight() * 1.3f) * 6)))
            {
                if(listbox.Success)
                {
                    foreach(var festival in Capability.ActiveFestivals)
                    {
                        if(festival.Id == 0)
                            continue;

                        var isSelected = festival.Id == _selectedFestival;

                        Capability.AllFestivals.TryGetValue(festival.Id, out var festivalEntry);

                        string name = $"{festivalEntry?.ToString() ?? "Unknown"} ({festival.Id} - {festival.Phase})" ;

                        if(ImGui.Selectable(name, isSelected))
                        {
                            _selectedFestival = (int)festival.Id;
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

            ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXXXX").X);
            ImGui.InputInt("###festival_phase_input", ref _selectedPhase, 0, 0);
            if(ImBrio.IsItemConfirmed())
            {
                Capability.ChangePhase((uint)_selectedFestival, (ushort)_selectedPhase);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("festival_add_button", FontAwesomeIcon.Plus, "Add Festival", Capability.CanAdd && _selectedFestival != 0))
            {
                if(_selectedPhase != 0)
                    Capability.ChangePhase((uint)_selectedFestival, (ushort)_selectedPhase);
                else
                    AddFestival();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("festival_remove_button", FontAwesomeIcon.Minus, "Remove Festival", _selectedFestival != 0 && Capability.ActiveFestivals.Any(f => f.Id == (uint)_selectedFestival)))
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
