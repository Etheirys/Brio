using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Types;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using System.Numerics;

namespace Brio.UI.Components;

public class ActorSelector
{
    public GameObject? SelectedObject { get; private set; } = null;

    public unsafe void Draw()
    {
        var gposeObjects = ActorService.Instance.GPoseActors;

        ImGui.Text($"Actors: {gposeObjects.Count}");

        ImGui.PushItemWidth(-1);
        if(ImGui.BeginListBox("###gpose_actor_list", new Vector2(-1, ImGui.GetTextLineHeight() * 9)))
        {

            var previousSelected = SelectedObject;
            var previousSelectedNative = previousSelected != null ? previousSelected.AsNative() : null;

            SelectedObject = null;
            for(int i = 0; i < gposeObjects.Count; i++)
            {
                var go = gposeObjects[i];
                var gon = go.AsNative();
                bool selected = previousSelectedNative != null ? previousSelectedNative->ObjectIndex == gon->ObjectIndex : false;
                bool shouldSelect = false;
                if(ImGui.Selectable($"{go.Name} ({go.ObjectKind})###object_{go.Address}_{i}", selected))
                {
                    shouldSelect = true;
                }

                if(shouldSelect || (SelectedObject == null && previousSelectedNative != null && previousSelectedNative->ObjectIndex == gon->ObjectIndex))
                {
                    SelectedObject = go;
                }
            }

            ImGui.EndListBox();
        }
        ImGui.PopItemWidth();
    }
}
