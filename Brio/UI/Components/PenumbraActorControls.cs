using Brio.Utils;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using ImGuiNET;

namespace Brio.UI.Components;

public static class PenumbraActorControls
{
    private static string? _selectedCollection = null;

    public unsafe static void Draw(GameObject gameObject)
    {
        if(Brio.PenumbraIPC.IsPenumbraEnabled)
        {
            var collections = Brio.PenumbraIPC.Collections;

            if (_selectedCollection == null && collections.Count > 0)
                _selectedCollection = collections[0];

            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Collection").X - 40);
            if (ImGui.BeginCombo("Collection", _selectedCollection))
            {
                foreach(var collection in collections)
                {
                    bool selected = collection == _selectedCollection;
                    if(ImGui.Selectable(collection, selected))
                    {
                        _selectedCollection= collection;
                    }
                }

                ImGui.EndCombo();
            }
            ImGui.PopItemWidth();

            ImGui.SameLine();
            
            ImGui.PushFont(UiBuilder.IconFont);
            if(ImGui.Button(FontAwesomeIcon.Redo.ToIconString()))
            {
                Brio.PenumbraIPC.RefreshPenumbraStatus();
            }
            ImGui.PopFont();

            bool validKind = gameObject.ObjectKind == ObjectKind.Player;
            bool allowed = validKind && Brio.PenumbraIPC.CanApplyCollection;
            if (!allowed) ImGui.BeginDisabled();
            if(ImGui.Button("Apply Collection"))
            {
                Brio.PenumbraIPC.RedrawActorWithCollection(gameObject, _selectedCollection!);
            }
            if (!validKind) ImGui.TextColored(new(1, 0, 0, 1), "Only available for Player.");
            if (!validKind) ImGui.TextColored(new(1, 0, 0, 1), "Spawn a Player above.");
            if (!allowed) ImGui.EndDisabled();
        }
        else
        {
            ImGui.TextColored(new(1, 0, 0, 1), "Penumbra integration disabled.");
            ImGui.TextColored(new(1, 0, 0, 1), "Ensure Penumbra is enabled and updated.");

        }
    }
}
