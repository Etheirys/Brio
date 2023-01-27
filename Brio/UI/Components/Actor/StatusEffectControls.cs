using ImGuiNET;
using System.Numerics;
using Dalamud.Interface;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Brio.Game.Actor.Extensions;
using Lumina.Excel.GeneratedSheets;

namespace Brio.UI.Components.Actor;
public static class StatusEffectControls
{
    private static int _selectedEntry = 0;
    private static string _searchTerm = string.Empty;
    public unsafe static void Draw(GameObject actor)
    {
        if(actor is BattleChara battleChara)
        {
            var statusManager = battleChara.GetStatusManager();

            var effects = statusManager->GetAllStatuses();
            ImGui.SetNextItemWidth(-1);
            if(ImGui.BeginListBox("###status_effects", new Vector2(0, ImGui.GetTextLineHeight() * 4)))
            {
                int tieBreak = 0;
                foreach(var effect in effects)
                {
                    tieBreak++;
                    var isSelected = _selectedEntry == effect.RowId;
                    if(ImGui.Selectable($"{effect.Name} ({effect.RowId})###active_status_{effect.RowId}_{tieBreak}", isSelected))
                    {
                        _selectedEntry = (int)effect.RowId;
                    }
                }

                ImGui.EndListBox();
            }

            bool isSelectedPlaying = effects.Count((i) => _selectedEntry == i.RowId) > 0;

            ImGui.SetNextItemWidth(ImGui.GetFontSize() * 8);
            ImGui.BeginGroup();
            ImGui.InputInt("###effect_id", ref _selectedEntry, 0, 0);
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);

            

            bool blockPlay = isSelectedPlaying || _selectedEntry <= 0;
            if(blockPlay) ImGui.BeginDisabled();
            if(ImGui.Button(FontAwesomeIcon.Play.ToIconString()))
            {
                statusManager->AddStatus((ushort)_selectedEntry);
            }
            if(blockPlay) ImGui.EndDisabled();

            ImGui.SameLine();

            if(!isSelectedPlaying) ImGui.BeginDisabled();
            if(ImGui.Button(FontAwesomeIcon.Stop.ToIconString()))
            {
                var idx = statusManager->GetStatusIndex((uint)_selectedEntry);
                if(idx != -1)
                    statusManager->RemoveStatus(idx);

            }
            if(!isSelectedPlaying) ImGui.EndDisabled();

            ImGui.SameLine();

            if(ImGui.Button(FontAwesomeIcon.Search.ToIconString()))
            {
                ImGui.OpenPopup("###global_status_list");
            }

            ImGui.PopFont();

            ImGui.EndGroup();

            if(ImGui.BeginPopup("###global_status_list"))
            {
                ImGui.InputText("###global_status_search", ref _searchTerm, 64);

                if(ImGui.BeginListBox("###global_status_listbox"))
                {
                    var statusSheet = Dalamud.DataManager.Excel.GetSheet<Status>();
                    if(statusSheet != null)
                    {
                        var list = statusSheet.Where(i => !string.IsNullOrEmpty(i.Name.RawString)).Where((i) => i.Name.RawString.Contains(_searchTerm, System.StringComparison.CurrentCultureIgnoreCase)).ToList();
                        foreach(var status in list)
                        {
                            if(ImGui.Selectable($"{status.Name} ({status.RowId})###global_status_{status.RowId}", _selectedEntry == status.RowId))
                            {
                                _selectedEntry = (int)status.RowId;
                            }

                            if(ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            {
                                ImGui.CloseCurrentPopup();

                                if(effects.Count((i) => _selectedEntry == i.RowId) == 0)
                                {
                                    statusManager->AddStatus((ushort)_selectedEntry);
                                }
                            }
                        }
                    }
                    ImGui.EndListBox();
                }

                ImGui.EndPopup();
            }
        }
        else
        {
            ImGui.Text("Incompatible actor type.");
        }
    }
}
