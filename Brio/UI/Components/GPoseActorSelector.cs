using ImGuiNET;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using Dalamud.Interface;

namespace Brio.UI.Components;

public class GPoseActorSelector
{
    public DalamudGameObject? SelectedObject { get; private set; } = null;

    public unsafe void Draw()
    {
        var gposeObjects = Brio.GPoseService.GPoseObjects;

        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.BeginListBox("###gpose_actor_list");

        var previousSelected = SelectedObject;
        var previousSelectedNative = previousSelected != null ? (GameObject*)previousSelected.Address : null;

        SelectedObject = null;
        for (int i = 0; i < gposeObjects.Count; i++)
        {
            var go = gposeObjects[i];
            var gon = (GameObject*)go.Address;
            bool selected = previousSelectedNative != null ? previousSelectedNative->ObjectIndex == gon->ObjectIndex : false;
            bool shouldSelect = false;
            if (ImGui.Selectable($"{go.Name} ({go.ObjectKind})###object_{go.Address}_{i}", selected))
            {
                shouldSelect = true;
            }

            if (shouldSelect || (SelectedObject == null && previousSelectedNative != null && previousSelectedNative->ObjectIndex == gon->ObjectIndex))
            {
                SelectedObject = go;
            }
        }

        ImGui.EndListBox();
        ImGui.PopItemWidth();
    }
}
