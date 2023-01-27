using Brio.Game.GPose;
using Brio.Game.World;
using Dalamud.Interface;
using ImGuiNET;

namespace Brio.UI.Components.World;
public static class FestivalControls
{
    private static string _searchTerm = string.Empty;
    private static bool _searchShowUnknown = false;
    private static bool _searchShowUnsafe = false;

    private static int _festivalInput;


    public unsafe static void Draw()
    {
        var currentFestival = FestivalService.Instance.CurrentFestival;
        var isOverridden = FestivalService.Instance.IsOverriden;

        ImGui.Text($"Current: {currentFestival}");

        ImGui.Spacing();

        if(!GPoseService.Instance.IsInGPose)
        {
            ImGui.Text("Must be in GPose to modify festival.");
            return;
        }

        ImGui.SetNextItemWidth(85f);
        ImGui.InputInt("Festival##input", ref _festivalInput, 0, 0);

        ImGui.SameLine();

        ImGui.PushFont(UiBuilder.IconFont);
        if(ImGui.Button(FontAwesomeIcon.Play.ToIconString() + "###festival_play_button"))
        {
            ApplyOverride((ushort)_festivalInput);
        }
        ImGui.SameLine();
        if(!isOverridden) ImGui.BeginDisabled();
        if(ImGui.Button(FontAwesomeIcon.Redo.ToIconString() + "###festival_reset_button"))
        {
            FestivalService.Instance.ResetFestivalOverride();
        }
        if(!isOverridden) ImGui.EndDisabled();

        ImGui.SameLine();
        if(ImGui.Button(FontAwesomeIcon.Search.ToIconString() + "###festival_search_button"))
        {
            ImGui.OpenPopup("###festival_search_popup");
        }

        ImGui.PopFont();

        DrawSearch();
    }

    private static void DrawSearch()
    {
        if(ImGui.BeginPopup("###festival_search_popup"))
        {
            ImGui.InputText("###festival_search_term", ref _searchTerm, 1024);
           
            if(ImGui.BeginListBox("###festival_search_list"))
            {
                var festivals = FestivalService.Instance.FestivalEntries;

                foreach(var entry in festivals )
                {
                    if(entry.Unknown && !_searchShowUnknown)
                        continue;

                    if(entry.Unsafe && !_searchShowUnsafe)
                        continue;

                    var entryText = entry.ToString();

                    if(!entryText.Contains(_searchTerm, System.StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    bool isSelected = _festivalInput == entry.Id;

                    if(ImGui.Selectable(entryText, isSelected))
                    {
                        _festivalInput= entry.Id;
                    }
                    if(ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        ApplyOverride((ushort)_festivalInput);
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndListBox();
            }

            ImGui.Checkbox("Show Unknown", ref _searchShowUnknown);
            ImGui.SameLine();
            ImGui.Checkbox("Show Unsafe", ref _searchShowUnsafe);

            ImGui.EndPopup();
        }
    }

    private static void ApplyOverride(ushort festivalId) => FestivalService.Instance.SetFestivalOverride(festivalId);
}
