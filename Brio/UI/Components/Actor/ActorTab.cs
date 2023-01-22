using Brio.Config;
using Brio.Game.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;

namespace Brio.UI.Components.Actor;

public static class ActorTab
{
    private static ActorSelector _selector = new ActorSelector();

    public unsafe static void Draw()
    {
        bool inGPose = GPoseService.Instance.IsInGPose;

        if(!inGPose) ImGui.TextColored(new(1, 0, 0, 1), "Must be in GPose");

        if(!inGPose) ImGui.BeginDisabled();

        if(ImGui.CollapsingHeader("Actors", ImGuiTreeNodeFlags.DefaultOpen))
        {
            _selector.Draw();

            ImGui.Separator();

            bool canSpawn = ActorSpawnService.Instance.CanSpawn;
            if(!canSpawn) ImGui.BeginDisabled();
            if(ImGui.Button("Spawn###gpose_actor_spawn"))
            {
                ushort? createdId = ActorSpawnService.Instance.Spawn();
                if(createdId == null)
                    Dalamud.ToastGui.ShowError("Failed to Create Actor.");
            }
            if(!canSpawn) ImGui.EndDisabled();

            ImGui.SameLine();

            GameObject? selectedObject = _selector.SelectedObject != null ? _selector.SelectedObject : null;
            bool hasSelected = selectedObject != null;
            if(!hasSelected) ImGui.BeginDisabled();

            if(ImGui.Button("Target###gpose_actor_target"))
            {
                TargetSystem.Instance()->GPoseTarget = selectedObject!.AsNative();
            }

            ImGui.SameLine();
            if(ImGui.Button("Delete###gpose_actor_delete"))
            {
                ActorSpawnService.Instance.DestroyObject(selectedObject!);
            }

            if(!hasSelected) ImGui.EndDisabled();

            ImGui.SameLine();

            if(ImGui.Button("Clear###gpose_actor_delete_all"))
            {
                ActorSpawnService.Instance.DestroyAll();
            }
        }

        if(ImGui.CollapsingHeader("Redraw"))
        {
            if(_selector.SelectedObject != null)
            {
                RedrawControls.Draw(_selector.SelectedObject);
            }
            else
            {
                ImGui.Text("No actor selected.");
            }
        }

        if(ConfigService.Configuration.AllowPenumbraIntegration)
        {
            if(ImGui.CollapsingHeader("Penumbra"))
            {
                if(_selector.SelectedObject != null)
                {
                    PenumbraCollectionControls.Draw(_selector.SelectedObject);
                }
                else
                {
                    ImGui.Text("No actor selected.");
                }
            }
        }

        if(ImGui.CollapsingHeader("Status Effects"))
        {
            if(_selector.SelectedObject != null)
            {
                StatusEffectControls.Draw(_selector.SelectedObject);
            }
            else
            {
                ImGui.Text("No actor selected.");
            }
        }

        if(!inGPose) ImGui.EndDisabled();
    }
}
