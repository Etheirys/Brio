using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;

namespace Brio.UI.Components;

public static class ActorTabControls
{
    private static GPoseActorSelector _selector = new GPoseActorSelector();

    public unsafe static void Draw()
    {
        bool inGPose = Brio.GPoseService.IsInGPose;

        if (!inGPose) ImGui.TextColored(new(1, 0, 0, 1), "Must be in GPose");

        if (!inGPose) ImGui.BeginDisabled();

        if (ImGui.CollapsingHeader("Actors", ImGuiTreeNodeFlags.DefaultOpen))
        { 
            _selector.Draw();

            ImGui.Separator();


            bool canSpawn = Brio.ActorSpawnService.CanSpawn;
            if (!canSpawn) ImGui.BeginDisabled();
            if (ImGui.Button("Spawn###gpose_actor_spawn"))
            {
                ushort? createdId = Brio.ActorSpawnService.Spawn();
                if (createdId == null)
                    Dalamud.ToastGui.ShowError("Failed to Create Actor.");
            }
            if (!canSpawn) ImGui.EndDisabled();

            ImGui.SameLine();

            GameObject* selectedObject = _selector.SelectedObject != null ? (GameObject*) _selector.SelectedObject.Address : null;
            bool hasSelected = selectedObject != null;
            if (!hasSelected) ImGui.BeginDisabled();

            if (ImGui.Button("Target###gpose_actor_target"))
            {
                TargetSystem.Instance()->GPoseTarget = selectedObject;
            }

            ImGui.SameLine();

            if (ImGui.Button("Delete###gpose_actor_delete"))
            {
                Brio.ActorSpawnService.DestroyObject(selectedObject);
            }

            if (!hasSelected) ImGui.EndDisabled();

            ImGui.SameLine();

            if (ImGui.Button("Clear###gpose_actor_delete_all"))
            {
                Brio.ActorSpawnService.DestroyAll();
            }
        }

        if (ImGui.CollapsingHeader("Actor Redraw"))
        {
            if (_selector.SelectedObject != null)
            {
                ActorRedrawControls.Draw(_selector.SelectedObject);
            }
            else
            {
                ImGui.Text("No actor selected.");
            }
        }

        if (Brio.Configuration.AllowPenumbraIntegration)
        {
            if (ImGui.CollapsingHeader("Penumbra"))
            {
                if (_selector.SelectedObject != null)
                {
                    PenumbraActorControls.Draw(_selector.SelectedObject);
                }
                else
                {
                    ImGui.Text("No actor selected.");
                }
            }
        }

        if (!inGPose) ImGui.EndDisabled();
    }
}
