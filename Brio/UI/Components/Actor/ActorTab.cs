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
    private static bool _allowCompanions = true;

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
                SpawnOptions options = _allowCompanions ? SpawnOptions.ReserveCompanionSlot : SpawnOptions.None;
                ushort? createdId = ActorSpawnService.Instance.Spawn(options);
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

            ImGui.Checkbox("Allow Attachments", ref _allowCompanions);
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Allow attachments to be attached to spawned actors.\nThis will take two slots instead of one so reduces the total actors you can spawn.");
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

        if(ImGui.CollapsingHeader("Animations"))
        {
            if(_selector.SelectedObject != null)
            {
                ActionTimelineControls.Draw(_selector.SelectedObject);
            }
            else
            {
                ImGui.Text("No actor selected.");
            }
        }

        if(ImGui.CollapsingHeader("Attachments"))
        {
            if(_selector.SelectedObject != null)
            {
                AttachmentControls.Draw(_selector.SelectedObject);
            }
            else
            {
                ImGui.Text("No actor selected.");
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
